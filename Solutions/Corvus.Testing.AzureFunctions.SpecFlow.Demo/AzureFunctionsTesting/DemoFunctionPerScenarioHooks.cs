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
        [BeforeScenario("usingInProcessDemoFunctionPerScenario")]
        public static Task StartInProcessFunctionsAsync(ScenarioContext scenarioContext)
        {
            FunctionsController functionsController = FunctionsBindings.GetFunctionsController(scenarioContext);
            FunctionConfiguration functionConfiguration = FunctionsBindings.GetFunctionConfiguration(scenarioContext);

            return functionsController.StartFunctionsInstance(
                "Corvus.Testing.AzureFunctions.DemoFunction.InProcess",
                7075,
                "net6.0",
                configuration: functionConfiguration);
        }

        [BeforeScenario("usingIsolatedDemoFunctionPerScenario")]
        public static Task StartIsolatedFunctionsAsync(ScenarioContext scenarioContext)
        {
            FunctionsController functionsController = FunctionsBindings.GetFunctionsController(scenarioContext);
            FunctionConfiguration functionConfiguration = FunctionsBindings.GetFunctionConfiguration(scenarioContext);

            return functionsController.StartFunctionsInstance(
                "Corvus.Testing.AzureFunctions.DemoFunctions.Isolated",
                7075,
                "net8.0",
                configuration: functionConfiguration);
        }

        [BeforeScenario("usingInProcessDemoFunctionPerScenarioWithAdditionalConfiguration")]
        public static Task StartInProcessFunctionWithAdditionalConfigurationAsync(ScenarioContext scenarioContext)
        {
            DemoFunctionConfig.SetupTestConfig(scenarioContext);
            return StartIsolatedFunctionsAsync(scenarioContext);
        }

        [BeforeScenario("usingIsolatedDemoFunctionPerScenarioWithAdditionalConfiguration")]
        public static Task StartIsolatedFunctionWithAdditionalConfigurationAsync(ScenarioContext scenarioContext)
        {
            DemoFunctionConfig.SetupTestConfig(scenarioContext);
            return StartIsolatedFunctionsAsync(scenarioContext);
        }

        [AfterScenario(
            "usingInProcessDemoFunctionPerScenario",
            "usingIsolatedDemoFunctionPerScenario",
            "usingInProcessDemoFunctionPerScenarioWithAdditionalConfiguration",
            "usingIsolatedDemoFunctionPerScenarioWithAdditionalConfiguration")]
        public static void StopFunction(ScenarioContext scenarioContext)
        {
            FunctionsController functionsController = FunctionsBindings.GetFunctionsController(scenarioContext);
            functionsController.TeardownFunctions();
        }
    }
}
