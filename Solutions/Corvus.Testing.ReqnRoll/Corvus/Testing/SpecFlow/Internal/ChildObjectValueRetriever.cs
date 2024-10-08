// <copyright file="ChildObjectValueRetriever.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Testing.ReqnRoll.Internal
{
    using System;
    using System.Collections.Generic;
    using Reqnroll;
    using Reqnroll.Assist;

    /// <summary>
    /// A value retriever that looks in the scenario context for a named instance.
    /// </summary>
    /// <param name="scenarioContext">The ambient scenario context.</param>
    internal class ChildObjectValueRetriever(ScenarioContext scenarioContext) : IValueRetriever
    {
        private readonly ScenarioContext scenarioContext = scenarioContext;

        /// <inheritdoc/>
        public bool CanRetrieve(KeyValuePair<string, string> keyValuePair, Type targetType, Type propertyType)
        {
            return keyValuePair.Value.StartsWith("{") && keyValuePair.Value.EndsWith("}");
        }

        /// <inheritdoc/>
        public object Retrieve(KeyValuePair<string, string> keyValuePair, Type targetType, Type propertyType)
        {
            return this.scenarioContext.Get<object>(keyValuePair.Value[1..^1]);
        }
    }
}