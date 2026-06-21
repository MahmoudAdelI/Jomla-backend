using Jomla.Application.Common.Interfaces;
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

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            ParentId = request.ParentId
        };

        _db.Categories.Add(category);

        await _db.SaveChangesAsync(cancellationToken);

        return category.Id;
    }
}