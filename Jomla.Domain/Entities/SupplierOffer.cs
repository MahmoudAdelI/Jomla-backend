namespace Jomla.Domain.Entities
{
    public class SupplierOffer
    {
        public Guid Id { get; set; }
        public Guid SupplierId { get; set; }
        public AppUser Supplier { get; set; } = null!;
        public Guid CategoryId { get; set; }
        public Category Category { get; set; } = null!;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountPercentage { get; set; }
        public int BatchTargetQuantity { get; set; }
        public int TotalQuantityAvailable { get; set; }
        public int? MinFallbackQuantity { get; set; }
        public string? VariantAttributes { get; set; }   // JSON
        public string? ImageUrls { get; set; }            // JSON array
        public SellerOfferStatus Status { get; set; }
        public ModerationStatus ModerationStatus { get; set; }
        public string? ModerationReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public ICollection<SupplierBatch> Batches { get; set; } = [];
    }
}
