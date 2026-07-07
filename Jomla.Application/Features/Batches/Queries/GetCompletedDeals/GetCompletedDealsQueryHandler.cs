using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jomla.Application.Common.Interfaces;
using Jomla.Domain;
using Jomla.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jomla.Application.Features.Batches.Queries.GetCompletedDeals
{
    public class GetCompletedDealsQueryHandler : IRequestHandler<GetCompletedDealsQuery, CompletedDealsResult>
    {
        private readonly IAppDbContext _context;

        public GetCompletedDealsQueryHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<CompletedDealsResult> Handle(GetCompletedDealsQuery request, CancellationToken cancellationToken)
        {
            var completedBatches = await _context.SupplierBatches
                .Include(b => b.Offer)
                .Include(b => b.Participants)
                    .ThenInclude(p => p.Buyer)
                .Where(b => b.Status == BatchStatus.Completed && b.Offer.SupplierId == request.SupplierId)
                .OrderByDescending(b => b.CompletedAt)
                .ToListAsync(cancellationToken);

            var dealsList = completedBatches.Select(b =>
            {
                var finalPrice = b.Offer.UnitPrice * (1 - (b.Offer.DiscountPercentage / 100m));
                var totalValue = b.CurrentQuantity * finalPrice;
                var buyers = b.Participants
                    .Where(p => p.Status == BatchParticipantStatus.Active)
                    .Select(p => {
                        var name = $"{p.Buyer.FirstName} {p.Buyer.LastName}".Trim();
                        if (string.IsNullOrWhiteSpace(name))
                        {
                            name = p.Buyer.Email ?? "Unknown Buyer";
                        }
                        var email = p.Buyer.Email ?? string.Empty;
                        var phone = p.PhoneNumber ?? p.Buyer.PhoneNumber;
                        var address = p.ShippingAddress ?? p.Buyer.ShippingAddress;
                        return new DealBuyerDto(name, p.Quantity, email, phone, address);
                    }).ToList();

                return new CompletedDealDto(
                    b.Id,
                    b.Offer.Title,
                    b.BatchNumber,
                    buyers.Count,
                    b.CurrentQuantity,
                    totalValue,
                    b.CompletedAt,
                    buyers
                );
            }).ToList();

            var totalRevenue = dealsList.Sum(d => d.TotalValue);
            var totalUnitsSold = dealsList.Sum(d => d.TotalUnits);
            var totalBatchesClosed = dealsList.Count;
            var totalBuyerCommitments = dealsList.Sum(d => d.BuyerCount);
            var avgUnitsPerBatch = totalBatchesClosed > 0 ? (double)totalUnitsSold / totalBatchesClosed : 0.0;

            return new CompletedDealsResult(
                totalRevenue,
                totalUnitsSold,
                totalBatchesClosed,
                totalBuyerCommitments,
                avgUnitsPerBatch,
                dealsList
            );
        }
    }
}
