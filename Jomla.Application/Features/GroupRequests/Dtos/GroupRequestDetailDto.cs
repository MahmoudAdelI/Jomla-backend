using Jomla.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Application.Features.GroupRequests.Dtos
{
    public sealed record GroupRequestDetailDto(
     Guid Id,
     string Title,
     string? Description,
     string? ImageUrls,
     int CurrentQuantity,
     string Status,
     string ModerationStatus,
     string? ModerationReason,
     DateTime CreatedAt,
     Guid InitiatorId,
     string CategoryName,
     int ParticipantsCount
    );

}
