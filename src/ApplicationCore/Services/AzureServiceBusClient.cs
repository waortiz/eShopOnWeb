using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;

namespace Microsoft.eShopWeb.ApplicationCore.Services;
public class AzureServiceBusClient : IAzureServiceBusClient
{
    private readonly ServiceBusClient _serviceBusClient;

    public AzureServiceBusClient(string serviceBusConnectionString, ServiceBusClientOptions serviceBusClientOptions)
    {
        _serviceBusClient = new ServiceBusClient(serviceBusConnectionString, serviceBusClientOptions);
    }

    public IAzureServiceBusSender CreateSender(string topic)
    {
        return new AzureServiceBusSender(_serviceBusClient.CreateSender(topic));
    }

    public async ValueTask DisposeAsync()
    {
        await _serviceBusClient.DisposeAsync().AsTask();
    }
}
