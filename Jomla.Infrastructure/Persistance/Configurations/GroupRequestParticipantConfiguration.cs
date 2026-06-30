using Jomla.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jomla.Infrastructure.Persistance.Configurations
{
    public class GroupRequestParticipantConfiguration : IEntityTypeConfiguration<GroupRequestParticipant>
    {
        public void Configure(EntityTypeBuilder<GroupRequestParticipant> builder)
        {
            builder.ToTable("group_request_participants");

            builder.HasKey(x => new { x.GroupRequestId, x.BuyerId });

            builder.Property(x => x.JoinedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.HasOne(x => x.Buyer)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
