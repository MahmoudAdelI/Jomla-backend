using MediatR;
using System;
using System.Collections.Generic;

namespace Jomla.Application.Features.Batches.Queries.GetCompletedDeals
{
    public record GetCompletedDealsQuery(Guid SupplierId) : IRequest<CompletedDealsResult>;

    public record CompletedDealsResult(
        decimal TotalRevenue,
        int TotalUnitsSold,
        int TotalBatchesClosed,
        int TotalBuyerCommitments,
        double AvgUnitsPerBatch,
        List<CompletedDealDto> Deals
    );

    public record CompletedDealDto(
        Guid Id,
        string OfferTitle,
        int BatchNumber,
        int BuyerCount,
        int TotalUnits,
        decimal TotalValue,
        DateTime? CompletedAt,
        List<DealBuyerDto> Buyers
    );

    public record DealBuyerDto(
        string Name,
        int Quantity
    );
}
