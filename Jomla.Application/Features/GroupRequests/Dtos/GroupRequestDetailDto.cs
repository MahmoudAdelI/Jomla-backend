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
     List<string> ImageUrls,
     int CurrentQuantity,
     string Status,
     string ModerationStatus,
     string? ModerationReason,
     DateTime CreatedAt,
     Guid InitiatorId,
     string InitiatorName,
     string CategoryName,
     int ParticipantsCount,
     List<GroupRequestOfferDto> Offers,
     List<GroupRequestParticipantDto> Participants
    );

    public record GroupRequestParticipantDto(
        Guid Id,
        string Name,
        int Quantity
    );
}
