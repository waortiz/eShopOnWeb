namespace eShopOnWebFunctionApp.Dto
{
    public class OrderItemDto
    {
        public CatalogItemOrderedDto ItemOrdered { get; set; }
        public decimal UnitPrice { get; set; }
        public int Units { get; set; }
    }
}