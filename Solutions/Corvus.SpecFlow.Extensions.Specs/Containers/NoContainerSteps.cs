namespace Corvus.SpecFlow.Extensions.Specs.Containers
{
    using System;
    using Corvus.SpecFlow.Extensions;
    using NUnit.Framework;
    using TechTalk.SpecFlow;

    [Binding]
    public class NoContainerSteps
    {
        private Exception exceptionFromGetServiceProvider;
        private readonly FeatureContext featureContext;

        public NoContainerSteps(FeatureContext featureContext)
        {
            this.featureContext = featureContext;
        }

        [When(@"I call ContainerBindings\.GetServiceProvider")]
        public void WhenICallContainerBindings_GetServiceProviderInsideATryBlock()
        {
            try
            {
                ContainerBindings.GetServiceProvider(this.featureContext);
            }
            catch (Exception x)
            {
                this.exceptionFromGetServiceProvider = x;
            }
        }

        [Then("it should throw an InvalidOperationException")]
        public void ThenItShouldThrowAnInvalidOperationException()
        {
            Assert.IsInstanceOf<InvalidOperationException>(this.exceptionFromGetServiceProvider);
        }
    }
}
