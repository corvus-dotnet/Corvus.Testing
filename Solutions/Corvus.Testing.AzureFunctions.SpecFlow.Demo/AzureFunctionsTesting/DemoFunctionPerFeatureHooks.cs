// <copyright file="DemoFunctionPerFeatureHooks.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Testing.SpecFlow.Demo.AzureFunctionsTesting
{
    using System.Threading.Tasks;

    using Corvus.Testing.AzureFunctions;
    using Corvus.Testing.AzureFunctions.SpecFlow;

    using Microsoft.Extensions.Logging;

    using TechTalk.SpecFlow;

    [Binding]
    public static class DemoFunctionPerFeatureHooks
    {
        [BeforeFeature("usingDemoFunctionPerFeature")]
        public static Task StartFunctionsAsync(FeatureContext featureContext)
        {
            FunctionsController functionsController = FunctionsBindings.GetFunctionsController(featureContext);
            FunctionConfiguration functionConfiguration = FunctionsBindings.GetFunctionConfiguration(featureContext);

            return functionsController.StartFunctionsInstance(
                "Corvus.Testing.AzureFunctions.DemoFunction.InProcess",
                7075,
                "net6.0",
                configuration: functionConfiguration);
        }

        [BeforeFeature("usingDemoFunctionPerFeatureWithAdditionalConfiguration")]
        public static Task StartFunctionWithAdditionalConfigurationAsync(FeatureContext featureContext)
        {
            FunctionConfiguration functionConfiguration = FunctionsBindings.GetFunctionConfiguration(featureContext);
            functionConfiguration.EnvironmentVariables.Add("ResponseMessage", "Welcome, {name}");

            return StartFunctionsAsync(featureContext);
        }

        [AfterScenario("usingDemoFunctionPerFeature", "usingDemoFunctionPerFeatureWithAdditionalConfiguration")]
        public static void WriteOutput(FeatureContext featureContext)
        {
            FunctionsController functionsController = FunctionsBindings.GetFunctionsController(featureContext);
            featureContext.Get<ILogger>().LogAllAndClear(functionsController.GetFunctionsOutput());
        }

        [AfterFeature("usingDemoFunctionPerFeature", "usingDemoFunctionPerFeatureWithAdditionalConfiguration")]
        public static void StopFunction(FeatureContext featureContext)
        {
            FunctionsController functionsController = FunctionsBindings.GetFunctionsController(featureContext);
            functionsController.TeardownFunctions();
        }
    }
}
