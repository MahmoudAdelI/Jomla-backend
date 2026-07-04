using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.GroupRequests.Dtos;
using Jomla.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Jomla.Application.Features.GroupRequests.Queries
{
    public sealed class GetGroupRequestDetailQueryHandler : IRequestHandler<GetGroupRequestDetailQuery, GroupRequestDetailDto?>
    {
        private readonly IAppDbContext _context;

        public GetGroupRequestDetailQueryHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<GroupRequestDetailDto?> Handle(GetGroupRequestDetailQuery request, CancellationToken cancellationToken)
        {
            var data = await _context.GroupRequests
                .Where(r => r.Id == request.GroupRequestId)
                .Select(r => new
                {
                    r.Id,
                    r.Title,
                    r.Description,
                    r.ImageUrls,
                    r.CurrentQuantity,
                    Status = r.Status.ToString(),
                    ModerationStatus = r.ModerationStatus.ToString(),
                    r.ModerationReason,
                    r.CreatedAt,
                    r.InitiatorId,
                    InitiatorName = r.Initiator != null ? (r.Initiator.FirstName + " " + r.Initiator.LastName).Trim() : "Unknown",
                    CategoryName = r.Category.Name,
                    ParticipantsCount = r.Participants.Count(p => p.Status == GroupRequestParticipantStatus.Active),
                    Offers = r.Offers.Select(o => new GroupRequestOfferDto(
                        o.Id,
                        o.SupplierId,
                        o.Supplier != null ? (o.Supplier.FirstName + " " + o.Supplier.LastName).Trim() : "Unknown Supplier",
                        o.UnitPrice,
                        o.MinUnitPrice,
                        o.CurrentUnitPrice,
                        o.QuantityAvailable,
                        o.MinFallbackQuantity,
                        o.AcceptedQuantity,
                        o.Status.ToString(),
                        o.CreatedAt,
                        o.ExpiresAt,
                        o.RoundNumber,
                        o.Responses.Where(res => res.Response == BuyerOfferResponseType.Accepted).Select(res => res.BuyerId).ToList(),
                        o.Responses.Where(res => res.Response == BuyerOfferResponseType.Rejected).Select(res => res.BuyerId).ToList()
                    )).ToList(),
                    Participants = r.Participants
                        .Where(p => p.Status == GroupRequestParticipantStatus.Active)
                        .Select(p => new GroupRequestParticipantDto(
                            p.BuyerId,
                            p.Buyer != null ? (p.Buyer.FirstName + " " + p.Buyer.LastName).Trim() : "Unknown",
                            p.Quantity
                        ))
                        .ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (data == null)
                return null;

            var imageUrlsList = string.IsNullOrEmpty(data.ImageUrls)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(data.ImageUrls) ?? new List<string>();

            return new GroupRequestDetailDto(
                data.Id,
                data.Title,
                data.Description,
                imageUrlsList,
                data.CurrentQuantity,
                data.Status,
                data.ModerationStatus,
                data.ModerationReason,
                data.CreatedAt,
                data.InitiatorId,
                data.InitiatorName,
                data.CategoryName,
                data.ParticipantsCount,
                data.Offers,
                data.Participants
            );
        }
    }
}
