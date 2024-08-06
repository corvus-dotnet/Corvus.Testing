// <copyright file="PerFeatureContainerSteps.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Testing.ReqnRoll.Specs.Containers
{
    using Corvus.Testing.ReqnRoll;
    using NUnit.Framework;
    using Reqnroll;

    [Binding]
    public class PerFeatureContainerSteps
    {
        private readonly FeatureContext featureContext;
        private readonly ContainerTestContext containerContext;

        public PerFeatureContainerSteps(FeatureContext featureContext)
        {
            this.featureContext = featureContext;
            this.containerContext = ContainerTestContext.GetForFeature(this.featureContext);
        }

        [Given("I have specified the perFeatureContainer tag")]
        public void GivenIHaveSpecifiedThePerFeatureContainerTag()
        {
            Assert.That(this.featureContext.FeatureInfo.Tags, Does.Contain("perFeatureContainer"));
        }

        [Given(@"I use feature ContainerBindings\.GetServiceProvider during a Given step")]
        public void GivenIUseContainerBindings_GetServiceProviderDuringAGivenStep()
        {
            this.GetServiceDuringStep(Phase.Given);
        }

        [When(@"I use feature ContainerBindings\.GetServiceProvider during a When step")]
        public void WhenIUseContainerBindings_GetServiceProviderDuringAWhenStep()
        {
            this.GetServiceDuringStep(Phase.When);
        }

        [Then(@"if I also use feature ContainerBindings\.GetServiceProvider during a Then step")]
        public void ThenIfIAlsoUseContainerBindings_GetServiceProviderDuringAThenStep()
        {
            this.GetServiceDuringStep(Phase.Then);
        }

        [Then("services added during the PopulateServiceCollection BeforeFeature phase should be available during '(.*)' steps")]
        public void ThenServicesAddedDuringThePopulateServiceCollectionBeforeFeaturePhaseShouldBeAvailableDuringSteps(Phase step)
        {
            this.VerifyServicesFromPhase(step);
        }

        [Then("during the ServiceProviderAvailable BeforeFeature phase, services added during the PopulateServiceCollection BeforeFeature phase should have been available")]
        public void ThenDuringTheServiceProviderAvailableBeforeFeaturePhaseServicesAddedDuringThePopulateServiceCollectionBeforeFeaturePhaseShouldHaveBeenAvailable()
        {
            this.VerifyServicesFromPhase(Phase.BeforeFeatureServiceProviderAvailable);
        }

        [Then("services added during the PopulateServiceCollection BeforeFeature phase should be available during the earliest BeforeScenario processing")]
        public void ThenDuringTheEarliestBeforeScenarioPhaseServicesAddedDuringThePopulateServiceCollectionBeforeFeaturePhaseShouldHaveBeenAvailable()
        {
            this.VerifyServicesFromPhase(Phase.BeforeScenarioEarliest);
        }

        [BeforeFeature("@runPerFeatureContainerTests", Order = ContainerBeforeFeatureOrder.PopulateServiceCollection)]
        public static void PopulateServiceCollectionBeforeFeature(FeatureContext featureContext)
        {
            ContainerTestContext.GetForFeature(featureContext).AddServices();
        }

        [BeforeFeature("@runPerFeatureContainerTests", Order = ContainerBeforeFeatureOrder.ServiceProviderAvailable)]
        public static void ServiceProviderAvailableBeforeFeature(FeatureContext featureContext)
        {
            GetServiceDuringPhase(featureContext, Phase.BeforeFeatureServiceProviderAvailable);
        }

        [BeforeScenario("@useServiceProviderBeforeScenarioInPerFeatureContainerTests", Order = 0)]
        public static void EarliestBeforeScenario(FeatureContext featureContext)
        {
            GetServiceDuringPhase(featureContext, Phase.BeforeScenarioEarliest);
        }

        private void GetServiceDuringStep(Phase step) => GetServiceDuringPhase(this.featureContext, step);

        private static void GetServiceDuringPhase(FeatureContext featureContext, Phase phase)
        {
            ContainerTestContext.GetForFeature(featureContext).GetServiceFromContainerDuringPhase(phase);
        }

        private void VerifyServicesFromPhase(Phase phase)
        {
            this.containerContext.VerifyServicesFromPhase(phase);
        }
    }
}