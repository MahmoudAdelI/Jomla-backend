using MediatR;

namespace Jomla.Application.Features.SupplierCategoryPreferences.Commands.SaveSupplierCategoryPreference;

public sealed record SaveSupplierCategoryPreferenceCommand(
    Guid SupplierId,
    Guid CategoryId,
    int MinQuantity
) : IRequest<bool>;
