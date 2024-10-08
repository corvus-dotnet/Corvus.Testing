// <copyright file="TeardownExceptionsBindings.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Testing.ReqnRoll
{
    using System;
    using System.Collections.Generic;
    using Reqnroll;

    /// <summary>
    /// Rethrows any exceptions detected by <see cref="TeardownExceptions"/>.
    /// </summary>
    [Binding]
    public static class TeardownExceptionsBindings
    {
        private const string TeardownBindingPhaseKey = "TeardownBindingPhase";

        /// <summary>
        /// Sets a flag in the feature context to confirm that rethrow support is available.
        /// </summary>
        /// <param name="featureContext">ReqnRoll feature context.</param>
        [AfterFeature(Order = 0)]
        public static void RethrowFeatureBindingAvailableValidation(FeatureContext featureContext)
        {
            featureContext.Set(true, TeardownBindingPhaseKey);
        }

        /// <summary>
        /// Sets a flag in the scenario context to confirm that rethrow support is available.
        /// </summary>
        /// <param name="scenarioContext">ReqnRoll scenario context.</param>
        [AfterScenario(Order = 0)]
        public static void RethrowScenarioBindingAvailableValidation(ScenarioContext scenarioContext)
        {
            scenarioContext.Set(true, TeardownBindingPhaseKey);
        }

        /// <summary>
        /// Detects whether any feature teardown exceptions were detected by <see cref="TeardownExceptions"/>,
        /// and rethrows them.
        /// </summary>
        /// <param name="featureContext">ReqnRoll feature context.</param>
        [AfterFeature(Order = int.MaxValue)]
        public static void RethrowFeatureTeardownExceptions(FeatureContext featureContext)
            => RethrowTeardownExceptions(featureContext, TeardownExceptions.FeatureContextKey);

        /// <summary>
        /// Detects whether any scenario teardown exceptions were detected by <see cref="TeardownExceptions"/>,
        /// and rethrows them.
        /// </summary>
        /// <param name="scenarioContext">ReqnRoll scenario context.</param>
        [AfterScenario(Order = int.MaxValue)]
        public static void RethrowScenarioTeardownExceptions(ScenarioContext scenarioContext)
            => RethrowTeardownExceptions(scenarioContext, TeardownExceptions.ScenarioContextKey);

        /// <summary>
        /// Called by helpers that only work if this binding is in place.
        /// </summary>
        /// <param name="context">The ReqnRoll context.</param>
        internal static void VerifyBindingAvailable(ReqnrollContext context)
        {
            if (!context.TryGetValue(TeardownBindingPhaseKey, out bool exceptionsNotYetRethrown))
            {
                throw new InvalidOperationException(
                    $"This method requires {typeof(TeardownExceptionsBindings).FullName} to be registered with ReqnRoll");
            }

            if (!exceptionsNotYetRethrown)
            {
                throw new InvalidOperationException(
                    "This method must be called during BeforeFeature bindings with an Order < int.MaxValue");
            }
        }

        private static void RethrowTeardownExceptions(ReqnrollContext context, string key)
        {
            context.Set(false, TeardownBindingPhaseKey);

            if (context.TryGetValue(key, out List<Exception> elist))
            {
                throw new AggregateException(elist);
            }
        }
    }
}
