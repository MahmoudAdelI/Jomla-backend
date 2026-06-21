using MediatR;

namespace Jomla.Application.Features.Categories.Commands.DeleteCategory;

public sealed record DeleteCategoryCommand(Guid Id)
: IRequest<bool>;
