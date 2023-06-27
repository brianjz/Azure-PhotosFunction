using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Xml;
using Photos.Models;
using System.Linq;
using Microsoft.Azure.Documents.Linq;
using System.Collections.Generic;
using Microsoft.Azure.Cosmos;
using Microsoft.WindowsAzure.Storage;
using Microsoft.Azure.Cosmos.Linq;

namespace Photos
{
    public static class PhotosSearch
    {
        [FunctionName("PhotosSearch")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            [CosmosDB("photos", "metadata", Connection = Literals.CosmosDBConnection, PartitionKey = "/id")] CosmosClient client,
            ILogger logger)
        {
            logger?.LogInformation("Searching...");

            var searchTerm = req.Query["searchTerm"];
            if(string.IsNullOrWhiteSpace(searchTerm))
            {
                return new NotFoundResult();
            }

            var results = new List<dynamic>();

            Container photosContainer = client.GetContainer("photos", "metadata");
            string continuationToken = null;
            do
            {
                var feedIterator = photosContainer.GetItemLinqQueryable<PhotoUploadModel>().Where(p => p.Description.Contains(searchTerm)).ToFeedIterator();

                while (feedIterator.HasMoreResults)
                {
                    FeedResponse<PhotoUploadModel> feedResponse = await feedIterator.ReadNextAsync();
                    continuationToken = feedResponse.ContinuationToken;
                    foreach (PhotoUploadModel item in feedResponse)
                    {
                        results.Add(item);
                    }
                }
            } while (continuationToken != null);

            return new OkObjectResult(results);
        }
    }
}
