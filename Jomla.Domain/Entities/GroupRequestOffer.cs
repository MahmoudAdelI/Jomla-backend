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
        public int RoundNumber { get; set; } = 1; // For tracking negotiation rounds
        public GroupRequestOfferStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public Guid? ParentId { get; set; } // For tracking offer revisions during negotiation
        public GroupRequestOffer? Parent { get; set; } 
        public ICollection<GroupRequestOffer> Children { get; set; } = []; // For tracking offer revisions during negotiation
        public ICollection<BuyerOfferResponse> Responses { get; set; } = [];
        public ICollection<NegotiationLog> NegotiationLogs { get; set; } = [];
    }
}
