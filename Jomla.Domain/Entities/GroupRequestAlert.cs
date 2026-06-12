namespace Jomla.Domain.Entities
{
    public class GroupRequestAlert
    {
        public Guid GroupRequestId { get; set; }
        public GroupRequest GroupRequest { get; set; } = null!;
        public Guid SupplierId { get; set; }
        public AppUser Supplier { get; set; } = null!;
        public GroupRequestAlertStatus Status { get; set; }
        public DateTime NotifiedAt { get; set; }
    }
}
