using Jomla.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jomla.Infrastructure.Persistance.Configurations
{
    public class UserContactInfoConfiguration : IEntityTypeConfiguration<UserContactInfo>
    {
        public void Configure(EntityTypeBuilder<UserContactInfo> builder)
        {
            builder.ToTable("user_contact_info");

            builder.HasKey(x => x.UserId);

            builder.Property(x => x.ShippingAddress)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(x => x.PhoneNumber)
                .IsRequired()
                .HasMaxLength(50);

            builder.HasOne(x => x.User)
                .WithOne(u => u.ContactInfo)
                .HasForeignKey<UserContactInfo>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
