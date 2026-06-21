using Jomla.Application.Features.Categories.Dto;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Application.Features.Categories.Query
{
    public sealed record GetCategoriesQuery()
        : IRequest<List<CategoryDto>>;
}
