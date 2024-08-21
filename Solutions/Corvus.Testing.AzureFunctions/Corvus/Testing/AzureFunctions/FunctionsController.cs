// <copyright file="FunctionsController.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Testing.AzureFunctions
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Management;
    using System.Net.Http;
    using System.Net.NetworkInformation;
    using System.Net.Sockets;
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

            await this.VerifyPortNotInUseAsync(port).ConfigureAwait(false);

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
                    """
                    Failed to start function host, process terminated unexpectedly with exit code {ExitCode}.
                    StdOut: {StdOut}
                    StdErr: {StdErr}
                    """,
                    exitCode,
                    bufferHandler.StandardOutputText,
                    bufferHandler.StandardErrorText);

                throw new FunctionStartupException(
                    $"Function host process terminated unexpectedly with exit code {exitCode}.",
                    stdout: bufferHandler.StandardOutputText,
                    stderr: bufferHandler.StandardErrorText);
            }

            await WaitUntilConnectionsAcceptedAsync(port).ConfigureAwait(false);

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
                catch (AggregateException ex)
                {
                    Console.WriteLine("Caught an AggregateException: " + ex.Message);
                    foreach (var innerEx in ex.InnerExceptions)
                    {
                        if (innerEx is Win32Exception win32Ex)
                        {
                            if (win32Ex.ErrorCode == E_ACCESSDENIED)
                            {
                                Console.Error.WriteLine($"Access denied when trying to kill process id {pid}, '{proc.ProcessName}'");
                                failedDueToAccessDenied = true;
                            }
                        }
                        else
                        {
                            throw;
                        }
                    }
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

        /// <summary>
        /// Waits until this computer is accepting TCP requests on the specified port.
        /// </summary>
        /// <param name="port">The port to check.</param>
        /// <returns>A task that completes once the check is complete.</returns>
        /// <exception cref="FunctionStartupException">
        /// Thrown if requests are not accepted on the specified port within 10 seconds.
        /// </exception>
        /// <remarks>
        /// <para>
        /// Normally we expect this test to succeed immediately, because we observe the standard
        /// output of the host process and wait until it produces text that it only produces when
        /// it is ready. So this is only to handle either odd cases, or the possibility that the
        /// function host's behaviour changes.
        /// </para>
        /// <para>
        /// We've seen occasional surprising failures where tests' attempts to connect to the
        /// function get an HttpRequestException with an inner SocketException reporting that
        /// nothing is listening on the specified port. It's possible that this was caused by
        /// the old behaviour in which we would continue without starting the function if the
        /// port was already in use. (This supported one way of debugging, but it appears that
        /// it creates a race condition in which we fail to launch a new process host because
        /// one from a previous test hadn't quite finished shutting down.) We've removed that
        /// behaviour in V3, so hopefully it won't happen again.
        /// However, the fact remains that there isn't any supported way to get a reliable
        /// indication that the function host ready. To improve robustness, we attempt to open a
        /// connection so that we have positive proof that it is now listening for incoming
        /// requests.
        /// </para>
        /// </remarks>
        private static async Task WaitUntilConnectionsAcceptedAsync(int port)
        {
            // We don't actually know what URL to hit, but as long as we get a 404 instead of a
            // connection failure, it means the host is now listening.
            string testUrl = $"http://localhost:{port}/";
            bool listeningOnPort = false;
            for (int i = 0; i < 20; ++i)
            {
                using HttpClient web = new();
                try
                {
                    HttpResponseMessage r = await web.GetAsync(testUrl).ConfigureAwait(false);
                    listeningOnPort = true;
                }
                catch (HttpRequestException x)
                when (x.InnerException is SocketException)
                {
                    // The host isn't yet accepting incoming connections. It's OK to swallow
                    // the exception because either we're about to wait and try again, or we're
                    // about to give up waiting. (But any other exception at this point is
                    // unexpected, so we let them continue.)
                }

                if (listeningOnPort)
                {
                    break;
                }

                await Task.Delay(500).ConfigureAwait(false);
            }

            if (!listeningOnPort)
            {
                throw new FunctionStartupException($"Functions instance did not start listening on port {port}.");
            }
        }

        /// <summary>
        /// Checks that a TCP port is not already in use by some other process.
        /// </summary>
        /// <param name="port">The port to check.</param>
        /// <returns>A task that completes once the check has been performed.</returns>
        /// <exception cref="FunctionStartupException">
        /// Thrown if the port is in use, and was not relinquished within 3 seconds.
        /// </exception>
        /// <remarks>
        /// It appears that sometimes the port looks like it's in use due to an earlier test not
        /// quite having shut down yet. Our test teardown logic waits until we have observed a
        /// process exit, but even then, it appears that just occasionally, the port still
        /// appears to be in use by the time the next test runs. It's a pretty narrow window, and
        /// seems to be impossible to observe with a debugger attached, but it does happen in
        /// normal execution. So if the port appears to be in use, we wait briefly and try again a
        /// few times in case it's one of these spurious transient cases where the OS hasn't quite
        /// registered that the previous process has gone.
        /// </remarks>
        private async Task VerifyPortNotInUseAsync(int port)
        {
            for (int tries = 0; PortFinder.IsSomethingAlreadyListeningOn(port); ++tries)
            {
                await Task.Delay(100).ConfigureAwait(false);
                if (tries > 30)
                {
                    // We can now be confident that whatever's using this port wasn't just about to
                    // exit, so we definitely have a problem.
                    // We used to tolerate this and plough on regardless, but this is a bad idea
                    // because you can get some very baffling test results. It's possible some people
                    // used to rely on this to be able to debug (e.g., start debugging the target
                    // function, then run tests). If that's what you want to do, it's better to
                    // stick a breakpoint somewhere in your tests at a point where the function
                    // will already be running, and before your test does anything interesting.
                    // You can then use Visual Studio's Debug -> Attach to Process command to
                    // attach the debugger to the function host. (That way you'll be able to step
                    // through both test code and the hosted function, even though they are in
                    // different processes.
                    this.logger.LogWarning("Found a process listening on {Port}. Is this a debug instance?", port);
                    throw new FunctionStartupException($"Found a process listening on {port}. Is this a debug instance?");
                }
            }
        }

        private async Task<string> GetToolPath()
        {
            string toolLocatorName = OperatingSystem.IsWindows() ? "where.exe" : "which";
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

            string[] toolPaths = toolLocator.StandardOutputText.Split("\n").Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();

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