// <copyright file="DemoFunctionFacts.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Testing.AzureFunctions.Xunit.Demo
{
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using global::Xunit;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public abstract class DemoFunctionFacts
    {
        private readonly HttpClient client;
        private HttpResponseMessage? lastHttpResponseMessage;

        protected DemoFunctionFacts()
        {
            this.client = new HttpClient();
        }

        protected async Task When_I_GET(string uri)
        {
            this.lastHttpResponseMessage = await this.client.GetAsync(uri).ConfigureAwait(false);
        }

        protected async Task When_I_POST(string uri, object? content = null)
        {
            HttpContent? requestBody = null;
            if (content != null)
            {
                requestBody = new StringContent(
                    JObject.FromObject(content).ToString(Formatting.None),
                    Encoding.UTF8,
                    "application/json");
            }

            await this.IssuePostRequest(uri, requestBody);
        }

        protected void Then_I_receive(HttpStatusCode expected)
        {
            Assert.NotNull(this.lastHttpResponseMessage);
            Assert.Equal(expected, this.lastHttpResponseMessage!.StatusCode);
        }

        protected async Task And_the_response_body_contains(string expected)
        {
            Assert.NotNull(this.lastHttpResponseMessage);

            string actual = await this.lastHttpResponseMessage!.Content.ReadAsStringAsync().ConfigureAwait(false);
            Assert.Equal(expected, actual);
        }

        private async Task IssuePostRequest(string uri, HttpContent? content = null)
        {
            this.lastHttpResponseMessage = await this.client.PostAsync(uri, content).ConfigureAwait(false);
        }
    }
}