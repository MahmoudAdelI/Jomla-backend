using Jomla.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jomla.Infrastructure.Persistance.Configurations
{
    public class SupplierBatchConfiguration : IEntityTypeConfiguration<SupplierBatch>
    {
        public void Configure(EntityTypeBuilder<SupplierBatch> builder)
        {
            builder.ToTable("supplier_batches");

            builder.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(x => x.CreatedAt)
                .HasDefaultValueSql("getdate()");
        }
    }
}
