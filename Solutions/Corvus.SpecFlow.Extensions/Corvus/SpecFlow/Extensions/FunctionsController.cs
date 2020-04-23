// <copyright file="FunctionsController.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.SpecFlow.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Management;
    using System.Threading.Tasks;

    using Corvus.SpecFlow.Extensions.Internal;
    using NUnit.Framework;

    using TechTalk.SpecFlow;

    /// <summary>
    /// Starts, manages and terminates functions instances for testing purposes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class supports a limited degree of thread safety: you can have multiple calls to
    /// <see cref="StartFunctionsInstance(FeatureContext, ScenarioContext, string, int, string, string)"/> in progress simultaneously for a single
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
        ///     Start a functions instance.
        /// </summary>
        /// <param name="featureContext">The current feature context.</param>
        /// <param name="scenarioContext">The current scenario context. Not required if using this class per-feature.</param>
        /// <param name="path">The location of the functions project.</param>
        /// <param name="port">The port on which to start the functions instance.</param>
        /// <param name="runtime">The runtime version, defaults to netcoreapp2.1.</param>
        /// <param name="provider">The functions provider. Defaults to csharp.</param>
        /// <returns>A task that completes once the function instance has started.</returns>
        public async Task StartFunctionsInstance(
            FeatureContext featureContext,
            ScenarioContext? scenarioContext,
            string path,
            int port,
            string runtime = "netcoreapp2.1",
            string provider = "csharp")
        {
            Console.WriteLine($"Starting a function instance for project {path} on port {port}");

            string directoryExtension = $"\\bin\\release\\{runtime}";

            string lowerInvariantCurrentDirectory = TestContext.CurrentContext.TestDirectory.ToLowerInvariant();
            if (lowerInvariantCurrentDirectory.Contains("debug"))
            {
                directoryExtension = $"\\bin\\debug\\{runtime}";
            }

            Console.WriteLine($"\tCurrent directory: {lowerInvariantCurrentDirectory}");

            string root = TestContext.CurrentContext.TestDirectory.Substring(
                0,
                TestContext.CurrentContext.TestDirectory.IndexOf(@"\Solutions\") + 11);

            Console.WriteLine($"\tRoot: {root}");

            string npmPrefix = await GetNpmPrefix().ConfigureAwait(false);
            string toolsFolder = Path.Combine(
                npmPrefix,
                @"node_modules\azure-functions-core-tools\bin");
            Assert.IsTrue(
                Directory.Exists(toolsFolder),
                $"Azure Functions runtime not found at {toolsFolder}. Have you run: 'npm install -g azure-functions-core-tools --unsafe-perm true'?");
            string toolPath = Path.Combine(
                toolsFolder,
                "func");

            Console.WriteLine($"\tToolsPath: {toolPath}");

            Console.WriteLine($"\tStarting process");

            var startInfo = new ProcessStartInfo(toolPath, $"host start --port {port} --{provider}")
            {
                WorkingDirectory = root + path + directoryExtension,
                UseShellExecute = false,
                CreateNoWindow = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                WindowStyle = ProcessWindowStyle.Normal,
            };

            FunctionConfiguration? functionConfiguration = null;
            scenarioContext?.TryGetValue(out functionConfiguration);

            if (functionConfiguration == null)
            {
                featureContext.TryGetValue(out functionConfiguration);
            }

            if (functionConfiguration != null)
            {
                foreach (KeyValuePair<string, string> kvp in functionConfiguration.EnvironmentVariables)
                {
                    startInfo.EnvironmentVariables[kvp.Key] = kvp.Value;
                }
            }

            var bufferHandler = new FunctionOutputBufferHandler(startInfo);

            lock (this.sync)
            {
                this.output.Add(bufferHandler);
            }

            Console.WriteLine($"\tProcess started; waiting for initialisation to complete");

            await Task.WhenAny(
                bufferHandler.JobHostStarted,
                bufferHandler.ExitCode,
                Task.Delay(TimeSpan.FromSeconds(StartupTimeout))).ConfigureAwait(false);
            if (bufferHandler.ExitCode.IsCompleted)
            {
                int exitCode = await bufferHandler.ExitCode.ConfigureAwait(false);
                Assert.Fail($"Function host process terminated unexpectedly with exit code {exitCode}. Error output: {bufferHandler.StandardErrorText}");
            }
            else if (!bufferHandler.JobHostStarted.IsCompleted)
            {
                Assert.Fail("Timed out while starting functions instance.");
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("\tStarted");
            }
        }

        /// <summary>
        /// Provides access to the output.
        /// </summary>
        public IEnumerable<IProcessOutput> GetFunctionsOutput()
        {
            return this.output.AsReadOnly();
        }

        /// <summary>
        /// Tear down the running functions instances. Should be called from inside a "RunAndStoreExceptions"
        /// block to ensure any issues do not cause test cleanup to be abandoned.
        /// </summary>
        public void TeardownFunctions()
        {
            var aggregate = new List<Exception>();
            foreach (FunctionOutputBufferHandler outputHandler in this.output)
            {
                try
                {
                    DateTimeOffset killTime = DateTimeOffset.Now;
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

        /// <summary>
        ///     Kill a process, and all of its children, grandchildren, etc.
        /// </summary>
        /// <param name="pid">Process ID.</param>
        private static void KillProcessAndChildren(int pid)
        {
            // Cannot close 'system idle process'.
            if (pid == 0)
            {
                return;
            }

            using (var searcher = new ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessID=" + pid))
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
        ///     Discover npm's global prefix (the parent of its global module cache location).
        /// </summary>
        /// <returns>
        ///     The global prefix reported by npm.
        /// </returns>
        /// <remarks>
        /// <para>
        ///     To run Azure Functions locally in tests, we need to run the <c>func</c> command
        ///     from the <c>azure-functions-core-tools</c> npm package.
        /// </para>
        /// <para>
        ///     Unfortunately, npm ends up putting this in different places on different machines.
        ///     Debugging locally, and also on private build agents, the global module cache is
        ///     typically in <c>%APPDATA%\npm\npm_modules</c>. However, on hosted build agents it
        ///     currently resides in <c>c:\npm\prefix</c>.
        /// </para>
        /// <para>
        ///     The most dependable way to find where npm puts these things is to ask npm, by
        ///     running the command <c>npm prefix -g</c>, which is what this function does.
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

            await Task.WhenAny(
                processHandler.ExitCode,
                Task.Delay(TimeSpan.FromSeconds(10))).ConfigureAwait(false);

            if (!processHandler.ExitCode.IsCompleted)
            {
                Assert.Fail(
                    "npm task did not exit before timeout. Stdout: {0}. Stderr: {1}",
                    processHandler.StandardOutputText,
                    processHandler.StandardErrorText);
            }

            processHandler.EnsureComplete();

            if (processHandler.Process.ExitCode != 0)
            {
                Assert.Fail("Unable to run npm: {0}", processHandler.StandardErrorText);
            }

            // We get a newline character on the end of the standard output, so we need to
            // trim before returning.
            return processHandler.StandardOutputText.Trim();
        }
    }
}
