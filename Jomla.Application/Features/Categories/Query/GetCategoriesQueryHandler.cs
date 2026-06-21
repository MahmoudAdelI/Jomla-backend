using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.Categories.Dto;
using Jomla.Application.Features.Categories.Query;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jomla.Application.Features.Categories.Queries.GetCategories;

public sealed class GetCategoriesQueryHandler(IAppDbContext db)
    : IRequestHandler<GetCategoriesQuery, List<CategoryDto>>
{
    private readonly IAppDbContext _db = db;

    public async Task<List<CategoryDto>> Handle(
        GetCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var categories = await _db.Categories
            .Select(c => new CategoryDto(
                c.Id,
                c.Name,
                c.ParentId))
            .ToListAsync(cancellationToken);

        return categories;
    }
}