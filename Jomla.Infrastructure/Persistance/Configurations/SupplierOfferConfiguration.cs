using Jomla.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jomla.Infrastructure.Persistance.Configurations
{
    public class SupplierOfferConfiguration : IEntityTypeConfiguration<SupplierOffer>
    {
        public void Configure(EntityTypeBuilder<SupplierOffer> builder)
        {
            builder.ToTable("supplier_offers");

            builder.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(x => x.Description)
                .HasMaxLength(1000);

            builder.Property(x => x.UnitPrice)
                .HasColumnType("decimal(10,2)");

            builder.Property(x => x.DiscountPercentage)
                .HasColumnType("decimal(5,2)");

            builder.Property(x => x.VariantAttributes)
                .HasColumnType("nvarchar(max)");

            builder.Property(x => x.ImageUrls)
                .HasColumnType("nvarchar(max)");

            builder.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(x => x.ModerationStatus)
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(x => x.ModerationReason)
                .HasMaxLength(1000);

            builder.Property(x => x.CreatedAt)
                .HasDefaultValueSql("getdate()");

            builder.HasOne(x => x.Category)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
