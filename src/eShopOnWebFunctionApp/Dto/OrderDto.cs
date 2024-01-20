using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eShopOnWebFunctionApp.Dto
{
    public class OrderDto
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        public string ShippingAddress { get; set; }
        public List<OrderItemDto> OrderItems { get; set; }
        public double FinalPrice { get; set; }

    }
}
