// <copyright file="DemoFunctionPerScenarioHooks.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Testing.SpecFlow.Demo.AzureFunctionsTesting
{
    using System.Threading.Tasks;

    using Corvus.Testing.AzureFunctions;
    using Corvus.Testing.AzureFunctions.SpecFlow;

    using TechTalk.SpecFlow;

    [Binding]
    public static class DemoFunctionPerScenarioHooks
    {
        [BeforeScenario("usingDemoFunctionPerScenario")]
        public static Task StartFunctionsAsync(ScenarioContext scenarioContext)
        {
            FunctionsController functionsController = FunctionsBindings.GetFunctionsController(scenarioContext);
            FunctionConfiguration functionConfiguration = FunctionsBindings.GetFunctionConfiguration(scenarioContext);

            return functionsController.StartFunctionsInstance(
                "Corvus.Testing.AzureFunctions.DemoFunction",
                7075,
                "net6.0",
                configuration: functionConfiguration);
        }

        [BeforeScenario("usingDemoFunctionPerScenarioWithAdditionalConfiguration")]
        public static Task StartFunctionWithAdditionalConfigurationAsync(ScenarioContext scenarioContext)
        {
            FunctionConfiguration functionConfiguration = FunctionsBindings.GetFunctionConfiguration(scenarioContext);
            functionConfiguration.EnvironmentVariables.Add("ResponseMessage", "Welcome, {name}");

            return StartFunctionsAsync(scenarioContext);
        }

        [AfterScenario("usingDemoFunctionPerScenario", "usingDemoFunctionPerScenarioWithAdditionalConfiguration")]
        public static void StopFunction(ScenarioContext scenarioContext)
        {
            FunctionsController functionsController = FunctionsBindings.GetFunctionsController(scenarioContext);
            functionsController.TeardownFunctions();
        }
    }
}
