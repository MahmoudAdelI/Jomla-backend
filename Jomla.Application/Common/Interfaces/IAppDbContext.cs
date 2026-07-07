using Jomla.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Jomla.Application.Common.Interfaces
{
    public interface IAppDbContext
    {
        DbSet<Category> Categories { get; }
        DbSet<SupplierCategoryPreference> SupplierCategoryPreferences { get; }
        DbSet<SupplierOffer> SupplierOffers { get; }
        DbSet<SupplierBatch> SupplierBatches { get; }
        DbSet<BatchParticipant> BatchParticipants { get; }
        DbSet<GroupRequest> GroupRequests { get; }
        DbSet<GroupRequestParticipant> GroupRequestParticipants { get; }
        DbSet<GroupRequestAlert> GroupRequestAlerts { get; }
        DbSet<GroupRequestOffer> GroupRequestOffers { get; }
        DbSet<BuyerOfferResponse> BuyerOfferResponses { get; }
        DbSet<NegotiationLog> NegotiationLogs { get; }
        DbSet<Notification> Notifications { get; }
        DbSet<Order> Orders { get; }
        DbSet<UserContactInfo> UserContactInfos { get; }
        DatabaseFacade Database { get; }
        Task<int> SaveChangesAsync(CancellationToken ct = default);
        Task<GroupRequest?> GetGroupRequestWithLockAsync(Guid id, CancellationToken ct);
    }
}
