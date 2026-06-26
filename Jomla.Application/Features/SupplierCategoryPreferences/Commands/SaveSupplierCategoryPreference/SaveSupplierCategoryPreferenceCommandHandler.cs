using Jomla.Application.Common.Exceptions;
using Jomla.Application.Common.Interfaces;
using Jomla.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jomla.Application.Features.SupplierCategoryPreferences.Commands.SaveSupplierCategoryPreference;

public sealed class SaveSupplierCategoryPreferenceCommandHandler(IAppDbContext db)
    : IRequestHandler<SaveSupplierCategoryPreferenceCommand, bool>
{
    private readonly IAppDbContext _db = db;

    public async Task<bool> Handle(
        SaveSupplierCategoryPreferenceCommand request,
        CancellationToken cancellationToken)
    {
        var categoryExists = await _db.Categories
            .AnyAsync(c => c.Id == request.CategoryId, cancellationToken);

        if (!categoryExists)
        {
            throw new NotFoundException(nameof(Category), request.CategoryId);
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

        return true;
    }
}
