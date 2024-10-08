// <copyright file="ContainerBeforeFeatureOrder.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Testing.ReqnRoll
{
    using System;
    using Reqnroll;

    /// <summary>
    /// Defines constants to use for <see cref="HookAttribute.Order"/> property on a
    /// <see cref="BeforeFeatureAttribute"/> to ensure that initialization occurs in the correct order when using
    /// <see cref="ContainerBindings"/> at feature scope. Tests must specify the <c>@perFeatureContainer</c> tag to opt
    /// into this functionality.
    /// </summary>
    /// <remarks>
    /// For backwards compatibility, the <c>@setupContainer</c> tag may also be used. However, this is deprecated
    /// because the addition of per-scenario container support made the meaning of that tag unclear.
    /// </remarks>
    public static class ContainerBeforeFeatureOrder
    {
        /// <summary>
        /// The <c>Order</c> at which the <c>ServiceCollection</c> for the container is created.
        /// </summary>
        public const int CreateServiceCollection = 10000;

        /// <summary>
        /// The minimum <c>Order</c> to specify on <c>BeforeFeature</c> bindings that use
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
