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
        [BeforeFeature("usingInProcessDemoFunctionPerFeature")]
        public static Task StartInProcessFunctionsAsync(FeatureContext featureContext)
        {
            FunctionsController functionsController = FunctionsBindings.GetFunctionsController(featureContext);
            FunctionConfiguration functionConfiguration = FunctionsBindings.GetFunctionConfiguration(featureContext);

            return functionsController.StartFunctionsInstance(
                "Corvus.Testing.AzureFunctions.DemoFunction.InProcess",
                7075,
                "net6.0",
                configuration: functionConfiguration);
        }

        [BeforeFeature("usingIsolatedDemoFunctionPerFeature")]
        public static Task StartIsolatedFunctionsAsync(FeatureContext featureContext)
        {
            FunctionsController functionsController = FunctionsBindings.GetFunctionsController(featureContext);
            FunctionConfiguration functionConfiguration = FunctionsBindings.GetFunctionConfiguration(featureContext);

            return functionsController.StartFunctionsInstance(
                "Corvus.Testing.AzureFunctions.DemoFunctions.Isolated",
                7075,
                "net8.0",
                configuration: functionConfiguration);
        }

        [BeforeFeature("usingInProcessDemoFunctionPerFeatureWithAdditionalConfiguration")]
        public static Task StartInProcessFunctionWithAdditionalConfigurationAsync(FeatureContext featureContext)
        {
            FunctionConfiguration functionConfiguration = FunctionsBindings.GetFunctionConfiguration(featureContext);
            functionConfiguration.EnvironmentVariables.Add("ResponseMessage", "Welcome, {name}");

            return StartInProcessFunctionsAsync(featureContext);
        }

        [BeforeFeature("usingIsolatedDemoFunctionPerFeatureWithAdditionalConfiguration")]
        public static Task StartIsolatedFunctionWithAdditionalConfigurationAsync(FeatureContext featureContext)
        {
            FunctionConfiguration functionConfiguration = FunctionsBindings.GetFunctionConfiguration(featureContext);
            functionConfiguration.EnvironmentVariables.Add("ResponseMessage", "Welcome, {name}");

            return StartIsolatedFunctionsAsync(featureContext);
        }

        [AfterScenario("usingInProcessDemoFunctionPerFeature", "usingIsolatedDemoFunctionPerFeature", "usingDemoFunctionPerFeatureWithAdditionalConfiguration")]
        public static void WriteOutput(FeatureContext featureContext)
        {
            FunctionsController functionsController = FunctionsBindings.GetFunctionsController(featureContext);
            featureContext.Get<ILogger>().LogAllAndClear(functionsController.GetFunctionsOutput());
        }

        [AfterFeature(
            "usingInProcessDemoFunctionPerFeature",
            "usingIsolatedDemoFunctionPerFeature",
            "usingInProcessDemoFunctionPerFeatureWithAdditionalConfiguration",
            "usingIsolatedDemoFunctionPerFeatureWithAdditionalConfiguration")]
        public static void StopFunction(FeatureContext featureContext)
        {
            FunctionsController functionsController = FunctionsBindings.GetFunctionsController(featureContext);
            functionsController.TeardownFunctions();
        }
    }
}
