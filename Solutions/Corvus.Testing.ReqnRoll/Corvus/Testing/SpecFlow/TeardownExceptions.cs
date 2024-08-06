// <copyright file="TeardownExceptions.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Testing.ReqnRoll
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Reqnroll;

    /// <summary>
    /// Enables exceptions thrown during feature and scenario teardown to be reported while still
    /// enabling any other pending teardowns to execute.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ReqnRoll has an unfortunate problem with exceptions in 'After' bindings. If one
    /// of them throws an exception, that prevents any other ones from running. However,
    /// the only way to report errors at this stage of execution is to let them out of the
    /// binding.
    /// </para>
    /// <para>
    /// We want to be able to run all applicable After bindings even if some fail, and we
    /// also want to report all failures. To do this, we catch failures, put them in the
    /// relevant ReqnRoll context, and then rethrow them later in the various bindings in
    /// <see cref="TeardownExceptionsBindings"/>.
    /// These have an Order of int.MaxValue, meaning that they will run after any other teardown handlers.
    /// This way, we can report all errors that occur, without blocking any teardown methods.
    /// </para>
    /// </remarks>
    public static class TeardownExceptions
    {
        /// <summary>
        /// The key by which we store the <see cref="List{Exception}"/> containing any exceptions thrown
        /// during feature teardown.
        /// </summary>
        internal const string FeatureContextKey = nameof(TeardownExceptions) + ".Feature.Errors";

        /// <summary>
        /// The key by which we store the <see cref="List{Exception}"/> containing any exceptions thrown
        /// during scenario teardown.
        /// </summary>
        internal const string ScenarioContextKey = nameof(TeardownExceptions) + ".Scenario.Errors";

        /// <summary>
        /// Runs a method inside an exception handler that records any exception for later
        /// reporting by <see cref="TeardownExceptionsBindings.RethrowFeatureTeardownExceptions(FeatureContext)"/>.
        /// </summary>
        /// <param name="featureContext">The ReqnRoll feature context.</param>
        /// <param name="action">The method to run.</param>
        public static void RunAndStoreExceptions(this FeatureContext featureContext, Action action)
            => RunAndStoreExceptions(featureContext, action, FeatureContextKey);

        /// <summary>
        /// Runs a method inside an exception handler that records any exception for later
        /// reporting by <see cref="TeardownExceptionsBindings.RethrowFeatureTeardownExceptions(FeatureContext)"/>.
        /// </summary>
        /// <param name="featureContext">The ReqnRoll feature context.</param>
        /// <param name="action">The method to run.</param>
        /// <returns>A task that completes once the work is complete. Note that this will report success even
        /// if the underlying action fails (because that's the point of this method).</returns>
        public static Task RunAndStoreExceptionsAsync(this FeatureContext featureContext, Func<Task> action)
            => RunAndStoreExceptionsAsync(featureContext, action, FeatureContextKey);

        /// <summary>
        /// Runs a method inside an exception handler that records any exception for later
        /// reporting by <see cref="TeardownExceptionsBindings.RethrowScenarioTeardownExceptions(ScenarioContext)"/>.
        /// </summary>
        /// <param name="scenarioContext">The ReqnRoll scenario context.</param>
        /// <param name="action">The method to run.</param>
        public static void RunAndStoreExceptions(this ScenarioContext scenarioContext, Action action)
            => RunAndStoreExceptions(scenarioContext, action, ScenarioContextKey);

        /// <summary>
        /// Runs a method inside an exception handler that records any exception for later
        /// reporting by <see cref="TeardownExceptionsBindings.RethrowScenarioTeardownExceptions(ScenarioContext)"/>.
        /// </summary>
        /// <param name="scenarioContext">The ReqnRoll scenario context.</param>
        /// <param name="action">The method to run.</param>
        /// <returns>A task that completes once the work is complete. Note that this will report success even
        /// if the underlying action fails (because that's the point of this method).</returns>
        public static Task RunAndStoreExceptionsAsync(this ScenarioContext scenarioContext, Func<Task> action)
            => RunAndStoreExceptionsAsync(scenarioContext, action, ScenarioContextKey);

        private static void RunAndStoreExceptions(this ReqnrollContext context, Action action, string key)
        {
            TeardownExceptionsBindings.VerifyBindingAvailable(context);
            try
            {
                action();
            }
            catch (Exception x)
            {
                StoreExceptionInContext(context, x, key);
            }
        }

        private static async Task RunAndStoreExceptionsAsync(this ReqnrollContext context, Func<Task> action, string key)
        {
            TeardownExceptionsBindings.VerifyBindingAvailable(context);
            try
            {
                await action().ConfigureAwait(false);
            }
            catch (Exception x)
            {
                StoreExceptionInContext(context, x, key);
            }
        }

        private static void StoreExceptionInContext(ReqnrollContext context, Exception x, string key)
        {
            if (!context.TryGetValue(key, out List<Exception> elist))
            {
                elist = new List<Exception>();
                context.Add(key, elist);
            }

            elist.Add(x);
        }
    }
}
