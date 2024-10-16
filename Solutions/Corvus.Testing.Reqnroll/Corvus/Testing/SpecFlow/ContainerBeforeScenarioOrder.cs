// <copyright file="ContainerBeforeScenarioOrder.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Testing.ReqnRoll
{
    using System;
    using Reqnroll;

    /// <summary>
    /// Defines constants to use for <see cref="HookAttribute.Order"/> property on a
    /// <see cref="BeforeScenarioAttribute"/> to ensure that initialization occurs in the correct order when using
    /// <see cref="ContainerBindings"/> at scenario scope. Tests must specify the <c>@perScenarioContainer</c> tag to opt
    /// into this functionality.
    /// </summary>
    public static class ContainerBeforeScenarioOrder
    {
        /// <summary>
        /// The <c>Order</c> at which the <c>ServiceCollection</c> for the container is created.
        /// </summary>
        public const int CreateServiceCollection = 10000;

        /// <summary>
        /// The minimum <c>Order</c> to specify on <c>BeforeScenario</c> bindings that use
        /// <see cref="ContainerBindings.ConfigureServices(FeatureContext, Action{Microsoft.Extensions.DependencyInjection.IServiceCollection})"/>.
        /// </summary>
        /// <remarks>
        /// If you need to run multiple bindings that populate the <c>ServiceCollection</c>, and
        /// if you need to control their order, you can use any number greater than or equal to this,
        /// and less then <see cref="BuildServiceProvider"/>.
        /// </remarks>
        public const int PopulateServiceCollection = CreateServiceCollection + 1;

        /// <summary>
        /// The <c>Order</c> at which the <c>IServiceProvider</c> gets built from the
        /// <c>ServiceCollection</c> that was available from when the order reached
        /// <see cref="PopulateServiceCollection"/>.
        /// </summary>
        public const int BuildServiceProvider = 20000;

        /// <summary>
        /// The <c>Order</c> at which the <c>IServiceProvider</c> built during the
        /// <see cref="PopulateServiceCollection"/> becomes available.
        /// </summary>
        public const int ServiceProviderAvailable = BuildServiceProvider + 1;
    }
}
