using Jomla.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jomla.Infrastructure.Persistance.Configurations
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.ToTable("notifications");

            builder.Property(x => x.Type)
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(x => x.Body)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(x => x.EntityType)
                .HasMaxLength(50);

            builder.Property(x => x.CreatedAt)
                .HasDefaultValueSql("getdate()");

            builder.HasOne(x => x.User)
                .WithMany()
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
