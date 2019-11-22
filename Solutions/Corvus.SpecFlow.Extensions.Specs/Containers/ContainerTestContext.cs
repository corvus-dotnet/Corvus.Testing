namespace Corvus.SpecFlow.Extensions.Specs.Containers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using TechTalk.SpecFlow;

    /// <summary>
    /// Manages the various objects required by the per-Feature and per-Scenario container tests.
    /// </summary>
    internal abstract class ContainerTestContext
    {
        public Dictionary<Phase, RootService> ExtractedServices = new Dictionary<Phase, RootService>();

        private protected ContainerTestContext()
        {
        }

        // We need to check that the services we supply at this stage are available later on. For this test, it
        // doesn't greatly matter what those services are, so we just created a couple of random objects that we
        // can look for again later. The RootService will pick these up because these are the types its constructor
        // shows that it depends on.
        public CultureInfo CultureOriginallySuppliedToContainer { get; } = new CultureInfo("fr-CA"); // Bonjour, eh?
        public IComparer<string> ComparerOriginallySuppliedToContainer { get; } = new Mock<IComparer<string>>().Object;

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
            Assert.IsTrue(this.ExtractedServices.TryGetValue(phase, out RootService root), $"No services obtained for phase {phase}");

            Assert.AreSame(this.CultureOriginallySuppliedToContainer, root.CultureInfo, "CultureInfo");
            Assert.AreSame(this.ComparerOriginallySuppliedToContainer, root.Comparer, "Comparer");
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
    }
}