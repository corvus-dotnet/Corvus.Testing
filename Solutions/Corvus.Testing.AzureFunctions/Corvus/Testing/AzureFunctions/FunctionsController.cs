// <copyright file="FunctionsController.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Testing.AzureFunctions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
#if NETSTANDARD2_0
    using System.Management;
#endif
    using System.Net.NetworkInformation;
    using System.Runtime.InteropServices;
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

        private readonly List<FunctionOutputBufferHandler> output = new List<FunctionOutputBufferHandler>();
        private readonly object sync = new object();

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
        /// <param name="runtime">The runtime version for use with the function host (e.g. netcoreapp3.1).</param>
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
        public void TeardownFunctions()
        {
            var aggregate = new List<Exception>();
            foreach (FunctionOutputBufferHandler outputHandler in this.output)
            {
                try
                {
                    KillProcessAndChildren(outputHandler.Process.Id);

                    outputHandler.Process.WaitForExit();
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
                "azure-functions-core-tools",
                "bin");

            if (!Directory.Exists(toolsFolder))
            {
                throw new FunctionStartupException(
                    $"Azure Functions runtime not found at {toolsFolder}. Have you run: " +
                    "'npm install -g azure-functions-core-tools@3 --unsafe-perm true'?");
            }

            string toolPath = Path.Combine(
                toolsFolder,
                "func");

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

#if NETSTANDARD2_0
            using (var searcher =
                new ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessID=" + pid))
            {
                foreach (ManagementObject mo in searcher.Get())
                {
                    KillProcessAndChildren(Convert.ToInt32(mo["ProcessID"]));
                }
            }
#endif
            try
            {
                var proc = Process.GetProcessById(pid);
#if NETSTANDARD2_0
                proc.Kill();
#else
                proc.Kill(entireProcessTree: true);
#endif
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
            // On Windows, the npm command is currently implemented as a .cmd file (the Windows
            // Command Prompt equivalent of a batch file). The Windows APIs for launching new
            // processes do not recognize command files, batch files, or anything similar: they
            // expect to be given a Win32 executable to launch. The ability to run a text file full
            // of script is, as far as Windows is concerned, the shell's business, not the OS's.
            // The upshot is that although typing "npm" at a command prompt will run npm, passing
            // just "npm" to the process start APIs doesn't work, because those APIs don't invoke
            // the command prompt logic unless you tell them too.
            // So on Windows, we have to launch cmd.exe and then tell it to execute the npm
            // command for us - this is the only way to execute command line tools that are
            // actually scripts from .NET on Windows. (And while it's possible that setting the
            // UseShellExecute flag might fix this, you can't redirect standard input and output
            // when you do that. The point of that mechanism is to get the same behaviour that a
            // user would get when running something interactively.)
            // Of course, Linux and MacOS don't have cmd.exe, so this approach is guaranteed to
            // fail on those. However, those operating systems bake in the support for determining
            // that the thing you've asked to run isn't an executable binary but some sort of
            // script, and automatically working out which program you need to launch to process
            // the text file you want to run. So on those operating systems if you ask the OS to
            // run "npm", after it determines that the file you've asked it to run doesn't look
            // like an executable program, it will then look for the conventional "#!" text on
            // the first line, which tells it the program that it really needs to run, and the OS
            // will run that instead.
            // Since Windows and Unix-like operating systems have quite different approaches here
            // we need OS-specific handling.
            (string command, string arguments) = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? ("cmd.exe", "/c npm root -g")
                : ("npm", "root -g");
            var processHandler = new ProcessOutputHandler(
                new ProcessStartInfo(command, arguments)
                {
                    UseShellExecute = false, // Standard IO capture only works if this is false.
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

            // Force the logging level to debug to ensure we can pick up the message that tells us the function is
            // ready to go.
            startInfo.EnvironmentVariables["AzureFunctionsJobHost:logging:logLevel:default"] = "Debug";

            var processHandler = new FunctionOutputBufferHandler(startInfo);
            processHandler.Start();

            return processHandler;
        }
    }
}