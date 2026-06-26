using MediatR;

namespace Jomla.Application.Features.SupplierCategoryPreferences.Commands.RemoveSupplierCategoryPreference;

public sealed record RemoveSupplierCategoryPreferenceCommand(
    Guid SupplierId,
    Guid CategoryId
) : IRequest<RemoveSupplierCategoryPreferenceResult>;

public sealed record RemoveSupplierCategoryPreferenceResult(bool Success, string? Error = null);
