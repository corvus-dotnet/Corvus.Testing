// <copyright file="ContainerBindings.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Testing.ReqnRoll
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;
    using Reqnroll;

    /// <summary>
    /// Provides ReqnRoll bindings for DI Composition.
    /// </summary>
    [Binding]
    public static class ContainerBindings
    {
        private const string ServiceCollectionKey = "ContainerBindings.ServiceCollection";
        private const string ContainerBindingPhaseKey = "ContainerBindings.ContainerBindingPhase";
        private const string ServiceProviderKey = "ContainerBindings.ServiceProvider";

        /// <summary>
        /// Add DI services to the per-feature container.
        /// </summary>
        /// <param name="featureContext">The ReqnRoll feature context.</param>
        /// <param name="configure">Callback that will be invoked with the service collection.</param>
        /// <remarks>
        /// Call this from a <c>BeforeFeature</c> binding. You must specify an <c>Order</c> that is
        /// greater than or equal to <see cref="ContainerBeforeFeatureOrder.PopulateServiceCollection"/>
        /// and less than <see cref="ContainerBeforeFeatureOrder.BuildServiceProvider"/>.
        /// </remarks>
        public static void ConfigureServices(
            FeatureContext featureContext,
            Action<IServiceCollection> configure)
        {
            ConfigureServices(
                featureContext,
                configure,
                $"This method must be called during BeforeFeature bindings with an Order >= {nameof(ContainerBeforeFeatureOrder)}.{nameof(ContainerBeforeFeatureOrder.PopulateServiceCollection)} and < {nameof(ContainerBeforeFeatureOrder)}.{nameof(ContainerBeforeFeatureOrder.BuildServiceProvider)}");
        }

        /// <summary>
        /// Add DI services to the per-scenario container.
        /// </summary>
        /// <param name="scenarioContext">The ReqnRoll scenario context.</param>
        /// <param name="configure">Callback that will be invoked with the service collection.</param>
        /// <remarks>
        /// Call this from a <c>BeforeScenario</c> binding. You must specify an <c>Order</c> that is
        /// greater than or equal to <see cref="ContainerBeforeScenarioOrder.PopulateServiceCollection"/>
        /// and less than <see cref="ContainerBeforeScenarioOrder.BuildServiceProvider"/>.
        /// </remarks>
        public static void ConfigureServices(
            ScenarioContext scenarioContext,
            Action<IServiceCollection> configure)
        {
            ConfigureServices(
                scenarioContext,
                configure,
                $"This method must be called during BeforeScenario bindings with an Order >= {nameof(ContainerBeforeScenarioOrder)}.{nameof(ContainerBeforeScenarioOrder.PopulateServiceCollection)} and < {nameof(ContainerBeforeScenarioOrder)}.{nameof(ContainerBeforeScenarioOrder.BuildServiceProvider)}");
        }

        /// <summary>
        /// Gets the <see cref="IServiceProvider"/> for the specified feature context.
        /// </summary>
        /// <param name="featureContext">The ReqnRoll context.</param>
        /// <returns>The service provider.</returns>
        /// <remarks>
        /// You can call this either during execution of steps, or in a <c>BeforeFeature</c> binding with
        /// an <c>Order</c> greater than or equal to <see cref="ContainerBeforeFeatureOrder.ServiceProviderAvailable"/>.
        /// </remarks>
        public static IServiceProvider GetServiceProvider(FeatureContext featureContext) => GetServiceProvider(
            featureContext,
            $"This method must be called either during Step execution, or in a BeforeFeature bindings with an Order >= {nameof(ContainerBeforeFeatureOrder)}.{nameof(ContainerBeforeFeatureOrder.ServiceProviderAvailable)}");

        /// <summary>
        /// Gets the <see cref="IServiceProvider"/> for the specified scenario context.
        /// </summary>
        /// <param name="scenarioContext">The ReqnRoll context.</param>
        /// <returns>The service provider.</returns>
        /// <remarks>
        /// You can call this either during execution of steps, or in a <c>BeforeScenario</c> binding with
        /// an <c>Order</c> greater than or equal to <see cref="ContainerBeforeScenarioOrder.ServiceProviderAvailable"/>.
        /// </remarks>
        public static IServiceProvider GetServiceProvider(ScenarioContext scenarioContext) => GetServiceProvider(
            scenarioContext,
            $"This method must be called either during Step execution, or in a BeforeScenario bindings with an Order >= {nameof(ContainerBeforeScenarioOrder)}.{nameof(ContainerBeforeScenarioOrder.ServiceProviderAvailable)}");

        /// <summary>
        /// Start setup of the DI container for a feature.
        /// </summary>
        /// <param name="featureContext">ReqnRoll-supplied context.</param>
        /// <remarks>We expect features run in parallel to be executing in separate app domains.</remarks>
        [BeforeFeature("@perFeatureContainer", "@setupContainer", Order = ContainerBeforeFeatureOrder.CreateServiceCollection)]
        public static void StartFeatureContainerSetup(FeatureContext featureContext)
        {
            CreateServiceCollection(featureContext);
        }

        /// <summary>
        /// Start setup of the DI container for a scenario.
        /// </summary>
        /// <param name="featureContext">ReqnRoll-supplied feature context.</param>
        /// <param name="scenarioContext">ReqnRoll-supplied scenario context.</param>
        /// <remarks>We expect scenarios run in parallel to be executing in separate app domains.</remarks>
        [BeforeScenario("@perScenarioContainer", Order = ContainerBeforeScenarioOrder.CreateServiceCollection)]
        public static void StartScenarioContainerSetup(
            FeatureContext featureContext,
            ScenarioContext scenarioContext)
        {
            if (featureContext.ContainsKey(ContainerBindingPhaseKey))
            {
                throw new InvalidOperationException("You cannot use both @perFeatureContainer and @perScenarioContainer in combination");
            }

            CreateServiceCollection(scenarioContext);
        }

        /// <summary>
        /// Complete setup of the DI container for a feature.
        /// </summary>
        /// <param name="featureContext">ReqnRoll-supplied context.</param>
        /// <remarks>We expect features run in parallel to be executing in separate app domains.</remarks>
        [BeforeFeature("@perFeatureContainer", "@setupContainer", Order = ContainerBeforeFeatureOrder.BuildServiceProvider)]
        public static void CompleteFeatureContainerSetup(FeatureContext featureContext)
        {
            CompleteContainerSetup(featureContext);
        }

        /// <summary>
        /// Complete setup of the DI container for a scenario.
        /// </summary>
        /// <param name="scenarioContext">ReqnRoll-supplied context.</param>
        /// <remarks>We expect scenarios run in parallel to be executing in separate app domains.</remarks>
        [BeforeScenario("@perScenarioContainer", Order = ContainerBeforeScenarioOrder.BuildServiceProvider)]
        public static void CompleteScenarioContainerSetup(ScenarioContext scenarioContext)
        {
            CompleteContainerSetup(scenarioContext);
        }

        /// <summary>
        /// Strongly discourage people from using the ambiguous old <c>@setupContainer</c> tag, and point them towards
        /// its replacement, @perFeatureContainer, and point out that @perScenarioContainer is now also available.
        /// </summary>
        [BeforeScenario("@setupContainer")]
        public static void WarnOfDeprecatedTag()
        {
            Assert.Warn("The @setupContainer is deprecated. Use @perFeatureContainer instead, or @perScenarioContainer if per-scenario container setup and teardown better suits your requirements.");
        }

        /// <summary>
        /// Tear down the container for a feature.
        /// </summary>
        /// <param name="featureContext">The feature context for the current feature.</param>
        /// <remarks>We expect features run in parallel to be executing in separate app domains.</remarks>
        [AfterFeature("@perFeatureContainer", "@setupContainer", Order = 1_000_000)]
        public static void TeardownContainer(FeatureContext featureContext)
        {
            featureContext.RunAndStoreExceptions(
                () => DisposeServiceProvider(featureContext));
        }

        /// <summary>
        /// Tear down the container for a scenario.
        /// </summary>
        /// <param name="scenarioContext">The context for the current scenario.</param>
        /// <remarks>We expect scenarios run in parallel to be executing in separate app domains.</remarks>
        [AfterScenario("@perScenarioContainer", "@setupContainer", Order = 1_000_000)]
        public static void TeardownContainer(ScenarioContext scenarioContext)
        {
            scenarioContext.RunAndStoreExceptions(
                () => DisposeServiceProvider(scenarioContext));
        }

        private static void ConfigureServices(
            ReqnrollContext context,
            Action<IServiceCollection> configure,
            string exceptionMessageIfBuildNotInProgress)
        {
            bool serviceBuildInProgress = VerifyBindingAvailable(context);

            if (!serviceBuildInProgress)
            {
                throw new InvalidOperationException(exceptionMessageIfBuildNotInProgress);
            }

            ServiceCollection serviceCollection = context.Get<ServiceCollection>(ServiceCollectionKey);
            configure(serviceCollection);
        }

        private static IServiceProvider GetServiceProvider(ReqnrollContext context, string messageForBuildInProgressError)
        {
            bool serviceBuildInProgress = VerifyBindingAvailable(context);

            if (serviceBuildInProgress)
            {
                throw new InvalidOperationException(messageForBuildInProgressError);
            }

            return context.Get<IServiceProvider>(ServiceProviderKey);
        }

        private static void CreateServiceCollection(ReqnrollContext scenarioContext)
        {
            scenarioContext.Set(true, ContainerBindingPhaseKey);
            var serviceCollection = new ServiceCollection();
            scenarioContext.Set(serviceCollection, ServiceCollectionKey);
        }

        private static void CompleteContainerSetup(ReqnrollContext context)
        {
            context.Set(false, ContainerBindingPhaseKey);

            ServiceCollection serviceCollection = context.Get<ServiceCollection>(ServiceCollectionKey);

            IServiceProvider service = serviceCollection.BuildServiceProvider();
            context.Set(service, ServiceProviderKey);
        }

        private static void DisposeServiceProvider(ReqnrollContext context)
        {
            if (context.Get<IServiceProvider>(ServiceProviderKey) is IDisposable spDisposable)
            {
                spDisposable.Dispose();
            }
        }

        private static bool VerifyBindingAvailable(ReqnrollContext featureContext)
        {
            if (!featureContext.TryGetValue(ContainerBindingPhaseKey, out bool serviceBuildInProgress))
            {
                throw new InvalidOperationException(
                    $"This method requires {typeof(ContainerBindings).FullName} to be registered with ReqnRoll and either the @perScenarioContainer or the @perFeatureContainer tag to be specified");
            }

            return serviceBuildInProgress;
        }
    }
}
