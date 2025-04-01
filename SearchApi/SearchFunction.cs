using System.Net;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.Identity;
using System.Text.Json;
using System.Web;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

public class SearchFunction
{
    private readonly ILogger _logger;

    public SearchFunction(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<SearchFunction>();
    }

    [Function("SearchFunction")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "search")] HttpRequestData req)
    {
        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query).Get("query");

        if (string.IsNullOrWhiteSpace(query))
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            AddCorsHeaders(badRequest);
            await badRequest.WriteStringAsync("Missing required 'query' parameter.");
            return badRequest;
        }

        try
        {
            var endpoint = Environment.GetEnvironmentVariable("AZURE_SEARCH_SERVICE_ENDPOINT");
            var indexName = Environment.GetEnvironmentVariable("AZURE_SEARCH_INDEX_NAME");

            var credential = new DefaultAzureCredential();
            var client = new SearchClient(new Uri(endpoint), indexName, credential);

            var options = new SearchOptions
            {
                Select = { "BugID", "SubmissionID", "RequirementNoFull", "BugType" }
            };

            var results = await client.SearchAsync<dynamic>(query, options);

            var documents = new List<Dictionary<string, object>>();
            await foreach (var result in results.Value.GetResultsAsync())
            {
                var document = JsonSerializer.Deserialize<Dictionary<string, object>>(result.Document.ToString());
                documents.Add(document);
            }

            var json = JsonSerializer.Serialize(documents);

            var response = req.CreateResponse(HttpStatusCode.OK);
            AddCorsHeaders(response);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(json);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search query failed.");

            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            AddCorsHeaders(errorResponse);
            await errorResponse.WriteStringAsync($"Search failed: {ex.Message}");
            return errorResponse;
        }
    }

    private static void AddCorsHeaders(HttpResponseData response)
    {
        response.Headers.Add("Access-Control-Allow-Origin", "*");
        response.Headers.Add("Access-Control-Allow-Methods", "GET, OPTIONS");
        response.Headers.Add("Access-Control-Allow-Headers", "*");
    }
}
