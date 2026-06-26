using Jomla.Application.Common.Interfaces;
using Jomla.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jomla.Application.Features.SupplierCategoryPreferences.Commands.SaveSupplierCategoryPreference;

public sealed class SaveSupplierCategoryPreferenceCommandHandler(IAppDbContext db)
    : IRequestHandler<SaveSupplierCategoryPreferenceCommand, SaveSupplierCategoryPreferenceResult>
{
    private readonly IAppDbContext _db = db;

    public async Task<SaveSupplierCategoryPreferenceResult> Handle(
        SaveSupplierCategoryPreferenceCommand request,
        CancellationToken cancellationToken)
    {
        if (request.MinQuantity < 1)
        {
            return new SaveSupplierCategoryPreferenceResult(false, "Minimum quantity must be at least 1.");
        }

        var categoryExists = await _db.Categories
            .AnyAsync(c => c.Id == request.CategoryId, cancellationToken);

        if (!categoryExists)
        {
            return new SaveSupplierCategoryPreferenceResult(false, "Category not found.");
        }

        var preference = await _db.SupplierCategoryPreferences
            .FirstOrDefaultAsync(p => p.SupplierId == request.SupplierId && p.CategoryId == request.CategoryId, cancellationToken);

        if (preference != null)
        {
            preference.MinQuantity = request.MinQuantity;
        }
        else
        {
            preference = new SupplierCategoryPreference
            {
                SupplierId = request.SupplierId,
                CategoryId = request.CategoryId,
                MinQuantity = request.MinQuantity
            };
            _db.SupplierCategoryPreferences.Add(preference);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new SaveSupplierCategoryPreferenceResult(true);
    }
}
