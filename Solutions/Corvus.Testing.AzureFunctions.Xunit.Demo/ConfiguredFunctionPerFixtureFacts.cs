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
        /// <summary>
        /// Initialize the fixture.
        /// </summary>
        /// <param name="fixture">
        /// We don't use this, but it has to be here because otherwise XUnit gets unhappy. It
        /// wants this because we implement <see cref="IClassFixture{ConfiguredAzureFunctionFixture}"/>.
        /// We need to do that so that the function gets started, but that side effect is all we care
        /// about - we don't actually need to refer to that instance at any point in the test.
        /// So we need to accept it as an ignored constructor argument, and then silence the
        /// analyzer messages.
        /// </param>
#pragma warning disable IDE0060 // Remove unused parameter - see comment above
#pragma warning disable RCS1163 // Unused parameter.
        public ConfiguredFunctionPerFixtureFacts(ConfiguredAzureFunctionFixture fixture)
#pragma warning restore RCS1163, IDE0060 // Unused parameter.
        {
        }

        private static int Port => ConfiguredAzureFunctionFixture.Port;

        private static string Uri => $"http://localhost:{Port}/";

        [Fact]
        public async Task A_Get_request_including_a_name_in_the_querystring_is_successful()
        {
            await When_I_GET($"{Uri}?name=Jon");

            Then_I_receive(HttpStatusCode.OK);
            await And_the_response_body_contains(ConfiguredAzureFunctionFixture.Greet("Jon"));
        }

        [Fact]
        public async Task A_Get_request_without_providing_a_name_in_the_querystring_fails()
        {
            await this.When_I_GET($"http://localhost:{Port}/");

            Then_I_receive(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task A_Post_request_including_a_name_in_the_querystring_is_successful()
        {
            await this.When_I_POST($"{Uri}?name=Jon");

            Then_I_receive(HttpStatusCode.OK);
            await And_the_response_body_contains(ConfiguredAzureFunctionFixture.Greet("Jon"));
        }

        [Fact]
        public async Task A_Post_request_including_a_name_in_the_request_body_is_successful()
        {
            await this.When_I_POST(Uri, new { name = "Jon" });

            Then_I_receive(HttpStatusCode.OK);
            await And_the_response_body_contains(ConfiguredAzureFunctionFixture.Greet("Jon"));
        }

        [Fact]
        public async Task A_Post_request_including_names_in_the_querystring_and_request_body_uses_the_name_in_the_querystring()
        {
            await this.When_I_POST($"{Uri}?name=Jon", new { name = "Jonathan" });

            Then_I_receive(HttpStatusCode.OK);
            await And_the_response_body_contains(ConfiguredAzureFunctionFixture.Greet("Jon"));
        }

        [Fact]
        public async Task A_Post_request_without_a_query_string_or_request_body_fails()
        {
            await this.When_I_POST(Uri);

            Then_I_receive(HttpStatusCode.BadRequest);
        }
    }
}