using Jomla.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jomla.Infrastructure.Persistance.Configurations
{
    public class BatchParticipantConfiguration : IEntityTypeConfiguration<BatchParticipant>

    {
        public void Configure(EntityTypeBuilder<BatchParticipant> builder)
        {
            builder.ToTable("batch_participants");

            builder.HasKey(x => new { x.BatchId, x.BuyerId });

            builder.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(x => x.StripePaymentIntentId)
                .HasMaxLength(255);

            builder.Property(x => x.JoinedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.HasOne(x => x.Buyer)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
