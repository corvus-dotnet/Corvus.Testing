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
    using System.Management;
    using System.Net.NetworkInformation;
    using System.Threading.Tasks;
    using Corvus.Testing.AzureFunctions.Internal;

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
            if (IsSomethingAlreadyListeningOn(port))
            {
                Console.WriteLine($"Found a process listening on {port}. Is this a debug instance?");
                Console.WriteLine("This test run will reuse this process, and so may produce unexpected results.");
                return;
            }

            Console.WriteLine($"Starting a function instance for project {path} on port {port}");
            Console.WriteLine("\tStarting process");

            FunctionOutputBufferHandler bufferHandler = StartFunctionHostProcess(
                port,
                provider,
                await GetToolPath(),
                FunctionProject.ResolvePath(path, runtime),
                configuration);

            lock (this.sync)
            {
                this.output.Add(bufferHandler);
            }

            Console.WriteLine("\tProcess started; waiting for initialisation to complete");

            await Task.WhenAny(
                bufferHandler.JobHostStarted,
                bufferHandler.ExitCode,
                Task.Delay(TimeSpan.FromSeconds(StartupTimeout))).ConfigureAwait(false);

            if (bufferHandler.ExitCode.IsCompleted)
            {
                int exitCode = await bufferHandler.ExitCode.ConfigureAwait(false);
                throw new FunctionStartupException(
                    $"Function host process terminated unexpectedly with exit code {exitCode}.",
                    stderr: bufferHandler.StandardErrorText);
            }

            if (!bufferHandler.JobHostStarted.IsCompleted)
            {
                throw new FunctionStartupException("Timed out while starting functions instance.");
            }

            Console.WriteLine();
            Console.WriteLine("\tStarted");
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

            this.output.WriteAllToConsoleAndClear();

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

            using (var searcher =
                new ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessID=" + pid))
            {
                foreach (ManagementObject mo in searcher.Get())
                {
                    KillProcessAndChildren(Convert.ToInt32(mo["ProcessID"]));
                }
            }

            try
            {
                var proc = Process.GetProcessById(pid);
                proc.Kill();
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

        private static FunctionOutputBufferHandler StartFunctionHostProcess(
            int port,
            string provider,
            string toolPath,
            string workingDirectory,
            FunctionConfiguration? functionConfiguration)
        {
            var startInfo = new ProcessStartInfo(toolPath, $"host start --port {port} --{provider}")
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

            var processHandler = new FunctionOutputBufferHandler(startInfo);
            processHandler.Start();

            return processHandler;
        }
    }
}