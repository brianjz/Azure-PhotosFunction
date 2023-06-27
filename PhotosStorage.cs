using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Photos.Models;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Reflection.Metadata;
using System.Text;
using System.Configuration;
using Photos.AnalyzerService.Abstractions;

namespace Photos
{
    public class PhotosStorage
    {
        private readonly IAnalyzerService analyzerService;

        public PhotosStorage(IAnalyzerService analyzerService)
        {
            this.analyzerService = analyzerService;
        }

        [FunctionName("PhotosStorage")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            [Blob("photos", FileAccess.ReadWrite, Connection = Literals.StorageConnectionString)] BlobContainerClient blobContainer,
            [CosmosDB("photos", "metadata", Connection = Literals.CosmosDBConnection, CreateIfNotExists = true, PartitionKey = "/id")] IAsyncCollector<dynamic> items,
            ILogger logger)
        {
           var body = await new StreamReader(req.Body).ReadToEndAsync();
           var request = JsonConvert.DeserializeObject<PhotoUploadModel>(body);

            var newId = Guid.NewGuid();
            var blobName = $"{newId}.jpg";

            await blobContainer.CreateIfNotExistsAsync();

            var cloudBlockBlob = blobContainer.GetBlobClient(blobName);
            var photoBytes = Convert.FromBase64String(request.Photo);
            //await cloudBlockBlob.UploadAsync(photoBytes);
            await cloudBlockBlob.UploadAsync(new MemoryStream(photoBytes),
                new BlobHttpHeaders()
                {
                    ContentType = "image/jpeg"
                });

            var analysisResult = await analyzerService.AnalyzeAsync(photoBytes);

            var item = new
            {
                id = newId,
                name = request.Name,
                description = request.Description,
                tags = request.Tags,
                analysis = analysisResult
            };
            await items.AddAsync(item);

            logger?.LogInformation($"Successfully upload {newId}.jpg file and its metadata");

            return new OkObjectResult(newId);

        }
    }
}
