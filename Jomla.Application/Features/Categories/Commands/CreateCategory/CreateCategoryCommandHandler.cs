using Jomla.Application.Common.Interfaces;
using Jomla.Application.Common.Exceptions;
using Jomla.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jomla.Application.Features.Categories.Commands.CreateCategory;

public sealed class CreateCategoryCommandHandler(IAppDbContext db)
    : IRequestHandler<CreateCategoryCommand, Guid>
{
    private readonly IAppDbContext _db = db;

    public async Task<Guid> Handle(CreateCategoryCommand request,CancellationToken cancellationToken)
    {
        if (request.ParentId.HasValue)
        {
            var parentExists = await _db.Categories.AnyAsync(
                    c => c.Id == request.ParentId.Value,cancellationToken);

            if (!parentExists) throw new ArgumentException("Parent category not found.");
        }

        var normalizedName = request.Name.Trim().ToLower();
        var categoryExists = await _db.Categories.AnyAsync(
            c => c.Name.ToLower() == normalizedName && c.ParentId == request.ParentId,
            cancellationToken);

        if (categoryExists)
        {
            throw new ConflictException($"A category named '{request.Name.Trim()}' already exists under this parent.");
        }

        var category = new Category
        {
            Name = request.Name.Trim(),
            ParentId = request.ParentId
        };

        _db.Categories.Add(category);

        await _db.SaveChangesAsync(cancellationToken);

        return category.Id;
    }
}