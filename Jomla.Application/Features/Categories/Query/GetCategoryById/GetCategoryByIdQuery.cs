using Jomla.Application.Features.Categories.Dto;
using MediatR;

namespace Jomla.Application.Features.Categories.Queries.GetCategoryById;

public sealed record GetCategoryByIdQuery(Guid Id): IRequest<CategoryDto?>;