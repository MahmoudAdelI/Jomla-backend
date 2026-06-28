using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.Categories.Dto;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Jomla.Application.Features.Categories.Query
{
    public sealed class DetectCategoryQueryHandler(IAppDbContext db, ICategoryAgent categoryAgent)
        : IRequestHandler<DetectCategoryQuery, CategoryDto?>
    {
        private readonly IAppDbContext _db = db;
        private readonly ICategoryAgent _categoryAgent = categoryAgent;

        public async Task<CategoryDto?> Handle(DetectCategoryQuery request, CancellationToken cancellationToken)
        {
            var categories = await _db.Categories
                .Include(c => c.Parent)
                .ToListAsync(cancellationToken);

            if (categories.Count == 0) return null;

            var categoryDtos = categories.Select(c => new Jomla.Application.Common.DTOs.CategoryDto(
                c.Id,
                c.Parent != null ? $"{c.Parent.Name} : {c.Name}" : c.Name
            ));

            try
            {
                var detectedId = await _categoryAgent.ResolveCategoryAsync(request.Title, categoryDtos, cancellationToken);
                var matchedCategory = categories.FirstOrDefault(c => c.Id == detectedId);
                
                if (matchedCategory == null) return null;

                return new CategoryDto(
                    matchedCategory.Id,
                    matchedCategory.Name,
                    matchedCategory.ParentId
                );
            }
            catch
            {
                // Fallback to "Other" or first category on agent error
                var fallbackCategory = categories.FirstOrDefault(c => c.Name.Equals("Other", System.StringComparison.OrdinalIgnoreCase))
                    ?? categories.FirstOrDefault();

                if (fallbackCategory == null) return null;

                return new CategoryDto(
                    fallbackCategory.Id,
                    fallbackCategory.Name,
                    fallbackCategory.ParentId
                );
            }
        }
    }
}
