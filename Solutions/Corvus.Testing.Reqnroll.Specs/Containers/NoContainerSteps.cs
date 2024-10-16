// <copyright file="NoContainerSteps.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Testing.ReqnRoll.Specs.Containers
{
    using System;
    using Corvus.Testing.ReqnRoll;
    using NUnit.Framework;
    using Reqnroll;

    [Binding]
    public class NoContainerSteps
    {
        private readonly FeatureContext featureContext;
        private Exception? exceptionFromGetServiceProvider;

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
            Assert.That(this.exceptionFromGetServiceProvider, Is.InstanceOf<InvalidOperationException>());
        }
    }
}
