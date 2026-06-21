using Jomla.Application.Common.DTOs;

namespace Jomla.Application.Common.Interfaces
{
    public interface ICategoryAgent
    {
        Task<Guid> ResolveCategoryAsync(string itemTitle, IEnumerable<CategoryDto> categories, CancellationToken ct = default);
    }
}
