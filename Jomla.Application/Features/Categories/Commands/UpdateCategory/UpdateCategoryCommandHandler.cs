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
            if (request.ParentId.Value == request.Id)
            {
                throw new ArgumentException("A category cannot be its own parent.");
            }

            var isDescendant = await IsDescendantOfAsync(request.ParentId.Value, request.Id, cancellationToken);
            if (isDescendant)
            {
                throw new ArgumentException("Proposed parent category is a child of this category, which creates a cycle.");
            }

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

    private async Task<bool> IsDescendantOfAsync(Guid targetParentId, Guid currentCategoryId, CancellationToken cancellationToken)
    {
        var category = await _db.Categories
            .FirstOrDefaultAsync(c => c.Id == targetParentId, cancellationToken);

        if (category == null) return false;

        var currentParentId = category.ParentId;
        while (currentParentId.HasValue)
        {
            if (currentParentId.Value == currentCategoryId)
            {
                return true;
            }

            var parent = await _db.Categories
                .FirstOrDefaultAsync(c => c.Id == currentParentId.Value, cancellationToken);

            currentParentId = parent?.ParentId;
        }

        return false;
    }
}
