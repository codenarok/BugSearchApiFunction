using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System;

namespace AzureSearchApi
{
    public class GetFunctionUrl
    {
        private readonly ILogger _logger;

        public GetFunctionUrl(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<GetFunctionUrl>();
        }

        [Function("GetFunctionUrl")]
        public HttpResponseData Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "getFunctionUrl")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            string functionUrl = Environment.GetEnvironmentVariable("FUNCTION_URL");

            if (string.IsNullOrEmpty(functionUrl))
            {
                _logger.LogError("FUNCTION_URL environment variable is not set.");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                errorResponse.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                errorResponse.WriteString("FUNCTION_URL environment variable is not set.");
                return errorResponse;
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            response.Headers.Add("Access-Control-Allow-Origin", "*"); // Add CORS header

            response.WriteString(functionUrl);

            return response;
        }
    }
}