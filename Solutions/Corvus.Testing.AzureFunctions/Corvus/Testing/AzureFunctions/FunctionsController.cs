﻿// <copyright file="FunctionsController.cs" company="Endjin Limited">
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
    using System.Runtime.InteropServices;
    using System.Threading;
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

        // Ref. https://learn.microsoft.com/en-us/windows/win32/debug/system-error-codes--0-499-#ERROR_BAD_EXE_FORMAT
        private const int INVALID_EXECUTABLE = 193;
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

            FunctionOutputBufferHandler bufferHandler = await this.StartFunctionHostProcess(
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
        /// agents) we get Access Denied errors when trying to kill the host forcibly. We will
        /// retry up to three times when that happens because sometimes race conditions can cause
        /// spurious access denied failures. But we will eventually give up. We don't throw an
        /// exception in this case because there may be situations in which tests can proceed,
        /// but in most cases this is likely to result in the next test failing, because it will
        /// try to run the functions host again with the same port number settings, and that will
        /// fail because the port is already in use by the process we were unable to terminate.
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

        private async Task<string> GetToolPath()
        {
            string toolLocatorName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "where.exe" : "which";
            const string toolName = "func";
            var toolLocator = new ProcessOutputHandler(
                new ProcessStartInfo(toolLocatorName, toolName)
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                });

            toolLocator.Start();
            bool found = await toolLocator.ExitCode == 0;
            toolLocator.EnsureComplete();

            if (!found)
            {
                throw new FunctionStartupException(
                    "Azure Functions runtime not found. Have you run: " +
                    "'npm install -g azure-functions-core-tools@ --unsafe-perm true'?");
            }

            string[] toolPaths = toolLocator.StandardOutputText.Split("\n").Select(s => s.Trim()).ToArray();

            foreach (string toolPath in toolPaths)
            {
                this.logger.LogDebug("Testing tool path '{ToolPath}' can be used for running Functions projects.", toolPath);
                var process = new ProcessOutputHandler(new ProcessStartInfo(toolPath));
                try
                {
                    process.Start();
                    if (await process.ExitCode == 0)
                    {
                        this.logger.LogInformation("Resolved tool path '{ToolPath}' for running Functions projects.", toolPath);
                        return toolPath;
                    }
                }
                catch (Win32Exception ex) when (ex.NativeErrorCode == INVALID_EXECUTABLE)
                {
                    // If the file is not a valid executable a Win32Exception will be thrown.
                    // Deliberately ignore those exceptions as they indicate we've another platform's
                    // intermediate format (e.g. the Node binary).
                    continue;
                }
            }

            throw new PlatformNotSupportedException("None of the resolved Functions executable paths could be invoked. Have you run 'npm install -g azure-functions-core-tools@ --unsafe-perm true'?")
            {
                Data =
                {
                    ["ResolvedPaths"] = toolPaths,
                },
            };
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

            bool failedDueToAccessDenied;
            int accessDenyRetryCount = 0;
            const int MaxAccessDeniedRetries = 3;

            do
            {
                failedDueToAccessDenied = false;

                Process proc;
                try
                {
                    proc = Process.GetProcessById(pid);
                }
                catch (ArgumentException)
                {
                    // Process already exited.
                    return;
                }

                try
                {
                    proc.Kill();
                }
                catch (Win32Exception x)
                when (x.ErrorCode == E_ACCESSDENIED)
                {
                    Console.Error.WriteLine($"Access denied when trying to kill process id {pid}, '{proc.ProcessName}'");
                    failedDueToAccessDenied = true;
                }

                if (failedDueToAccessDenied && accessDenyRetryCount < MaxAccessDeniedRetries)
                {
                    Thread.Sleep(100);
                }
            }
            while (failedDueToAccessDenied && accessDenyRetryCount++ < MaxAccessDeniedRetries);
        }

        private static bool IsSomethingAlreadyListeningOn(int port)
        {
            return IPGlobalProperties
                .GetIPGlobalProperties()
                .GetActiveTcpListeners()
                .Any(e => e.Port == port);
        }

        private async Task<FunctionOutputBufferHandler> StartFunctionHostProcess(
            int port,
            string provider,
            string workingDirectory,
            FunctionConfiguration? functionConfiguration)
        {
            var startInfo = new ProcessStartInfo(
                await this.GetToolPath().ConfigureAwait(false),
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