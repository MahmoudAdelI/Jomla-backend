using System;
using System.Linq;
using System.Threading.Tasks;
using Jomla.Application.Common.Interfaces;
using Jomla.Application.Jobs.Sync;
using Jomla.Domain;
using Microsoft.EntityFrameworkCore;

namespace Jomla.Infrastructure.Jobs.Sync
{
    public class NegotiationRoundSyncJob(
        IAppDbContext db,
        INegotiationRoundIndexer roundIndexer) : INegotiationRoundSyncJob
    {
        private readonly IAppDbContext _db = db;
        private readonly INegotiationRoundIndexer _roundIndexer = roundIndexer;

        public async Task ExcuteAsync()
        {
            var offers = await _db.GroupRequestOffers
                .Include(o => o.Responses)
                .Include(o => o.GroupRequest)
                    .ThenInclude(gr => gr.Participants)
                .Include(o => o.GroupRequest)
                    .ThenInclude(gr => gr.Category)
                .ToListAsync();

            foreach (var offer in offers)
            {
                var categoryName = offer.GroupRequest?.Category?.Name ?? "General";
                var totalParticipants = offer.GroupRequest?.Participants
                    .Count(p => p.Status == GroupRequestParticipantStatus.Active) ?? 0;

                try
                {
                    await _roundIndexer.IndexAsync(offer, categoryName, totalParticipants);
                }
                catch (Exception)
                {
                    // Continue indexing others even if one fails
                }
            }
        }
    }
}
