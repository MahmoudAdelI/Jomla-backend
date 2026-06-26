namespace Jomla.Application.Features.SupplierCategoryPreferences.DTOs;

public sealed record SupplierCategoryPreferenceDto(
    Guid CategoryId,
    string CategoryName,
    int MinQuantity
);
