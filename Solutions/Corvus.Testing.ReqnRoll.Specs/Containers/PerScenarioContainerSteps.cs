// <copyright file="PerScenarioContainerSteps.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Testing.ReqnRoll.Specs.Containers
{
    using NUnit.Framework;
    using Reqnroll;

    [Binding]
    public class PerScenarioContainerSteps
    {
        private readonly FeatureContext featureContext;
        private readonly ScenarioContext scenarioContext;
        private readonly ContainerTestContext containerContext;

        public PerScenarioContainerSteps(
            FeatureContext featureContext,
            ScenarioContext scenarioContext)
        {
            this.featureContext = featureContext;
            this.scenarioContext = scenarioContext;
            this.containerContext = ContainerTestContext.GetForScenario(this.scenarioContext);
        }

        [Given("I have specified the perScenarioContainer tag")]
        public void GivenIHaveSpecifiedThePerScenarioContainerTag()
        {
            Assert.That(this.featureContext.FeatureInfo.Tags, Does.Contain("perScenarioContainer"));
        }

        [Given(@"I use scenario ContainerBindings\.GetServiceProvider during a Given step")]
        public void GivenIUseScenarioContainerBindings_GetServiceProviderDuringAGivenStep()
        {
            this.GetServiceDuringStep(Phase.Given);
        }

        [When(@"I use scenario ContainerBindings\.GetServiceProvider during a When step")]
        public void WhenIUseScenarioContainerBindings_GetServiceProviderDuringAWhenStep()
        {
            this.GetServiceDuringStep(Phase.When);
        }

        [Then(@"if I also use scenario ContainerBindings\.GetServiceProvider during a Then step")]
        public void ThenIfIAlsoUseScenarioContainerBindings_GetServiceProviderDuringAThenStep()
        {
            this.GetServiceDuringStep(Phase.Then);
        }

        [Then("services added during the PopulateServiceCollection BeforeScenario phase should be available during '(.*)' steps")]
        public void ThenServicesAddedDuringThePopulateServiceCollectionBeforeScenarioPhaseShouldBeAvailableDuringSteps(Phase step)
        {
            this.containerContext.VerifyServicesFromPhase(step);
        }

        [Then("during the ServiceProviderAvailable BeforeScenario phase, services added during the PopulateServiceCollection BeforeScenario phase should have been available")]
        public void ThenDuringTheServiceProviderAvailableBeforeScenarioPhaseServicesAddedDuringThePopulateServiceCollectionBeforeScenarioPhaseShouldHaveBeenAvailable()
        {
            this.containerContext.VerifyServicesFromPhase(Phase.BeforeScenarioServiceProviderAvailable);
        }

        [BeforeScenario("@runPerScenarioContainerTests", Order = ContainerBeforeScenarioOrder.PopulateServiceCollection)]
        public static void PopulateServiceCollectionBeforeScenario(ScenarioContext scenarioContext)
        {
            ContainerTestContext.GetForScenario(scenarioContext).AddServices();
        }

        [BeforeScenario("@runPerScenarioContainerTests", Order = ContainerBeforeScenarioOrder.ServiceProviderAvailable)]
        public static void ServiceProviderAvailableBeforeScenario(ScenarioContext scenarioContext)
        {
            GetServiceDuringPhase(scenarioContext, Phase.BeforeScenarioServiceProviderAvailable);
        }

        private void GetServiceDuringStep(Phase step) => GetServiceDuringPhase(this.scenarioContext, step);

        private static void GetServiceDuringPhase(ScenarioContext scenarioContext, Phase phase)
        {
            ContainerTestContext.GetForScenario(scenarioContext).GetServiceFromContainerDuringPhase(phase);
        }
    }
}