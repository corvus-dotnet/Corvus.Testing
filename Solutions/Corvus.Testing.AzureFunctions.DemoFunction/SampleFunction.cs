// <copyright file="SampleFunction.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Testing.SpecFlow.DemoFunction
{
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;

    /// <summary>
    /// Sample functions for the demo to invoke.
    /// </summary>
    public class SampleFunction
    {
        private readonly string message;

        /// <summary>
        /// Initializes a new instance of the <see cref="SampleFunction"/> class.
        /// </summary>
        /// <param name="configuration">The current config.</param>
        public SampleFunction(IConfiguration configuration)
        {
            this.message = configuration["ResponseMessage"];
        }

        /// <summary>
        /// Sample endpoint that returns a basic Hello World string with a value drawn from either the
        /// querystring or the request body.
        /// </summary>
        /// <param name="req">The incoming request.</param>
        /// <param name="log">The logger.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName("SampleFunction-Get")]
        public async Task<IActionResult> SampleGetHandler(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "{*path}")] HttpRequest req,
            ILogger log)
        {
            // Note: the demo function has the log level set to "None" in host.json. This is intentional, to show that
            // our code in Corvus.Testing.AzureFunctions is able to detect that the function has started correctly
            // even when the majority of logging is turned off.
            log.LogInformation("C# HTTP trigger function processed a request.");

            string? name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync().ConfigureAwait(false);
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name ??= data?.name;

            string result = this.message.Replace("{name}", name);

            return name != null
                ? (ActionResult)new OkObjectResult(result)
                : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
        }
    }
}
