// <copyright file="FunctionConfigurationExtensions.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.SpecFlow.Extensions
{
    using System.Collections.Generic;
    using Corvus.Testing.AzureFunctions.Internal;
    using Microsoft.Extensions.Configuration;
    using TechTalk.SpecFlow;

    /// <summary>
    /// Extension methods to assist with function configuration.
    /// </summary>
    public static class FunctionConfigurationExtensions
    {
        /// <summary>
        /// Adds an environment variable to the configuration that will be used for any
        /// functions started via <see cref="FunctionsBindings"/>.
        /// </summary>
        /// <param name="testContext">The context to add the configuration to.</param>
        /// <param name="name">The name of the environment variable.</param>
        /// <param name="value">The value of the environment variable.</param>
        public static void AddFunctionConfigurationEnvironmentVariable(this SpecFlowContext testContext, string name, string value)
        {
            FunctionConfiguration config = testContext.GetFunctionConfiguration();
            config.EnvironmentVariables.Add(name, value);
        }

        /// <summary>
        /// Copies all of the values from the supplied <see cref="IConfigurationRoot"/> to function configuration
        /// environment variables.
        /// </summary>
        /// <param name="testContext">The context to add the configuration to.</param>
        /// <param name="configuration">The configuration to copy.</param>
        public static void CopyToFunctionConfigurationEnvironmentVariables(this SpecFlowContext testContext, IConfigurationRoot configuration)
        {
            FunctionConfiguration config = testContext.GetFunctionConfiguration();

            foreach (KeyValuePair<string, string> item in configuration.AsEnumerable())
            {
                config.EnvironmentVariables.Add(item.Key, item.Value);
            }
        }

        /// <summary>
        /// Retrieves the <see cref="FunctionConfiguration"/> from the context.
        /// </summary>
        /// <param name="testContext">The context in which the configuration is stored.</param>
        /// <returns>The <see cref="FunctionConfiguration"/>.</returns>
        public static FunctionConfiguration GetFunctionConfiguration(this SpecFlowContext testContext)
        {
            if (!testContext.TryGetValue(out FunctionConfiguration value))
            {
                value = new FunctionConfiguration();
                testContext.Set(value);
            }

            return value;
        }
    }
}
