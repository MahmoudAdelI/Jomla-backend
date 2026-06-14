using Jomla.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jomla.Infrastructure.Persistance.Configurations
{
    public class GroupRequestOfferConfiguration : IEntityTypeConfiguration<GroupRequestOffer>
    {
        public void Configure(EntityTypeBuilder<GroupRequestOffer> builder)
        {
            builder.ToTable("group_request_offers");

            builder.Property(x => x.UnitPrice)
                .HasColumnType("decimal(10,2)");

            builder.Property(x => x.MinUnitPrice)
                .HasColumnType("decimal(10,2)");

            builder.Property(x => x.CurrentUnitPrice)
                .HasColumnType("decimal(10,2)");

            builder.Property(x => x.VariantAttributes)
                .HasColumnType("nvarchar(max)");

            builder.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(x => x.CreatedAt)
                .HasDefaultValueSql("getdate()");

            builder.HasOne(x => x.Supplier)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Parent)
                .WithMany(x => x.Children)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
