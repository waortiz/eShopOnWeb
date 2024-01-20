using System;

namespace Microsoft.eShopWeb.ApplicationCore.Interfaces;
public interface IAzureServiceBusClient : IAsyncDisposable
{   
    IAzureServiceBusSender CreateSender(string topic);
}
