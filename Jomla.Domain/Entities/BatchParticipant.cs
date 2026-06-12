namespace Jomla.Domain.Entities
{
    public class BatchParticipant
    {
        public Guid BatchId { get; set; }
        public SupplierBatch Batch { get; set; } = null!;
        public Guid BuyerId { get; set; }
        public AppUser Buyer { get; set; } = null!;
        public int Quantity { get; set; }
        public string StripePaymentIntentId { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; }
    }
}
