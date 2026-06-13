using Jomla.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jomla.Infrastructure.Persistance.Configurations
{
    public class BuyerOfferResponseConfiguration : IEntityTypeConfiguration<BuyerOfferResponse>
    {
        public void Configure(EntityTypeBuilder<BuyerOfferResponse> builder)
        {
            builder.ToTable("buyer_offer_responses");

            builder.HasKey(x => new { x.OfferId, x.BuyerId });

            builder.Property(x => x.Response)
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(x => x.StripePaymentIntentId)
                .HasMaxLength(255);

            builder.Property(x => x.RespondedAt)
                .HasDefaultValueSql("getdate()");
            builder.HasOne(x => x.Buyer)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
