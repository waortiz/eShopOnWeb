using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace Microsoft.eShopWeb.ApplicationCore.Interfaces;
public interface IAzureServiceBusSender
{
    Task SendMessageAsync(ServiceBusMessage message);
}
