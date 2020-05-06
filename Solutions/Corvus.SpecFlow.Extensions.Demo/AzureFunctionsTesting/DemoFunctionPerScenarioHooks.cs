// <copyright file="DemoFunctionPerScenarioHooks.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.SpecFlow.Extensions.Demo.AzureFunctionsTesting
{
    using System.Threading.Tasks;
    using Corvus.Testing.AzureFunctions;
    using NUnit.Framework;
    using TechTalk.SpecFlow;

    [Binding]
    public class DemoFunctionPerScenarioHooks
    {
        [BeforeScenario("usingDemoFunctionPerScenario")]
        public Task StartFunctionsAsync(ScenarioContext scenarioContext)
        {
            FunctionsController functionsController = FunctionsBindings.GetFunctionsController(scenarioContext);
            FunctionConfiguration functionConfiguration = FunctionsBindings.GetFunctionConfiguration(scenarioContext);

            return functionsController.StartFunctionsInstance(
                TestContext.CurrentContext.TestDirectory,
                "Corvus.SpecFlow.Extensions.DemoFunction",
                7075,
                "netcoreapp3.1",
                configuration: functionConfiguration);
        }

        [BeforeScenario("usingDemoFunctionPerScenarioWithAdditionalConfiguration")]
        public Task StartFunctionWithAdditionalConfigurationAsync(ScenarioContext scenarioContext)
        {
            FunctionConfiguration functionConfiguration = FunctionsBindings.GetFunctionConfiguration(scenarioContext);
            functionConfiguration.EnvironmentVariables.Add("ResponseMessage", "Welcome, {name}");

            return this.StartFunctionsAsync(scenarioContext);
        }

        [AfterScenario("usingDemoFunctionPerScenario", "usingDemoFunctionPerScenarioWithAdditionalConfiguration")]
        public void StopFunction(ScenarioContext scenarioContext)
        {
            FunctionsController functionsController = FunctionsBindings.GetFunctionsController(scenarioContext);
            functionsController.TeardownFunctions();
        }
    }
}
