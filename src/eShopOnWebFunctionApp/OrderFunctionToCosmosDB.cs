using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using eShopOnWebFunctionApp.Dto;
using Microsoft.Azure.Cosmos;

namespace eShopOnWebFunctionApp
{
    public static class OrderFunctionToCosmosDB
    {
        [FunctionName("OrderFunctionToCosmosDB")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            [CosmosDB(
        databaseName: "Order",
        containerName: "OrderContainer",
        Connection = "CosmoDbConnection")] CosmosClient outputDocument,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            var order = JsonConvert.DeserializeObject<OrderDto>(requestBody);
            var container = outputDocument.GetContainer("OrderContainer", "Order");

            await container.UpsertItemAsync<OrderDto>(order);

            return new OkResult();
        }
    }
}
