// <copyright file="Phase.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Testing.ReqnRoll.Specs.Containers
{
    /// <summary>
    /// Represents the moment within test execution that we want to attempt or measure something.
    /// </summary>
    public enum Phase
    {
        BeforeFeatureServiceProviderAvailable,
        BeforeScenarioEarliest,
        BeforeScenarioServiceProviderAvailable,
        Given,
        When,
        Then,
    }
}