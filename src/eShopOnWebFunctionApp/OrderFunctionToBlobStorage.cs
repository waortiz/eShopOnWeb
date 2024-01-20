using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Azure.Core;
using Azure.Storage.Blobs;
using System.Text;

namespace eShopOnWebFunctionApp
{
    public static class OrderFunctionToBlobStorage
    {
        [FunctionName("OrderFunctionToBlobStorage")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            string connection = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            string containerName = Environment.GetEnvironmentVariable("ContainerName");
            Stream myBlob = new MemoryStream(Encoding.UTF8.GetBytes(requestBody ?? ""));
            BlobClientOptions blobOptions = new BlobClientOptions()
            {
                Retry = {
                            Delay = TimeSpan.FromSeconds(2),
                            MaxRetries = 3,
                            Mode = RetryMode.Exponential,
                            MaxDelay = TimeSpan.FromSeconds(10),
                            NetworkTimeout = TimeSpan.FromSeconds(100)
                        },
            };
            var blobClient = new BlobContainerClient(connection, containerName, blobOptions);
            var blob = blobClient.GetBlobClient(containerName);
            await blob.UploadAsync(myBlob);

            string responseMessage = "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response.";

            return new OkObjectResult(responseMessage);
        }
    }
}
