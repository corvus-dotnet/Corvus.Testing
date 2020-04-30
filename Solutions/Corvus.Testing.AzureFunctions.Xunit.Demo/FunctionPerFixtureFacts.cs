// <copyright file="FunctionPerFixtureFacts.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

// ReSharper disable ArrangeThisQualifier
namespace Corvus.Testing.AzureFunctions.Xunit.Demo
{
    using System.Net;
    using System.Threading.Tasks;
    using global::Xunit;

    public class FunctionPerFixtureFacts : DemoFunctionFacts, IClassFixture<AzureFunctionFixture>
    {
        private readonly AzureFunctionFixture fixture;

        public FunctionPerFixtureFacts(AzureFunctionFixture fixture)
        {
            this.fixture = fixture;
        }

        public int Port => fixture.Port;

        [Fact]
        public async Task A_Get_request_including_a_name_in_the_querystring_is_successful()
        {
            await When_I_GET($"http://localhost:{Port}/?name=Jon");

            Then_I_receive(HttpStatusCode.OK);
            await And_the_response_body_contains("Hello, Jon");
        }

        [Fact]
        public async Task A_Get_request_without_providing_a_name_in_the_querystring_fails()
        {
            await When_I_GET($"http://localhost:{Port}/");

            Then_I_receive(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task A_Post_request_including_a_name_in_the_querystring_is_successful()
        {
            await this.When_I_POST($"http://localhost:{Port}/?name=Jon");

            Then_I_receive(HttpStatusCode.OK);
            await And_the_response_body_contains("Hello, Jon");
        }
    }
}
