using Jomla.Application.Features.Categories.Dto;
using MediatR;

namespace Jomla.Application.Features.Categories.Query
{
    public sealed record DetectCategoryQuery(string Title) : IRequest<CategoryDto?>;
}
