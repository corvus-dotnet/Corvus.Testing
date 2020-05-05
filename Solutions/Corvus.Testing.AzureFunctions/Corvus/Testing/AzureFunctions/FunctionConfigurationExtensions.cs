// <copyright file="FunctionConfigurationExtensions.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Testing.AzureFunctions
{
    using System.Collections.Generic;

    /// <summary>
    /// Extension methods to assist with function configuration.
    /// </summary>
    public static class FunctionConfigurationExtensions
    {
        /// <summary>
        /// Copies all of the values from the supplied list to function configuration
        /// environment variables.
        /// </summary>
        /// <param name="functionConfiguration">The function configuration to copy the values to.</param>
        /// <param name="values">The values to copy.</param>
        public static void CopyToEnvironmentVariables(
            this FunctionConfiguration functionConfiguration,
            IEnumerable<KeyValuePair<string, string>> values)
        {
            foreach (KeyValuePair<string, string> item in values)
            {
                functionConfiguration.EnvironmentVariables.Add(item.Key, item.Value);
            }
        }
    }
}
