// <copyright file="FunctionsController.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Testing.AzureFunctions
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Management;
    using System.Net.NetworkInformation;
    using System.Threading.Tasks;

    using Corvus.Testing.AzureFunctions.Internal;

    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Starts, manages and terminates functions instances for testing purposes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class supports a limited degree of thread safety: you can have multiple calls to
    /// <see cref="StartFunctionsInstance(string, int, string, string, FunctionConfiguration?)"/> in progress simultaneously for a single
    /// instance of this class, but <see cref="TeardownFunctions"/> must not be called concurrently
    /// with any other calls into this class. The intention is to enable tests to spin up multiple
    /// functions simultaneously. This is useful because function startup can be the dominant
    /// factor in test execution time for integration tests.
    /// </para>
    /// </remarks>
    public sealed class FunctionsController
    {
        private const long StartupTimeout = 60;
#pragma warning disable SA1310 // Field names should not contain underscore - this is the proper name for this symbol
        private const int E_ACCESSDENIED = unchecked((int)0x80070005);
#pragma warning restore SA1310

        private readonly List<FunctionOutputBufferHandler> output = new();
        private readonly object sync = new();

        private readonly ILogger logger;
        private IDisposable? functionLogScope;

        /// <summary>
        /// Instantiates an object for starting and stopping instances of Azure Functions.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The <see cref="ILogger" /> argument can be provided using <c>Microsoft.Extensions.Logging.LoggerFactory.Create()</c>
        /// and <see cref="ILoggerFactory.CreateLogger(string)"/>. This allows you to easily configure
        /// logging as you would within an ASP.NET Core app, using an <c>Microsoft.Extensions.Logging.ILoggingBuilder</c> instance
        /// and the usual extension methods like <c>AddDebug()</c> and <c>AddConsole()</c>.
        /// </para>
        /// </remarks>
        /// <param name="logger">An <see cref="ILogger"/> destination for useful output messages.</param>
        public FunctionsController(ILogger logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Start a functions instance.
        /// </summary>
        /// <param name="path">The location of the functions project.</param>
        /// <param name="port">The port on which to start the functions instance.</param>
        /// <param name="runtime">The runtime version for use with the function host (e.g. net6.0).</param>
        /// <param name="provider">The functions provider. Defaults to csharp.</param>
        /// <param name="configuration">A <see cref="FunctionConfiguration"/> instance, for conveying
        /// configuration values via environment variables to the function host process.</param>
        /// <returns>A task that completes once the function instance has started.</returns>
        public async Task StartFunctionsInstance(string path, int port, string runtime, string provider = "csharp", FunctionConfiguration? configuration = null)
        {
            this.functionLogScope = this.logger.BeginScope(this);
            if (IsSomethingAlreadyListeningOn(port))
            {
                this.logger.LogWarning("Found a process listening on {Port}. Is this a debug instance?", port);
                this.logger.LogWarning("This test run will reuse this process, and so may produce unexpected results.");
                return;
            }

            this.logger.LogInformation("Starting a function instance for project {Path} on port {Port}", path, port);
            this.logger.LogDebug("Starting process");

            FunctionOutputBufferHandler bufferHandler = await StartFunctionHostProcess(
                port,
                provider,
                FunctionProject.ResolvePath(path, runtime, this.logger),
                configuration).ConfigureAwait(false);

            lock (this.sync)
            {
                this.output.Add(bufferHandler);
            }

            this.logger.LogDebug("Process started; waiting for initialisation to complete");

            await Task.WhenAny(
                bufferHandler.JobHostStarted,
                bufferHandler.ExitCode,
                Task.Delay(TimeSpan.FromSeconds(StartupTimeout))).ConfigureAwait(false);

            if (bufferHandler.ExitCode.IsCompleted)
            {
                int exitCode = await bufferHandler.ExitCode.ConfigureAwait(false);
                this.logger.LogError(
                    @"Failed to start function host, process terminated unexpectedly with exit code {ExitCode}.
StdOut: {StdOut}
StdErr: {StdErr}",
                    exitCode,
                    bufferHandler.StandardOutputText,
                    bufferHandler.StandardErrorText);

                throw new FunctionStartupException(
                    $"Function host process terminated unexpectedly with exit code {exitCode}.",
                    stdout: bufferHandler.StandardOutputText,
                    stderr: bufferHandler.StandardErrorText);
            }

            if (!bufferHandler.JobHostStarted.IsCompleted)
            {
                throw new FunctionStartupException("Timed out while starting functions instance.");
            }

            this.logger.LogDebug("Initialisation completed");
            this.logger.LogInformation("Function {Path} now running on port {Port}", path, port);
        }

        /// <summary>
        /// Provides access to the output.
        /// </summary>
        /// <returns>All output from the function host process.</returns>
        public IEnumerable<IProcessOutput> GetFunctionsOutput()
        {
            return this.output.AsReadOnly();
        }

        /// <summary>
        /// Tear down the running functions instances, forcibly killing the process where required.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is a best-effort approach, because in some environments (e.g., on some build
        /// agents) we get Access Denied errors when trying to kill the host forcibly.
        /// </para>
        /// </remarks>
        public void TeardownFunctions()
        {
            var aggregate = new List<Exception>();
            foreach (FunctionOutputBufferHandler outputHandler in this.output)
            {
                try
                {
                    KillProcessAndChildren(outputHandler.Process.Id);

                    if (!outputHandler.Process.WaitForExit(10000))
                    {
                        Console.Error.WriteLine("Unable to shut down functions host");
                    }
                }
                catch (Exception e)
                {
                    aggregate.Add(e);
                }
            }

            this.logger.LogAllAndClear(this.output);
            this.functionLogScope?.Dispose();

            if (aggregate.Count > 0)
            {
                throw new AggregateException(aggregate);
            }
        }

        private static async Task<string> GetToolPath()
        {
            string npmPrefix = await GetNpmPrefix().ConfigureAwait(false);
            string toolsFolder = Path.Combine(
                npmPrefix,
                @"node_modules\azure-functions-core-tools\bin");

            if (!Directory.Exists(toolsFolder))
            {
                throw new FunctionStartupException(
                    $"Azure Functions runtime not found at {toolsFolder}. Have you run: " +
                    "'npm install -g azure-functions-core-tools@3 --unsafe-perm true'?");
            }

            string toolPath = Path.Combine(
                toolsFolder,
                Environment.OSVersion.Platform == PlatformID.Win32NT ? "func.exe" : "func");

            Console.WriteLine($"\tToolsPath: {toolPath}");
            return toolPath;
        }

        /// <summary>
        /// Kill a process, and all of its children, grandchildren, etc.
        /// </summary>
        /// <param name="pid">Process ID.</param>
        private static void KillProcessAndChildren(int pid)
        {
            // Cannot close 'system idle process'.
            if (pid == 0)
            {
                return;
            }

            using (var searcher =
                new ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessID=" + pid))
            {
                foreach (ManagementObject mo in searcher.Get().Cast<ManagementObject>())
                {
                    KillProcessAndChildren(Convert.ToInt32(mo["ProcessID"]));
                }
            }

            try
            {
                var proc = Process.GetProcessById(pid);
                try
                {
                    proc.Kill();
                }
                catch (Win32Exception x)
                when (x.ErrorCode == E_ACCESSDENIED)
                {
                    Console.WriteLine($"Access denied when trying to kill process id {pid}, '{proc.ProcessName}'");
                }
            }
            catch (ArgumentException)
            {
                // Process already exited.
            }
        }

        /// <summary>
        /// Discover npm's global prefix (the parent of its global module cache location).
        /// </summary>
        /// <returns>
        /// The global prefix reported by npm.
        /// </returns>
        /// <remarks>
        /// <para>
        /// To run Azure Functions locally in tests, we need to run the <c>func</c> command
        /// from the <c>azure-functions-core-tools</c> npm package.
        /// </para>
        /// <para>
        /// Unfortunately, npm ends up putting this in different places on different machines.
        /// Debugging locally, and also on private build agents, the global module cache is
        /// typically in <c>%APPDATA%\npm\npm_modules</c>. However, on hosted build agents it
        /// currently resides in <c>c:\npm\prefix</c>.
        /// </para>
        /// <para>
        /// The most dependable way to find where npm puts these things is to ask npm, by
        /// running the command <c>npm prefix -g</c>, which is what this function does.
        /// </para>
        /// </remarks>
        private static async Task<string> GetNpmPrefix()
        {
            // Running npm directly can run into weird PATH issues, so it's more reliable to run
            // cmd.exe, and then ask it to run our command - that way we get the same PATH
            // behaviour we'd get running the command manually.
            var processHandler = new ProcessOutputHandler(
                new ProcessStartInfo("cmd.exe", "/c npm prefix -g")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                });

            processHandler.Start();

            await Task.WhenAny(
                processHandler.ExitCode,
                Task.Delay(TimeSpan.FromSeconds(10))).ConfigureAwait(false);

            if (!processHandler.ExitCode.IsCompleted)
            {
                throw new FunctionStartupException(
                    "npm task did not exit before timeout.",
                    stdout: processHandler.StandardOutputText,
                    stderr: processHandler.StandardErrorText);
            }

            processHandler.EnsureComplete();

            if (processHandler.Process.ExitCode != 0)
            {
                throw new FunctionStartupException("Unable to run npm.", stderr: processHandler.StandardErrorText);
            }

            // We get a newline character on the end of the standard output, so we need to
            // trim before returning.
            return processHandler.StandardOutputText.Trim();
        }

        private static bool IsSomethingAlreadyListeningOn(int port)
        {
            return IPGlobalProperties
                .GetIPGlobalProperties()
                .GetActiveTcpListeners()
                .Any(e => e.Port == port);
        }

        private static async Task<FunctionOutputBufferHandler> StartFunctionHostProcess(
            int port,
            string provider,
            string workingDirectory,
            FunctionConfiguration? functionConfiguration)
        {
            var startInfo = new ProcessStartInfo(
                await GetToolPath().ConfigureAwait(false),
                $"host start --port {port} --{provider}")
            {
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                CreateNoWindow = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                WindowStyle = ProcessWindowStyle.Normal,
            };

            if (functionConfiguration != null)
            {
                foreach (KeyValuePair<string, string> kvp in functionConfiguration.EnvironmentVariables)
                {
                    startInfo.EnvironmentVariables[kvp.Key] = kvp.Value;
                }
            }

            // This prevents the Azure Functions host from observing the filesystem and attempting to restart when
            // it thinks something has changed. We never want this in these test scenarios, because nothing should
            // be changing, and all that happens is the host exits and then fails to restart. We sometimes see
            // spurious restarts, which is why we disable this.
            startInfo.EnvironmentVariables["AzureFunctionsJobHost__FileWatchingEnabled"] = "false";

            // Force the logging level to debug to ensure we can pick up the message that tells us the function is
            // ready to go.
            startInfo.EnvironmentVariables["AzureFunctionsJobHost:logging:logLevel:default"] = "Debug";

            var processHandler = new FunctionOutputBufferHandler(startInfo);
            processHandler.Start();

            return processHandler;
        }
    }
}