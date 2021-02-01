// <copyright file="ConfiguredAzureFunctionFixture.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Testing.AzureFunctions.Xunit.Demo
{
    using System.Threading.Tasks;
    using global::Xunit;
    using global::Xunit.Abstractions;
    using Microsoft.Extensions.Logging;
    using Serilog;
    using ILogger = Microsoft.Extensions.Logging.ILogger;

    public class ConfiguredAzureFunctionFixture : IAsyncLifetime
    {
        private readonly FunctionsController function;

        public ConfiguredAzureFunctionFixture(IMessageSink output)
        {
            ILogger logger = new LoggerFactory()
                .AddSerilog(
                    new LoggerConfiguration()
                        .WriteTo.File(@$"C:\temp\{this.GetType().FullName}.log")
                        .WriteTo.TestOutput(output)
                        .MinimumLevel.Debug()
                        .CreateLogger())
                .CreateLogger("Xunit Demo tests");

            this.function = new FunctionsController(logger);
        }

        public int Port => 7077;

        public string Greet(string name) => $"Welcome, {name}";

        public async Task InitializeAsync()
        {
            var configuration = new FunctionConfiguration();
            configuration.EnvironmentVariables.Add("ResponseMessage", this.Greet("{name}"));

            await this.function.StartFunctionsInstance(
                "Corvus.Testing.AzureFunctions.DemoFunction",
                this.Port,
                "netcoreapp3.1",
                configuration: configuration);
        }

        public async Task DisposeAsync()
        {
            this.function.TeardownFunctions();
            await Task.CompletedTask;
        }
    }
}