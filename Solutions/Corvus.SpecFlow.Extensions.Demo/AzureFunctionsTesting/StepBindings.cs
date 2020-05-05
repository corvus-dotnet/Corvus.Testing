// <copyright file="StepBindings.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.SpecFlow.Extensions.Demo.AzureFunctionsTesting
{
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using TechTalk.SpecFlow;
    using Corvus.Testing.AzureFunctions;

    [Binding]
    public class StepBindings
    {
        private readonly HttpClient client;
        private readonly ScenarioContext scenarioContext;

        private HttpResponseMessage? lastHttpResponseMessage;

        public StepBindings(ScenarioContext scenarioContext)
        {
            this.client = new HttpClient();
            this.scenarioContext = scenarioContext;
        }

        [Given("I have set additional configuration for functions instances")]
        public void GivenIHaveSetAdditionalConfigurationForFunctionsInstances(Table table)
        {
            FunctionConfiguration functionConfiguration = FunctionsBindings.GetFunctionConfiguration(this.scenarioContext);

            foreach (TableRow row in table.Rows)
            {
                functionConfiguration.EnvironmentVariables.Add(row[0], row[1]);
            }
        }

        [When("I send a GET request to '(.*)'")]
        public async Task WhenISendAGetRequestTo(string uri)
        {
            this.lastHttpResponseMessage = await this.client.GetAsync(uri).ConfigureAwait(false);
        }

        [When("I send a POST request to '(.*)'")]
        public async Task WhenISendAPOSTRequestTo(string uri)
        {
            this.lastHttpResponseMessage = await this.client.PostAsync(uri, null).ConfigureAwait(false);
        }

        [When("I send a POST request to '(.*)' with data in the request body")]
        public async Task WhenISendAPOSTRequestToWithDataInTheRequestBody(string uri, Table table)
        {
            var requestBody = new JObject();
            foreach (TableRow row in table.Rows)
            {
                requestBody.Add(row[0], row[1]);
            }

            var content = new StringContent(requestBody.ToString(Formatting.None), Encoding.UTF8, "application/json");

            this.lastHttpResponseMessage = await this.client.PostAsync(uri, content).ConfigureAwait(false);
        }

        [Then("I receive a (.*) response code")]
        public void ThenIReceiveAResponseCode(int expectedCode)
        {
            Assert.IsNotNull(this.lastHttpResponseMessage, "Could not verify last response status code as there is no last response");
            Assert.AreEqual((HttpStatusCode)expectedCode, this.lastHttpResponseMessage!.StatusCode);
        }

        [Then("the response body contains the text '(.*)'")]
        public async Task ThenTheResponseBodyContainsTheText(string expectedContent)
        {
            Assert.IsNotNull(this.lastHttpResponseMessage, "Could not verify last response status code as there is no last response");
            string actualContent = await this.lastHttpResponseMessage!.Content.ReadAsStringAsync().ConfigureAwait(false);

            Assert.AreEqual(expectedContent, actualContent);
        }
    }
}
