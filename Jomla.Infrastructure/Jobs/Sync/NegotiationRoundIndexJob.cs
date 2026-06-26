using System;
using System.Linq;
using System.Threading.Tasks;
using Jomla.Application.Common.Interfaces;
using Jomla.Application.Jobs.Sync;
using Jomla.Domain;
using Microsoft.EntityFrameworkCore;

namespace Jomla.Infrastructure.Jobs.Sync
{
    public class NegotiationRoundIndexJob(
        IAppDbContext db,
        INegotiationRoundIndexer roundIndexer) : INegotiationRoundIndexJob
    {
        private readonly IAppDbContext _db = db;
        private readonly INegotiationRoundIndexer _roundIndexer = roundIndexer;

        public async Task ExcuteAsync(Guid offerId)
        {
            var offer = await _db.GroupRequestOffers
                .Include(o => o.Responses)
                .Include(o => o.GroupRequest)
                    .ThenInclude(gr => gr.Participants)
                .Include(o => o.GroupRequest)
                    .ThenInclude(gr => gr.Category)
                .FirstOrDefaultAsync(o => o.Id == offerId);

            if (offer is null) return;

            var categoryName = offer.GroupRequest?.Category?.Name ?? "General";
            var totalParticipants = offer.GroupRequest?.Participants
                .Count(p => p.Status == GroupRequestParticipantStatus.Active) ?? 0;

            await _roundIndexer.IndexAsync(offer, categoryName, totalParticipants);
        }
    }
}
