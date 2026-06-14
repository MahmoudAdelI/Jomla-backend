namespace Jomla.Domain.Entities
{
    public class SupplierBatch
    {
        public Guid Id { get; set; }
        public Guid OfferId { get; set; }
        public SupplierOffer Offer { get; set; } = null!;
        public int BatchNumber { get; set; }
        public int TargetQuantity { get; set; }
        public int CurrentQuantity { get; set; }
        public BatchStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public ICollection<BatchParticipant> Participants { get; set; } = [];
    }
}
