using Jomla.Application.Features.Batches.DTOs;
using MediatR;
using System;
using System.Collections.Generic;

namespace Jomla.Application.Features.Batches.Queries.SearchBatches
{
    public sealed record SearchBatchesQuery(
        string? SearchTerm,
        string? Status,
        int Page = 1,
        int PageSize = 10
    ) : IRequest<PagedBatchesResult>;

    public sealed record PagedBatchesResult(
        List<BatchSearchItemDto> Items,
        int TotalCount,
        int Page,
        int PageSize
    );
}
