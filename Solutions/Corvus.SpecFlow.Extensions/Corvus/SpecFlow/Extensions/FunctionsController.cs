// <copyright file="FunctionsController.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.SpecFlow.Extensions
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Corvus.Testing.AzureFunctions;
    using NUnit.Framework;

    using TechTalk.SpecFlow;

    /// <summary>
    /// Starts, manages, and terminates functions instances from within a SpecFlow context.
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
        private readonly Testing.AzureFunctions.FunctionsController functionsController;

        /// <summary>
        /// Creates a new FunctionsController for use with SpecFlow.
        /// </summary>
        public FunctionsController()
        {
            this.functionsController = new Corvus.Testing.AzureFunctions.FunctionsController();
        }

        /// <summary>
        /// Start a functions instance.
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
            FunctionConfiguration? functionConfiguration = null;
            scenarioContext?.TryGetValue(out functionConfiguration);

            if (functionConfiguration == null)
            {
                featureContext.TryGetValue(out functionConfiguration);
            }

            await this.functionsController.StartFunctionsInstance(
                TestContext.CurrentContext.TestDirectory,
                path,
                port,
                runtime,
                provider,
                functionConfiguration);
        }

        /// <summary>
        /// Tear down the running functions instances.
        /// <remarks>
        /// <para>Should be called from inside a "RunAndStoreExceptions"
        /// block to ensure any issues do not cause test cleanup to be abandoned.
        /// </para>
        /// </remarks>
        /// </summary>
        public void TeardownFunctions() => this.functionsController.TeardownFunctions();

        /// <summary>
        /// Provides access to the output.
        /// </summary>
        /// <returns>All output from the function host process.</returns>
        public IEnumerable<IProcessOutput> GetFunctionsOutput() => this.functionsController.GetFunctionsOutput();
    }
}
