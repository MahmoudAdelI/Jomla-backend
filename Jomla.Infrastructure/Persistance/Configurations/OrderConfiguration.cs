using Jomla.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jomla.Infrastructure.Persistance.Configurations
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.ToTable("orders");

            builder.Property(x => x.TotalAmount)
                .HasColumnType("decimal(10,2)");

            builder.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(x => x.CreatedAt)
                .HasDefaultValueSql("getdate()");

            builder.HasOne(x => x.Buyer)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.ToTable(t => t.HasCheckConstraint(
                "CK_Orders_BatchOrOffer",
                "([BatchId] IS NOT NULL AND [OfferId] IS NULL) OR ([BatchId] IS NULL AND [OfferId] IS NOT NULL)"
            ));
        }
    }
}
