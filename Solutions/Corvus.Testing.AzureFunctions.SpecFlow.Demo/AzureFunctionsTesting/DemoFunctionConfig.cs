// <copyright file="DemoFunctionConfig.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Testing.SpecFlow.Demo.AzureFunctionsTesting
{
    using System.Collections.Generic;

    using Corvus.Testing.AzureFunctions;
    using Corvus.Testing.AzureFunctions.SpecFlow;

    using TechTalk.SpecFlow;

    internal static class DemoFunctionConfig
    {
        public static void SetupTestConfig(SpecFlowContext context)
        {
            FunctionConfiguration functionConfiguration = FunctionsBindings.GetFunctionConfiguration(context);
            var config = new Dictionary<string, string?>()
            {
                { "ResponseMessage", "Welcome, {name}" },

                // IConfiguration's AsEnumerable includes null-valued entries for each
                // section. Since the most common way to set up a function's configuration
                // with this library is to pass that enumeration, we need to test this.
                // See https://github.com/corvus-dotnet/Corvus.Testing/issues/368
                { "Emulate:Null:Section:Entry", null },
            };
            functionConfiguration.CopyToEnvironmentVariables(config);
        }
    }
}