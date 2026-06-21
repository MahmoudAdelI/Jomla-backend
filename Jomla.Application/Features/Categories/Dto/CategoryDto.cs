using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Application.Features.Categories.Dto
{
    public sealed record CategoryDto(
      Guid Id,
      string Name,
      Guid? ParentId
  );
}
