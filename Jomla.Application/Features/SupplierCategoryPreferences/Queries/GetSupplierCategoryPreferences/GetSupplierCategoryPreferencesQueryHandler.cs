using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.SupplierCategoryPreferences.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jomla.Application.Features.SupplierCategoryPreferences.Queries.GetSupplierCategoryPreferences;

public sealed class GetSupplierCategoryPreferencesQueryHandler(IAppDbContext db)
    : IRequestHandler<GetSupplierCategoryPreferencesQuery, List<SupplierCategoryPreferenceDto>>
{
    private readonly IAppDbContext _db = db;

    public async Task<List<SupplierCategoryPreferenceDto>> Handle(
        GetSupplierCategoryPreferencesQuery request,
        CancellationToken cancellationToken)
    {
        var preferences = await _db.SupplierCategoryPreferences
            .Include(p => p.Category)
            .Where(p => p.SupplierId == request.SupplierId)
            .ToListAsync(cancellationToken);

        return preferences.Select(p => new SupplierCategoryPreferenceDto(
            p.CategoryId,
            p.Category.Name,
            p.MinQuantity
        )).ToList();
    }
}
