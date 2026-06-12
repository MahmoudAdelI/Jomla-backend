namespace Jomla.Domain.Entities
{
    public class NegotiationLog
    {
        public Guid Id { get; set; }
        public Guid OfferId { get; set; }
        public GroupRequestOffer Offer { get; set; } = null!;
        public decimal PreviousPrice { get; set; }
        public decimal NewPrice { get; set; }
        public string? ReasoningSummary { get; set; }
        public DateTime ActedAt { get; set; }
    }
}
