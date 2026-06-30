using Jomla.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jomla.Infrastructure.Persistance.Configurations
{
    public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
    {
        public void Configure(EntityTypeBuilder<AppUser> builder)
        {
            builder.ToTable("users");

            builder.Property(x => x.FirstName)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(x => x.LastName)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(x => x.ImageUrl)
                .HasMaxLength(500);

            builder.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.OwnsMany(x => x.RefreshTokens, rt =>
            {
                rt.ToTable("user_refresh_tokens");
                rt.Property(x => x.Token).HasMaxLength(255);
                rt.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            });
        }
    }
}
