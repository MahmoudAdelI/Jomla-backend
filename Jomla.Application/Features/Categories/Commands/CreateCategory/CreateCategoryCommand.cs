using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediatR;

namespace Jomla.Application.Features.Categories.Commands.CreateCategory;

public sealed record CreateCategoryCommand(string Name,Guid? ParentId) : IRequest<Guid>;

