// <copyright file="ContainerTestContext.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Testing.ReqnRoll.Specs.Containers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;
    using Reqnroll;

    /// <summary>
    /// Manages the various objects required by the per-Feature and per-Scenario container tests.
    /// </summary>
    internal abstract class ContainerTestContext
    {
        private protected ContainerTestContext()
        {
        }

        public Dictionary<Phase, RootService> ExtractedServices { get; } = new Dictionary<Phase, RootService>();

        // We need to check that the services we supply at this stage are available later on. For this test, it
        // doesn't greatly matter what those services are, so we just created a couple of random objects that we
        // can look for again later. The RootService will pick these up because these are the types its constructor
        // shows that it depends on.
        public CultureInfo CultureOriginallySuppliedToContainer { get; } = new CultureInfo("fr-CA"); // Bonjour, eh?

        public IComparer<string> ComparerOriginallySuppliedToContainer { get; } = new FakeComparer();

        private protected abstract IServiceProvider ServiceProvider { get; }

        public static ContainerTestContext GetForFeature(FeatureContext featureContext)
        {
            if (!featureContext.TryGetValue(out ContainerTestContext result))
            {
                result = new FeatureContainerTestContext(featureContext);
                featureContext.Set(result);
            }

            return result;
        }

        public static ContainerTestContext GetForScenario(ScenarioContext scenarioContext)
        {
            if (!scenarioContext.TryGetValue(out ContainerTestContext result))
            {
                result = new ScenarioContainerTestContext(scenarioContext);
                scenarioContext.Set(result);
            }

            return result;
        }

        public void AddServices()
        {
            this.ConfigureServices(
                services =>
                {
                    services.AddTransient<RootService>();
                    services.AddSingleton(this.CultureOriginallySuppliedToContainer);
                    services.AddSingleton(this.ComparerOriginallySuppliedToContainer);
                });
        }

        public void GetServiceFromContainerDuringPhase(Phase phase)
        {
            this.ExtractedServices.Add(phase, this.ServiceProvider.GetRequiredService<RootService>());
        }

        public void VerifyServicesFromPhase(Phase phase)
        {
            Assert.That(this.ExtractedServices.TryGetValue(phase, out RootService? root), $"No services obtained for phase {phase}");
            Assert.That(root!.CultureInfo, Is.SameAs(this.CultureOriginallySuppliedToContainer), "CultureInfo");
            Assert.That(root!.Comparer, Is.SameAs(this.ComparerOriginallySuppliedToContainer), "Comparer");
        }

        private protected abstract void ConfigureServices(Action<IServiceCollection> services);

        private class FeatureContainerTestContext : ContainerTestContext
        {
            private readonly FeatureContext featureContext;

            public FeatureContainerTestContext(FeatureContext featureContext)
            {
                this.featureContext = featureContext;
            }

            private protected override IServiceProvider ServiceProvider => ContainerBindings.GetServiceProvider(this.featureContext);

            private protected override void ConfigureServices(Action<IServiceCollection> configureServices)
            {
                ContainerBindings.ConfigureServices(this.featureContext, configureServices);
            }
        }

        private class ScenarioContainerTestContext : ContainerTestContext
        {
            private readonly ScenarioContext scenarioContext;

            public ScenarioContainerTestContext(ScenarioContext scenarioContext)
            {
                this.scenarioContext = scenarioContext;
            }

            private protected override IServiceProvider ServiceProvider => ContainerBindings.GetServiceProvider(this.scenarioContext);

            private protected override void ConfigureServices(Action<IServiceCollection> configureServices)
            {
                ContainerBindings.ConfigureServices(this.scenarioContext, configureServices);
            }
        }

        private class FakeComparer : IComparer<string>
        {
            public int Compare(string? x, string? y) => Comparer<string>.Default.Compare(x, y);
        }
    }
}