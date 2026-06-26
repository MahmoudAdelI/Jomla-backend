using Jomla.Application.Common.Exceptions;
using Jomla.Application.Common.Interfaces;
using Jomla.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jomla.Application.Features.SupplierCategoryPreferences.Commands.RemoveSupplierCategoryPreference;

public sealed class RemoveSupplierCategoryPreferenceCommandHandler(IAppDbContext db)
    : IRequestHandler<RemoveSupplierCategoryPreferenceCommand, bool>
{
    private readonly IAppDbContext _db = db;

    public async Task<bool> Handle(
        RemoveSupplierCategoryPreferenceCommand request,
        CancellationToken cancellationToken)
    {
        var preference = await _db.SupplierCategoryPreferences
            .FirstOrDefaultAsync(p => p.SupplierId == request.SupplierId && p.CategoryId == request.CategoryId, cancellationToken);

        if (preference == null)
        {
            throw new NotFoundException(nameof(SupplierCategoryPreference), $"{request.SupplierId}-{request.CategoryId}");
        }

        _db.SupplierCategoryPreferences.Remove(preference);
        await _db.SaveChangesAsync(cancellationToken);

        return true;
    }
}
