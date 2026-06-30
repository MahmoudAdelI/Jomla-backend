using Jomla.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jomla.Infrastructure.Persistance.Configurations
{
    public class GroupRequestAlertConfiguration : IEntityTypeConfiguration<GroupRequestAlert>
    {
        public void Configure(EntityTypeBuilder<GroupRequestAlert> builder)
        {
            builder.ToTable("group_request_alerts");

            builder.HasKey(x => new { x.GroupRequestId, x.SupplierId });

            builder.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(x => x.NotifiedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.HasOne(x => x.Supplier)
            .WithMany()
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
        }

    }
}
