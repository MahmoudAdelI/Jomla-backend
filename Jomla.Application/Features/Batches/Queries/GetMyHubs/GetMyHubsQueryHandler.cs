using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jomla.Application.Common.Interfaces;
using Jomla.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jomla.Application.Features.Batches.Queries.GetMyHubs
{
    public class GetMyHubsQueryHandler : IRequestHandler<GetMyHubsQuery, List<BuyerHubDto>>
    {
        private readonly IAppDbContext _context;

        public GetMyHubsQueryHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<List<BuyerHubDto>> Handle(GetMyHubsQuery request, CancellationToken cancellationToken)
        {
            var results = new List<BuyerHubDto>();

            // 1. Fetch joined supplier batches
            var supplierBatches = await _context.SupplierBatches
                .Include(b => b.Offer)
                .Include(b => b.Participants)
                .Where(b => b.Participants.Any(p => p.BuyerId == request.BuyerId && p.Status == BatchParticipantStatus.Active))
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync(cancellationToken);

            foreach (var batch in supplierBatches)
            {
                var userParticipant = batch.Participants.First(p => p.BuyerId == request.BuyerId && p.Status == BatchParticipantStatus.Active);
                results.Add(new BuyerHubDto
                {
                    Id = $"batch_{batch.Id}",
                    Type = "supplier_offer",
                    Title = batch.Offer.Title,
                    Status = batch.Status.ToString().ToLower(),
                    CommittedUnits = userParticipant.Quantity,
                    BatchId = batch.Id,
                    RequestId = null,
                    FillProgress = batch.CurrentQuantity,
                    FillTarget = batch.TargetQuantity
                });
            }

            // 2. Fetch joined/initiated group requests
            var groupRequests = await _context.GroupRequests
                .Include(r => r.Participants)
                .Where(r => r.InitiatorId == request.BuyerId || 
                            r.Participants.Any(p => p.BuyerId == request.BuyerId && p.Status == GroupRequestParticipantStatus.Active))
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync(cancellationToken);

            foreach (var req in groupRequests)
            {
                var userParticipant = req.Participants.FirstOrDefault(p => p.BuyerId == request.BuyerId && p.Status == GroupRequestParticipantStatus.Active);
                var committedUnits = userParticipant?.Quantity ?? req.CurrentQuantity;
                results.Add(new BuyerHubDto
                {
                    Id = $"request_{req.Id}",
                    Type = "group_request",
                    Title = req.Title,
                    Status = req.Status.ToString().ToLower(),
                    CommittedUnits = committedUnits,
                    BatchId = null,
                    RequestId = req.Id,
                    FillProgress = null,
                    FillTarget = null
                });
            }

            return results;
        }
    }
}
