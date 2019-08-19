// <copyright file="FunctionsBindings.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.SpecFlow.Bindings
{
    using System.Threading.Tasks;

    using TechTalk.SpecFlow;

    /// <summary>
    ///     Specflow bindings to support Azure Functions.
    /// </summary>
    [Binding]
    public class FunctionsBindings
    {
        private readonly FunctionsController functionsController;
        private readonly FeatureContext featureContext;
        private readonly ScenarioContext scenarioContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="FunctionsBindings"/> class.
        /// </summary>
        /// <param name="featureContext">The current feature context.</param>
        /// <param name="scenarioContext">The current scenario context.</param>
        public FunctionsBindings(FeatureContext featureContext, ScenarioContext scenarioContext)
        {
            this.functionsController = new FunctionsController();
            this.featureContext = featureContext;
            this.scenarioContext = scenarioContext;
        }

        /// <summary>
        ///     Start a functions instance.
        /// </summary>
        /// <param name="path">The location of the functions project.</param>
        /// <param name="port">The port on which to start the functions instance.</param>
        /// <returns>A task that completes once the function instance has started.</returns>
        [Given("I start a functions instance for the local project '(.*)' on port (.*)")]
        public Task StartAFunctionsInstance(string path, int port)
        {
            return this.functionsController.StartFunctionsInstance(this.featureContext, this.scenarioContext, path, port);
        }

        /// <summary>
        ///     Tear down the running functions instances for the scenario.
        /// </summary>
        [AfterScenario]
        public void TeardownFunctionsAfterScenario()
        {
            this.scenarioContext.RunAndStoreExceptions(this.functionsController.TeardownFunctions);
        }
    }
}