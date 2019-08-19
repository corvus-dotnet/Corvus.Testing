// <copyright file="ContainerBindings.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.SpecFlow.Extensions
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using TechTalk.SpecFlow;

    /// <summary>
    /// Provides Specflow bindings for Endjin Composition.
    /// </summary>
    [Binding]
    public static class ContainerBindings
    {
        private const string ServiceCollectionKey = "ContainerBindings.ServiceCollection";
        private const string ContainerBindingPhaseKey = "ContainerBindings.ContainerBindingPhase";
        private const string ServiceProviderKey = "ContainerBindings.ServiceProvider";

        /// <summary>
        /// Start setup of the endjin container for a feature.
        /// </summary>
        /// <param name="featureContext">SpecFlow-supplied context.</param>
        /// <remarks>We expect features run in parallel to be executing in separate app domains.</remarks>
        [BeforeFeature("@setupContainer", Order = ContainerBeforeFeatureOrder.CreateServiceCollection)]
        public static void StartFeatureContainerSetup(FeatureContext featureContext)
        {
            featureContext.Set(true, ContainerBindingPhaseKey);
            var serviceCollection = new ServiceCollection();
            featureContext.Set(serviceCollection, ServiceCollectionKey);
        }

        /// <summary>
        /// Add DI services.
        /// </summary>
        /// <param name="featureContext">The SpecFlow feature context.</param>
        /// <param name="configure">Callback that will be invoked with the service collection.</param>
        /// <remarks>
        /// Call this from a <c>BeforeFeature</c> binding. You must specify an <c>Order</c> that is
        /// greated than or equal to <see cref="ContainerBeforeFeatureOrder.PopulateServiceCollection"/>
        /// and less than <see cref="ContainerBeforeFeatureOrder.BuildServiceProvider"/>.
        /// </remarks>
        public static void ConfigureServices(
            FeatureContext featureContext,
            Action<IServiceCollection> configure)
        {
            bool serviceBuildInProgress = VerifyBindingAvailable(featureContext);

            if (!serviceBuildInProgress)
            {
                throw new InvalidOperationException(
                    $"This method must be called during BeforeFeature bindings with an Order >= {nameof(ContainerBeforeFeatureOrder)}.{nameof(ContainerBeforeFeatureOrder.PopulateServiceCollection)} and < {nameof(ContainerBeforeFeatureOrder)}.{nameof(ContainerBeforeFeatureOrder.BuildServiceProvider)}");
            }

            ServiceCollection serviceCollection = featureContext.Get<ServiceCollection>(ServiceCollectionKey);
            configure(serviceCollection);
        }

        /// <summary>
        /// Complete setup of the endjin container for a feature.
        /// </summary>
        /// <param name="featureContext">SpecFlow-supplied context.</param>
        /// <remarks>We expect features run in parallel to be executing in separate app domains.</remarks>
        [BeforeFeature("@setupContainer", Order = ContainerBeforeFeatureOrder.PopulateServiceCollection)]
        public static void CompleteFeatureContainerSetup(FeatureContext featureContext)
        {
            featureContext.Set(false, ContainerBindingPhaseKey);

            ServiceCollection serviceCollection = featureContext.Get<ServiceCollection>(ServiceCollectionKey);

            IServiceProvider service = serviceCollection.BuildServiceProvider();
            featureContext.Set(service, ServiceProviderKey);
        }

        /// <summary>
        /// Gets the <see cref="IServiceProvider"/> for the specified context.
        /// </summary>
        /// <param name="featureContext">The SpecFlow context.</param>
        /// <returns>The service provider.</returns>
        /// <remarks>
        /// You can call this either during execution of steps, or in a <c>BeforeFeature</c> binding with
        /// an <c>Order</c> greater than or equal to <see cref="ContainerBeforeFeatureOrder.ServiceProviderAvailable"/>.
        /// </remarks>
        public static IServiceProvider GetServiceProvider(FeatureContext featureContext)
        {
            bool serviceBuildInProgress = VerifyBindingAvailable(featureContext);

            if (serviceBuildInProgress)
            {
                throw new InvalidOperationException(
                    $"This method must be called either during Step execution, or in a BeforeFeature bindings with an Order >= {nameof(ContainerBeforeFeatureOrder)}.{nameof(ContainerBeforeFeatureOrder.ServiceProviderAvailable)}");
            }

            return featureContext.Get<IServiceProvider>(ServiceProviderKey);
        }

        /// <summary>
        /// Tear down the endjin container for a feature.
        /// </summary>
        /// <param name="featureContext">The feature context for the current feature.</param>
        /// <remarks>We expect features run in parallel to be executing in separate app domains.</remarks>
        [AfterFeature("@setupContainer", Order = 1_000_000)]
        public static void TeardownContainer(FeatureContext featureContext)
        {
            featureContext.RunAndStoreExceptions(
                () =>
                {
                });
        }

        private static bool VerifyBindingAvailable(FeatureContext featureContext)
        {
            if (!featureContext.TryGetValue(ContainerBindingPhaseKey, out bool serviceBuildInProgress))
            {
                throw new InvalidOperationException(
                    $"This method requires {typeof(ContainerBindings).FullName} to be registered with SpecFlow and the @setupContainer tag to be specified");
            }

            return serviceBuildInProgress;
        }
    }
}
