using MediatR;

namespace Jomla.Application.Features.Categories.Commands.UpdateCategory;

public sealed record UpdateCategoryCommand(Guid Id,string Name,Guid? ParentId) : IRequest<bool>;
