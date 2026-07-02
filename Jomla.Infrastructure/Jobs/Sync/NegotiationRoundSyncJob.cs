using System;
using System.Linq;
using System.Threading.Tasks;
using Jomla.Application.Common.Interfaces;
using Jomla.Application.Jobs.Sync;
using Jomla.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jomla.Infrastructure.Jobs.Sync
{
    public class NegotiationRoundSyncJob(
        IAppDbContext db,
        INegotiationRoundIndexer roundIndexer,
        ILogger<NegotiationRoundSyncJob> logger) : INegotiationRoundSyncJob
    {
        private readonly IAppDbContext _db = db;
        private readonly INegotiationRoundIndexer _roundIndexer = roundIndexer;
        private readonly ILogger<NegotiationRoundSyncJob> _logger = logger;

        public async Task ExcuteAsync()
        {
            var offers = await _db.GroupRequestOffers
                .AsSplitQuery()
                .Include(o => o.Responses)
                .Include(o => o.GroupRequest)
                    .ThenInclude(gr => gr.Participants)
                .Include(o => o.GroupRequest)
                    .ThenInclude(gr => gr.Category)
                .ToListAsync();

            _logger.LogInformation("Starting sequential Qdrant sync for {Count} offers...", offers.Count);

            int succeeded = 0;
            int failed = 0;

            foreach (var offer in offers)
            {
                var categoryName = offer.GroupRequest?.Category?.Name ?? "General";
                var totalParticipants = offer.GroupRequest?.Participants
                    .Count(p => p.Status == GroupRequestParticipantStatus.Active) ?? 0;

                try
                {
                    await _roundIndexer.IndexAsync(offer, categoryName, totalParticipants);
                    succeeded++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to index Offer {OfferId} to Qdrant.", offer.Id);
                    failed++;
                }

                // Add delay to prevent hitting GitHub Models API rate limits
                await Task.Delay(150);
            }

            _logger.LogInformation("Qdrant sync complete. Succeeded: {Succeeded}, Failed: {Failed}.", succeeded, failed);
        }
    }
}
