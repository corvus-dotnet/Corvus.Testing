// <copyright file="ChildObjectBindings.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Testing.ReqnRoll
{
    using Corvus.Testing.ReqnRoll.Internal;
    using Reqnroll;
    using Reqnroll.Assist;

    /// <summary>
    /// Provides ReqnRoll bindings for retrieving child objects from the scenario context.
    /// </summary>
    [Binding]
    public static class ChildObjectBindings
    {
        private const string ChildObjectValueRetrieverKey = "ChildObjectValueRetriever";

        /// <summary>
        /// Adds the child object binding retriever.
        /// </summary>
        /// <param name="scenarioContext">The ambient scenario context.</param>
        [BeforeScenario("@useChildObjects", Order = 100)]
        public static void SetupValueRetrievers(ScenarioContext scenarioContext)
        {
            var instance = new ChildObjectValueRetriever(scenarioContext);
            scenarioContext.Set(instance, ChildObjectValueRetrieverKey);
            Service.Instance.ValueRetrievers.Register(instance);
        }

        /// <summary>
        /// Removes the child object binding retriever.
        /// </summary>
        /// <param name="scenarioContext">The ambient scenario context.</param>
        [AfterScenario("@useChildObjects", Order = 100)]
        public static void TearDownValueRetrievers(ScenarioContext scenarioContext)
        {
            scenarioContext.RunAndStoreExceptions(() =>
                Service.Instance.ValueRetrievers.Unregister(scenarioContext.Get<ChildObjectValueRetriever>(ChildObjectValueRetrieverKey)));
        }
    }
}
