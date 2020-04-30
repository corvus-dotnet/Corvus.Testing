// <copyright file="DemoFunctionPerScenarioHooks.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.SpecFlow.Extensions.Demo.AzureFunctionsTesting
{
    using System.Threading.Tasks;
    using Corvus.Testing.AzureFunctions;
    using TechTalk.SpecFlow;
    using FunctionsController = Corvus.SpecFlow.Extensions.FunctionsController;

    [Binding]
    public class DemoFunctionPerScenarioHooks
    {
        [BeforeScenario("usingDemoFunctionPerScenario")]
        public Task StartFunctionsAsync(FeatureContext featureContext, ScenarioContext scenarioContext)
        {
            var functionsController = new FunctionsController();
            scenarioContext.Set(functionsController);

            return functionsController.StartFunctionsInstance(
                featureContext,
                scenarioContext,
                "Corvus.SpecFlow.Extensions.DemoFunction",
                7075,
                "netcoreapp3.1");
        }

        [BeforeScenario("usingDemoFunctionPerScenarioWithAdditionalConfiguration")]
        public Task StartFunctionWithAdditionalConfigurationAsync(FeatureContext featureContext, ScenarioContext scenarioContext)
        {
            var functionConfiguration = new FunctionConfiguration();
            functionConfiguration.EnvironmentVariables.Add("ResponseMessage", "Welcome, {name}");
            scenarioContext.Set(functionConfiguration);

            return this.StartFunctionsAsync(featureContext, scenarioContext);
        }

        [AfterScenario("usingDemoFunctionPerScenario", "usingDemoFunctionPerScenarioWithAdditionalConfiguration")]
        public void StopFunction(ScenarioContext scenarioContext)
        {
            FunctionsController functionsController = scenarioContext.Get<FunctionsController>();
            functionsController.TeardownFunctions();
        }
    }
}
