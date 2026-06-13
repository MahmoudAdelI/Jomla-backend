namespace Jomla.Domain.Entities
{
    public class GroupRequestOffer
    {
        public Guid Id { get; set; }
        public Guid GroupRequestId { get; set; }
        public GroupRequest GroupRequest { get; set; } = null!;
        public Guid SupplierId { get; set; }
        public AppUser Supplier { get; set; } = null!;
        public decimal UnitPrice { get; set; }
        public decimal? MinUnitPrice { get; set; }
        public decimal CurrentUnitPrice { get; set; } // For tracking price changes during negotiation
        public int QuantityAvailable { get; set; }
        public int? MinFallbackQuantity { get; set; }
        public string? VariantAttributes { get; set; }   // JSON
        public GroupRequestOfferStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public ICollection<BuyerOfferResponse> Responses { get; set; } = [];
        public ICollection<NegotiationLog> NegotiationLogs { get; set; } = [];
    }
}
