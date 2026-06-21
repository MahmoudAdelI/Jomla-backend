using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.Categories.Dto;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jomla.Application.Features.Categories.Queries.GetCategoryById;

public sealed class GetCategoryByIdQueryHandler(IAppDbContext db)
    : IRequestHandler<GetCategoryByIdQuery, CategoryDto?>
{
    private readonly IAppDbContext _db = db;

    public async Task<CategoryDto?> Handle(
        GetCategoryByIdQuery request,
        CancellationToken cancellationToken)
    {
        return await _db.Categories
            .Where(c => c.Id == request.Id)
            .Select(c => new CategoryDto(
                c.Id,
                c.Name,
                c.ParentId))
            .FirstOrDefaultAsync(cancellationToken);
    }
}