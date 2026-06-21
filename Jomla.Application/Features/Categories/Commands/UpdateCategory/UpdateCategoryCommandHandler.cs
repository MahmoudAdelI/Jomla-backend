using Jomla.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jomla.Application.Features.Categories.Commands.UpdateCategory;

public sealed class UpdateCategoryCommandHandler(IAppDbContext db): IRequestHandler<UpdateCategoryCommand, bool>
{
    private readonly IAppDbContext _db = db;

public async Task<bool> Handle(
    UpdateCategoryCommand request,
    CancellationToken cancellationToken)
    {
        var category = await _db.Categories
            .FirstOrDefaultAsync(
                c => c.Id == request.Id,
                cancellationToken);

        if (category is null)
            return false;

        if (request.ParentId.HasValue)
        {
            var parentExists = await _db.Categories
                .AnyAsync(
                    c => c.Id == request.ParentId.Value,
                    cancellationToken);

            if (!parentExists)
                throw new ArgumentException("Parent category not found.");
        }

        category.Name = request.Name.Trim();
        category.ParentId = request.ParentId;

        await _db.SaveChangesAsync(cancellationToken);

        return true;
    }

}
