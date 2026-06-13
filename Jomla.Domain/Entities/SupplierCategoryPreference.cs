namespace Jomla.Domain.Entities
{
    public class SupplierCategoryPreference
    {
        public Guid SupplierId { get; set; }
        public AppUser Supplier { get; set; } = null!;
        public Guid CategoryId { get; set; }
        public Category Category { get; set; } = null!;
        public int MinQuantity { get; set; }
    }
}
