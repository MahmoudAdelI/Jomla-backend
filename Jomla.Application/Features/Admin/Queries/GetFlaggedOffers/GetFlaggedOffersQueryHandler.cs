using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.Admin.Dtos;
using Jomla.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Application.Features.Admin.Queries.GetFlaggedOffers
{
    public sealed class GetFlaggedOffersQueryHandler : IRequestHandler<GetFlaggedOffersQuery, PagedResult<FlaggedOfferDto>>
    {
        private readonly IAppDbContext _context;

        public GetFlaggedOffersQueryHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<FlaggedOfferDto>> Handle(GetFlaggedOffersQuery request, CancellationToken cancellationToken)
        {
            var query = _context.SupplierOffers
                .Where(o => o.ModerationStatus == ModerationStatus.Flagged);

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var searchTerm = request.Search.Trim().ToLower();
                query = query.Where(o => o.Title.ToLower().Contains(searchTerm) || 
                                         (o.Description != null && o.Description.ToLower().Contains(searchTerm)) ||
                                         (o.ModerationReason != null && o.ModerationReason.ToLower().Contains(searchTerm)));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(o => new FlaggedOfferDto(
                    o.Id,
                    o.Title,
                    o.Description,
                    o.ModerationReason ?? "",
                    o.CreatedAt,
                    o.SupplierId
                ))
                .ToListAsync(cancellationToken);

            return new PagedResult<FlaggedOfferDto>(items, totalCount, request.Page, request.PageSize);
        }
    }
}
