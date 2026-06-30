using Jomla.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jomla.Infrastructure.Persistance.Configurations
{
    public class GroupRequestConfiguration : IEntityTypeConfiguration<GroupRequest>
    {
        public void Configure(EntityTypeBuilder<GroupRequest> builder)
        {
            builder.ToTable("group_requests");

            builder.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(x => x.ModerationStatus)
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(x => x.ModerationReason)
                .HasMaxLength(1000);

            builder.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.HasOne(x => x.Category)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
