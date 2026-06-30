using Jomla.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jomla.Infrastructure.Persistance.Configurations
{
    public class NegotiationLogConfiguration : IEntityTypeConfiguration<NegotiationLog>
    {
        public void Configure(EntityTypeBuilder<NegotiationLog> builder)
        {
            builder.ToTable("negotiation_logs");

            builder.Property(x => x.PreviousPrice)
                .HasColumnType("decimal(10,2)");

            builder.Property(x => x.NewPrice)
                .HasColumnType("decimal(10,2)");

            builder.Property(x => x.ReasoningSummary)
                .HasColumnType("nvarchar(max)");

            builder.Property(x => x.ActedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.HasOne(x => x.Offer)
                .WithMany(o => o.NegotiationLogs)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
