using Jomla.Application.Features.SupplierCategoryPreferences.DTOs;
using MediatR;

namespace Jomla.Application.Features.SupplierCategoryPreferences.Queries.GetSupplierCategoryPreferences;

public sealed record GetSupplierCategoryPreferencesQuery(Guid SupplierId) 
    : IRequest<List<SupplierCategoryPreferenceDto>>;
