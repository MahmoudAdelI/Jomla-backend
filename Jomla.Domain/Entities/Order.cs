namespace Jomla.Domain.Entities
{
    public class Order
    {
        public Guid Id { get; set; }
        public Guid BuyerId { get; set; }
        public AppUser Buyer { get; set; } = null!;
        public Guid? BatchId { get; set; }
        public SupplierBatch? Batch { get; set; }
        public Guid? OfferId { get; set; }
        public GroupRequestOffer? Offer { get; set; }
        public int Quantity { get; set; }
        public decimal TotalAmount { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
