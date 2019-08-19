// <copyright file="ChildObjectValueRetriever.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.SpecFlow.Extensions.Internal
{
    using System;
    using System.Collections.Generic;
    using TechTalk.SpecFlow;
    using TechTalk.SpecFlow.Assist;

    /// <summary>
    /// A value retriever that looks in the scenario context for a named instance.
    /// </summary>
    internal class ChildObjectValueRetriever : IValueRetriever
    {
        private readonly ScenarioContext scenarioContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChildObjectValueRetriever"/> class.
        /// </summary>
        /// <param name="scenarioContext">The ambient scenario context.</param>
        public ChildObjectValueRetriever(ScenarioContext scenarioContext)
        {
            this.scenarioContext = scenarioContext;
        }

        /// <inheritdoc/>
        public bool CanRetrieve(KeyValuePair<string, string> keyValuePair, Type targetType, Type propertyType)
        {
            return keyValuePair.Value.StartsWith("{") && keyValuePair.Value.EndsWith("}");
        }

        /// <inheritdoc/>
        public object Retrieve(KeyValuePair<string, string> keyValuePair, Type targetType, Type propertyType)
        {
            return this.scenarioContext.Get<object>(keyValuePair.Value.Substring(1, keyValuePair.Value.Length - 2));
        }
    }
}