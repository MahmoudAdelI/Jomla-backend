using Jomla.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jomla.Infrastructure.Persistance.Configurations
{
    public class SupplierCategoryPreferenceConfiguration : IEntityTypeConfiguration<SupplierCategoryPreference>
    {
        public void Configure(EntityTypeBuilder<SupplierCategoryPreference> builder)
        {
            builder.ToTable("supplier_category_preferences");

            builder.HasKey(x => new { x.SupplierId, x.CategoryId });

            builder.HasOne(x => x.Category)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
