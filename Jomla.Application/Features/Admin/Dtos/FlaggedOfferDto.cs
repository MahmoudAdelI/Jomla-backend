using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Application.Features.Admin.Dtos
{
    public sealed record FlaggedOfferDto(
     Guid Id,
     string Title,
     string? Description,
     string ModerationReason,
     DateTime CreatedAt,
     Guid SupplierId
 );
}
