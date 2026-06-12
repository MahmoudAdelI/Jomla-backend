namespace Jomla.Domain.Entities
{
    public class GroupRequest
    {
        public Guid Id { get; set; }
        public Guid InitiatorId { get; set; }
        public AppUser Initiator { get; set; } = null!;
        public Guid CategoryId { get; set; }
        public Category Category { get; set; } = null!;
        public string ItemTitle { get; set; } = string.Empty;
        public int CurrentQuantity { get; set; }
        public GroupRequestStatus Status { get; set; }
        public ModerationStatus ModerationStatus { get; set; }
        public string? ModerationReason { get; set; }
        public DateTime? InactiveSince { get; set; }
        public DateTime CreatedAt { get; set; }
        public ICollection<GroupRequestParticipant> Participants { get; set; } = [];
        public ICollection<GroupRequestOffer> Offers { get; set; } = [];
        public ICollection<GroupRequestAlert> Alerts { get; set; } = [];
    }
}
