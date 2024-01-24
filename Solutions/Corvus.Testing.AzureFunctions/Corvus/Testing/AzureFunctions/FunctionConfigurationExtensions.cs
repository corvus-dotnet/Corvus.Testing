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
        /// <remarks>
        /// <para>
        /// It may seem odd that <paramref name="values"/> permits entries where the value is null.
        /// This is because <c>IConfiguration</c>'s <c>AsEnumerable</c> extension method has always
        /// supplied null-valued items to represent sections (but didn't make that clear until they
        /// finally added nullabililty annotations). For example, given this:
        /// </para>
        /// <code><![CDATA[
        /// {
        ///   "Root": {
        ///     "Middle": {
        ///       "ExplicitNull": null,
        ///       "Value": "v"
        ///     }
        ///   },
        ///   "ExplicitNull": null
        /// }
        /// ]]></code>
        /// <para>
        /// the enumeration will include <c>Root</c> and <c>Root:Middle</c> entries that have a null
        /// value. This wasn't obvious until <c>Microsoft.Extensions.Configuration.Abstractions</c> was
        /// updated with nullability annotations.
        /// </para>
        /// <para>
        /// The most common usage of this method is to pass that enumerable from an <c>IConfiguration</c>
        /// which is why this tolerates nulls.
        /// </para>
        /// </remarks>
        public static void CopyToEnvironmentVariables(
            this FunctionConfiguration functionConfiguration,
            IEnumerable<KeyValuePair<string, string?>> values)
        {
            foreach (KeyValuePair<string, string?> item in values)
            {
                if (item.Value is string value)
                {
                    functionConfiguration.EnvironmentVariables.Add(item.Key, value);
                }
            }
        }
    }
}
