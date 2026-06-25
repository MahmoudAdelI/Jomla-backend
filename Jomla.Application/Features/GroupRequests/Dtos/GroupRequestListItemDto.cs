using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Application.Features.GroupRequests.Dtos
{
    public sealed record GroupRequestListItemDto(
    Guid Id,
    string Title,
    string? Description,
    int CurrentQuantity,
    string Status,
    string CategoryName,
    DateTime CreatedAt,
    int ParticipantsCount
    );
}
