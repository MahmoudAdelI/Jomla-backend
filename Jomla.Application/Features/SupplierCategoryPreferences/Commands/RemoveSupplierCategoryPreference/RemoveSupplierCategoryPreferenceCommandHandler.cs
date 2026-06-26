using Jomla.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jomla.Application.Features.SupplierCategoryPreferences.Commands.RemoveSupplierCategoryPreference;

public sealed class RemoveSupplierCategoryPreferenceCommandHandler(IAppDbContext db)
    : IRequestHandler<RemoveSupplierCategoryPreferenceCommand, RemoveSupplierCategoryPreferenceResult>
{
    private readonly IAppDbContext _db = db;

    public async Task<RemoveSupplierCategoryPreferenceResult> Handle(
        RemoveSupplierCategoryPreferenceCommand request,
        CancellationToken cancellationToken)
    {
        var preference = await _db.SupplierCategoryPreferences
            .FirstOrDefaultAsync(p => p.SupplierId == request.SupplierId && p.CategoryId == request.CategoryId, cancellationToken);

        if (preference == null)
        {
            return new RemoveSupplierCategoryPreferenceResult(false, "Category preference not found.");
        }

        _db.SupplierCategoryPreferences.Remove(preference);
        await _db.SaveChangesAsync(cancellationToken);

        return new RemoveSupplierCategoryPreferenceResult(true);
    }
}
