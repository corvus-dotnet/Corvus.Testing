// <copyright file="DemoFunctionPerFeatureHooks.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.SpecFlow.Extensions.Demo.AzureFunctionsTesting
{
    using System.Threading.Tasks;
    using Corvus.SpecFlow.Extensions;
    using TechTalk.SpecFlow;
    using Corvus.Testing.AzureFunctions;
    using Corvus.Testing.AzureFunctions.Internal;

    [Binding]
    public static class DemoFunctionPerFeatureHooks
    {
        [BeforeFeature("usingDemoFunctionPerFeature")]
        public static Task StartFunctionsAsync(FeatureContext featureContext)
        {
            var functionsController = new FunctionsController();
            featureContext.Set(functionsController);

            return functionsController.StartFunctionsInstance(
                featureContext,
                null,
                "Corvus.SpecFlow.Extensions.DemoFunction",
                7075,
                "netcoreapp3.1");
        }

        [BeforeFeature("usingDemoFunctionPerFeatureWithAdditionalConfiguration")]
        public static Task StartFunctionWithAdditionalConfigurationAsync(FeatureContext featureContext)
        {
            var functionConfiguration = new FunctionConfiguration();
            functionConfiguration.EnvironmentVariables.Add("ResponseMessage", "Welcome, {name}");
            featureContext.Set(functionConfiguration);

            return StartFunctionsAsync(featureContext);
        }

        [AfterScenario("usingDemoFunctionPerFeature", "usingDemoFunctionPerFeatureWithAdditionalConfiguration")]
        public static void WriteOutput(FeatureContext featureContext)
        {
            FunctionsController functionsController = featureContext.Get<FunctionsController>();
            functionsController.GetFunctionsOutput().WriteAllToConsoleAndClear();
        }

        [AfterFeature("usingDemoFunctionPerFeature", "usingDemoFunctionPerFeatureWithAdditionalConfiguration")]
        public static void StopFunction(FeatureContext featureContext)
        {
            FunctionsController functionsController = featureContext.Get<FunctionsController>();
            functionsController.TeardownFunctions();
        }
    }
}
