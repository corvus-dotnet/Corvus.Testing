namespace Corvus.Testing.Tenancy.SpecFlow.Demo.Steps
{
    using Corvus.Tenancy;
    using NUnit.Framework;
    using TechTalk.SpecFlow;

    [Binding]
    public class GetAWellKnownTenantSteps
    {
        private readonly FeatureContext featureContext;

        public GetAWellKnownTenantSteps(FeatureContext featureContext)
        {
            this.featureContext = featureContext;
        }

        [When(@"I acquire a well known tenant called ""(.*)""")]
        public void WhenIAcquireAWellKnownTenantCalled(string tenantKey)
        {
            this.featureContext.Set(this.featureContext.Get<ITenant>(), tenantKey);
        }

        [Then(@"the tenant called ""(.*)"" should not be null")]
        public void ThenTheTenantCalledShouldNotBeNull(string tenantKey)
        {
            Assert.IsNotNull(this.featureContext.Get<ITenant>(tenantKey));
        }
    }
}
