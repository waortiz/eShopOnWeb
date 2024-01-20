using Azure.Messaging.ServiceBus;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using System.Threading.Tasks;

namespace Microsoft.eShopWeb.ApplicationCore.Services;
public class AzureServiceBusSender : IAzureServiceBusSender
{
    private readonly ServiceBusSender _serviceBusSender;

    public AzureServiceBusSender(ServiceBusSender serviceBusSender)
    {
        _serviceBusSender = serviceBusSender;
    }

    public async Task SendMessageAsync(ServiceBusMessage message)
    {
        await _serviceBusSender.SendMessageAsync(message);
    }
}
