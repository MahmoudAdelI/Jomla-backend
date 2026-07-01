namespace Jomla.Domain.Entities
{
    public class BuyerOfferResponse
    {
        public Guid OfferId { get; set; }
        public GroupRequestOffer Offer { get; set; } = null!;
        public Guid BuyerId { get; set; }
        public AppUser Buyer { get; set; } = null!;
        public BuyerOfferResponseType Response { get; set; }
        public int AcceptedQuantity { get; set; }
        public string? StripePaymentIntentId { get; set; }
        public DateTime RespondedAt { get; set; }
    }
}
