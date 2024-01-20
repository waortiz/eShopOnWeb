using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Azure.Messaging.ServiceBus;
using Microsoft.eShopWeb.ApplicationCore.Entities;
using Microsoft.eShopWeb.ApplicationCore.Entities.BasketAggregate;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.ApplicationCore.Specifications;
using Microsoft.Extensions.Options;

namespace Microsoft.eShopWeb.ApplicationCore.Services;

public class OrderService : IOrderService
{
    private readonly IRepository<Order> _orderRepository;
    private readonly IUriComposer _uriComposer;
    private readonly IRepository<Basket> _basketRepository;
    private readonly IRepository<CatalogItem> _itemRepository;
    private readonly IAzureServiceBusClient _azureServiceBusClient;
    private readonly string _orderFunctionToCosmosDBUrl;
    private readonly string _orderFunctionToBlobStorageUrl;
    private readonly string _orderServiceBusConnectionString;
    private readonly ServiceBusClientOptions _serviceBusClientOptions;

    public OrderService(IRepository<Basket> basketRepository,
        IRepository<CatalogItem> itemRepository,
        IRepository<Order> orderRepository,
        IUriComposer uriComposer,
        IAzureServiceBusClient azureServiceBusClient,
        IOptions<ServiceBus> serviceBus,
        IOptions<AzureFunctions> azureFunctions)
    {
        _orderRepository = orderRepository;
        _uriComposer = uriComposer;
        _basketRepository = basketRepository;
        _itemRepository = itemRepository;
        _azureServiceBusClient = azureServiceBusClient; 
        _orderFunctionToCosmosDBUrl = azureFunctions.Value.OrderFunctionToCosmosDB;
        _orderFunctionToBlobStorageUrl = azureFunctions.Value.OrderFunctionToBlobStorage;
        _orderServiceBusConnectionString = serviceBus.Value.OrderServiceBus;
        _serviceBusClientOptions = new ServiceBusClientOptions
        {
            RetryOptions = new ServiceBusRetryOptions()
            {
                Mode = ServiceBusRetryMode.Exponential,
            }
        };
    }

    public async Task CreateOrderAsync(int basketId, Address shippingAddress)
    {
        var basketSpec = new BasketWithItemsSpecification(basketId);
        var basket = await _basketRepository.FirstOrDefaultAsync(basketSpec);

        Guard.Against.Null(basket, nameof(basket));
        Guard.Against.EmptyBasketOnCheckout(basket.Items);

        var catalogItemsSpecification = new CatalogItemsSpecification(basket.Items.Select(item => item.CatalogItemId).ToArray());
        var catalogItems = await _itemRepository.ListAsync(catalogItemsSpecification);

        var items = basket.Items.Select(basketItem =>
        {
            var catalogItem = catalogItems.First(c => c.Id == basketItem.CatalogItemId);
            var itemOrdered = new CatalogItemOrdered(catalogItem.Id, catalogItem.Name, _uriComposer.ComposePicUri(catalogItem.PictureUri));
            var orderItem = new OrderItem(itemOrdered, basketItem.UnitPrice, basketItem.Quantity);
            return orderItem;
        }).ToList();

        var order = new Order(basket.BuyerId, shippingAddress, items);

        await PostOrderToOrderItemsReserverFunctionAsync(order);
        await PostOrderToAzureServiceBusAsync(order);

        await _orderRepository.AddAsync(order);
    }

    public async Task PostOrderToOrderItemsReserverFunctionAsync(Order order)
    {
        var content = new StringContent(JsonSerializer.Serialize(order), Encoding.UTF8, "application/json");

        using (var client = new HttpClient())
        using (var response = await client.PostAsync(_orderFunctionToCosmosDBUrl, content))
        using (var httpContent = response.Content)
        {
            var result = await httpContent.ReadAsStringAsync();
            var azureResponse = JsonSerializer.Deserialize<AzureResponse>(result);
        }
    }

    public async Task PostOrderToAzureServiceBusAsync(Order order)
    {
        var content = JsonSerializer.Serialize(order);

        await SendMessageToTopicAsync(content, "order");
    }

    public async Task PostOrderToBlobStorageAsync(Order order)
    {
        var content = new StringContent(JsonSerializer.Serialize(order), Encoding.UTF8, "application/json");

        using (var client = new HttpClient())
        using (var response = await client.PostAsync(_orderFunctionToBlobStorageUrl, content))
        using (var httpContent = response.Content)
        {
            var result = await httpContent.ReadAsStringAsync();
            var azureResponse = JsonSerializer.Deserialize<AzureResponse>(result);
        }

    }

    private async Task SendMessageToTopicAsync(string message, string topic)
    {
        await using var client = _azureServiceBusClient ?? new AzureServiceBusClient(_orderServiceBusConnectionString, _serviceBusClientOptions);
        var sender = client.CreateSender(topic);
        var serviceBusMessage = new ServiceBusMessage(Encoding.UTF8.GetBytes(message));
        await sender.SendMessageAsync(serviceBusMessage);
    }
}
