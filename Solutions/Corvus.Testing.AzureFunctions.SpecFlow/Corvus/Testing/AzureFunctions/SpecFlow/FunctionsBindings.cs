// <copyright file="FunctionsBindings.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Testing.AzureFunctions.SpecFlow
{
    using System.Threading.Tasks;
    using Corvus.SpecFlow.Extensions;
    using Corvus.Testing.AzureFunctions;
    using NUnit.Framework;
    using TechTalk.SpecFlow;

    /// <summary>
    ///     Specflow bindings to support Azure Functions.
    /// </summary>
    [Binding]
    public class FunctionsBindings
    {
        private readonly ScenarioContext scenarioContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="FunctionsBindings"/> class.
        /// </summary>
        /// <param name="scenarioContext">The current scenario context.</param>
        public FunctionsBindings(ScenarioContext scenarioContext)
        {
            this.scenarioContext = scenarioContext;
        }

        /// <summary>
        /// Retrieves the current functions controller from the supplied context.
        /// </summary>
        /// <param name="context">The SpecFlow context to retrieve from.</param>
        /// <returns>The FunctionsController.</returns>
        /// <remarks>
        /// If the controller hasn't already been added to the context, this method will create
        /// and add a new instance.
        /// </remarks>
        public static FunctionsController GetFunctionsController(SpecFlowContext context)
        {
            if (!context.TryGetValue(out FunctionsController controller))
            {
                controller = new FunctionsController();
                context.Set(controller);
            }

            return controller;
        }

        /// <summary>
        /// Retrieves the <see cref="FunctionConfiguration"/> from the context.
        /// </summary>
        /// <param name="context">The context in which the configuration is stored.</param>
        /// <returns>The <see cref="FunctionConfiguration"/>.</returns>
        /// <remarks>
        /// If a <see cref="FunctionConfiguration"/> hasn't already been added to the context,
        /// this method will create and add a new instance.
        /// </remarks>
        public static FunctionConfiguration GetFunctionConfiguration(SpecFlowContext context)
        {
            if (!context.TryGetValue(out FunctionConfiguration value))
            {
                value = new FunctionConfiguration();
                context.Set(value);
            }

            return value;
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
            return GetFunctionsController(this.scenarioContext)
                .StartFunctionsInstance(TestContext.CurrentContext.TestDirectory, path, port, "netcoreapp3.1");
        }

        /// <summary>
        ///     Start a functions instance.
        /// </summary>
        /// <param name="path">The location of the functions project.</param>
        /// <param name="port">The port on which to start the functions instance.</param>
        /// <param name="runtime">The id of the runtime to use.</param>
        /// <returns>A task that completes once the function instance has started.</returns>
        [Given("I start a functions instance for the local project '(.*)' on port (.*) with runtime '(.*)'")]
        public Task StartAFunctionsInstance(string path, int port, string runtime)
        {
            FunctionConfiguration configuration = FunctionsBindings.GetFunctionConfiguration(this.scenarioContext);
            return GetFunctionsController(this.scenarioContext)
                .StartFunctionsInstance(
                    TestContext.CurrentContext.TestDirectory,
                    path,
                    port,
                    runtime,
                    "csharp",
                    configuration);
        }

        /// <summary>
        ///     Tear down the running functions instances for the scenario.
        /// </summary>
        [AfterScenario]
        public void TeardownFunctionsAfterScenario()
        {
            this.scenarioContext.RunAndStoreExceptions(
                GetFunctionsController(this.scenarioContext).TeardownFunctions);
        }
    }
}