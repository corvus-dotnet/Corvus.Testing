namespace Corvus.Testing.AzureFunctions.DemoFunctions.Isolated
{
    using System.Net;
    using System.Text.Json;

    using Microsoft.Azure.Functions.Worker;
    using Microsoft.Azure.Functions.Worker.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;

    public class SampleFunction
    {
        private readonly string message;
        private readonly ILogger log;

        public SampleFunction(
            IConfiguration configuration,
            ILoggerFactory loggerFactory)
        {
            this.message = configuration["ResponseMessage"];
            this.log = loggerFactory.CreateLogger<SampleFunction>();
        }

        [Function("SampleFunction-Get")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            // Note: the demo function has the log level set to "None" in host.json. This is intentional, to show that
            // our code in Corvus.Testing.AzureFunctions is able to detect that the function has started correctly
            // even when the majority of logging is turned off.
            this.log.LogInformation("C# HTTP trigger function processed a request.");

            string? name = req.Query["name"];

            try
            {
                using JsonDocument doc = await JsonDocument.ParseAsync(req.Body);
                if (doc.RootElement.TryGetProperty("name", out JsonElement nameElement)
                    && nameElement.ValueKind == JsonValueKind.String)
                {
                    name ??= nameElement.GetString();
                }
            }
            catch (JsonException)
            {
            }

            string result = this.message.Replace("{name}", name);

            HttpResponseData response;
            if (name is null)
            {
                response = req.CreateResponse(HttpStatusCode.BadRequest);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                response.WriteString("Please pass a name on the query string or in the request body");
            }
            else
            {
                response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

                response.WriteString(this.message.Replace("{name}", name));

            }

            return response;
        }
    }
}
