namespace Jomla.Domain.Entities
{
    public class GroupRequestParticipant
    {
        public Guid GroupRequestId { get; set; }
        public GroupRequest GroupRequest { get; set; } = null!;
        public Guid BuyerId { get; set; }
        public AppUser Buyer { get; set; } = null!;
        public int Quantity { get; set; }
        public DateTime JoinedAt { get; set; }
    }
}
