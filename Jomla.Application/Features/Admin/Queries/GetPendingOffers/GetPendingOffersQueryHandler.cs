using Jomla.Application.Common.BaseClass;
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

namespace Jomla.Application.Features.Admin.Queries.GetPendingOffers
{
    public sealed class GetPendingOffersQueryHandler : IRequestHandler<GetPendingOffersQuery, PagedResponse<FlaggedOfferDto>>
    {
        private readonly IAppDbContext _context;

        public GetPendingOffersQueryHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResponse<FlaggedOfferDto>> Handle(GetPendingOffersQuery request, CancellationToken cancellationToken)
        {
            var pageNumber = request.PageNumber ?? 1;
            var pageSize = request.PageSize ?? 10;

            var query = _context.SupplierOffers
                .Where(o => o.ModerationStatus == ModerationStatus.Pending);
                         //&& o.CreatedAt < DateTime.UtcNow.AddMinutes(-30));

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new FlaggedOfferDto(
                    o.Id,
                    o.Title,
                    o.Description,
                    o.ModerationReason ?? "",
                    o.CreatedAt,
                    o.SupplierId
                ))
                .ToListAsync(cancellationToken);

            return new PagedResponse<FlaggedOfferDto>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };
        }
    }
}
