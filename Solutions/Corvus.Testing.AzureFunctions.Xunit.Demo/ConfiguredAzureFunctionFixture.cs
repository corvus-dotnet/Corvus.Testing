// <copyright file="ConfiguredAzureFunctionFixture.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Testing.AzureFunctions.Xunit.Demo
{
    using System;
    using System.Threading.Tasks;
    using global::Xunit;

    public class ConfiguredAzureFunctionFixture : IAsyncLifetime
    {
        private readonly FunctionsController function = new FunctionsController();

        public int Port => 7077;

        public string Greet(string name) => $"Welcome, {name}";

        public async Task InitializeAsync()
        {
            var configuration = new FunctionConfiguration();
            configuration.EnvironmentVariables.Add("ResponseMessage", this.Greet("{name}"));

            await this.function.StartFunctionsInstance(
                Environment.CurrentDirectory,
                "Corvus.SpecFlow.Extensions.DemoFunction",
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