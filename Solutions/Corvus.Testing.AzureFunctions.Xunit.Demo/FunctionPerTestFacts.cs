// <copyright file="FunctionPerTestFacts.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

// ReSharper disable ArrangeThisQualifier
namespace Corvus.Testing.AzureFunctions.Xunit.Demo
{
    using System;
    using System.Net;
    using System.Reflection;
    using System.Threading.Tasks;
    using global::Xunit;
    using global::Xunit.Abstractions;
    using Microsoft.Extensions.Logging;
    using Serilog;
    using ILogger = Microsoft.Extensions.Logging.ILogger;

    public class FunctionPerTestFacts : DemoFunctionFacts, IAsyncLifetime
    {
        private readonly FunctionsController function;

        public FunctionPerTestFacts(ITestOutputHelper output)
        {
            var test = output.GetType()
                .GetField("test", BindingFlags.Instance | BindingFlags.NonPublic)?
                .GetValue(output) as ITest;

            ILogger logger = new LoggerFactory()
                .AddSerilog(
                    new LoggerConfiguration()
                        .WriteTo.File(@$"C:\temp\{test?.DisplayName}.log")
                        .WriteTo.Logger(output.CreateTestLogger())
                        .MinimumLevel.Debug()
                        .CreateLogger())
                .CreateLogger("Xunit Demo tests");

            this.function = new FunctionsController(logger);
            this.Port = new Random().Next(50000, 60000);
        }

        public int Port { get; }

        private string Uri => $"http://localhost:{this.Port}/";

        [Fact]
        public async Task A_Get_request_including_a_name_in_the_querystring_is_successful()
        {
            await When_I_GET($"{Uri}?name=Jon");

            Then_I_receive(HttpStatusCode.OK);
            await And_the_response_body_contains("Hello, Jon");
        }

        [Fact]
        public async Task A_Get_request_without_providing_a_name_in_the_querystring_fails()
        {
            await this.When_I_GET($"http://localhost:{this.Port}/");

            Then_I_receive(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task A_Post_request_including_a_name_in_the_querystring_is_successful()
        {
            await this.When_I_POST($"{Uri}?name=Jon");

            Then_I_receive(HttpStatusCode.OK);
            await And_the_response_body_contains("Hello, Jon");
        }

        [Fact]
        public async Task A_Post_request_including_a_name_in_the_request_body_is_successful()
        {
            await this.When_I_POST(Uri, new { name = "Jon" });

            Then_I_receive(HttpStatusCode.OK);
            await And_the_response_body_contains("Hello, Jon");
        }

        [Fact]
        public async Task A_Post_request_including_names_in_the_querystring_and_request_body_uses_the_name_in_the_querystring()
        {
            await this.When_I_POST($"{Uri}?name=Jon", new { name = "Jonathan" });

            Then_I_receive(HttpStatusCode.OK);
            await And_the_response_body_contains("Hello, Jon");
        }

        [Fact]
        public async Task A_Post_request_without_a_query_string_or_request_body_fails()
        {
            await this.When_I_POST(Uri);

            Then_I_receive(HttpStatusCode.BadRequest);
        }

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