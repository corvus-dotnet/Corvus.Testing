// <copyright file="ConfiguredFunctionPerFixtureFacts.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

// ReSharper disable ArrangeThisQualifier
namespace Corvus.Testing.AzureFunctions.Xunit.Demo
{
    using System.Net;
    using System.Threading.Tasks;
    using global::Xunit;

    public class ConfiguredFunctionPerFixtureFacts : DemoFunctionFacts, IClassFixture<ConfiguredAzureFunctionFixture>
    {
        private readonly ConfiguredAzureFunctionFixture fixture;

        public ConfiguredFunctionPerFixtureFacts(ConfiguredAzureFunctionFixture fixture) => this.fixture = fixture;

        private int Port => fixture.Port;

        private string Uri => $"http://localhost:{this.Port}/";

        [Fact]
        public async Task A_Get_request_including_a_name_in_the_querystring_is_successful()
        {
            await When_I_GET($"{Uri}?name=Jon");

            Then_I_receive(HttpStatusCode.OK);
            await And_the_response_body_contains(this.fixture.Greet("Jon"));
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
            await And_the_response_body_contains(this.fixture.Greet("Jon"));
        }

        [Fact]
        public async Task A_Post_request_including_a_name_in_the_request_body_is_successful()
        {
            await this.When_I_POST(Uri, new { name = "Jon" });

            Then_I_receive(HttpStatusCode.OK);
            await And_the_response_body_contains(this.fixture.Greet("Jon"));
        }

        [Fact]
        public async Task A_Post_request_including_names_in_the_querystring_and_request_body_uses_the_name_in_the_querystring()
        {
            await this.When_I_POST($"{Uri}?name=Jon", new { name = "Jonathan" });

            Then_I_receive(HttpStatusCode.OK);
            await And_the_response_body_contains(this.fixture.Greet("Jon"));
        }

        [Fact]
        public async Task A_Post_request_without_a_query_string_or_request_body_fails()
        {
            await this.When_I_POST(Uri);

            Then_I_receive(HttpStatusCode.BadRequest);
        }
    }
}