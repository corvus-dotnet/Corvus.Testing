// <copyright file="AzureFunctionFixture.cs" company="Endjin Limited">
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

    /// <summary>
    /// Adapter class for Xunit.net to set up the Functions host process before all tests run, and tear it down only
    /// after they have all completed.
    /// </summary>
    public class AzureFunctionFixture : IAsyncLifetime
    {
        private readonly FunctionsController function;

        public AzureFunctionFixture(IMessageSink output)
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

        public int Port => 7076;

        public async Task InitializeAsync()
        {
            await this.function.StartFunctionsInstance(
                "Corvus.Testing.AzureFunctions.DemoFunction",
                this.Port,
                "netcoreapp3.1");
        }

        public Task DisposeAsync()
        {
            this.function.TeardownFunctions();
            return Task.CompletedTask;
        }
    }
}