using Jomla.Application.Common.Interfaces;
using Jomla.Domain.Entities;
using Jomla.Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Jomla.Application.Tests.Infrastructure;

public class TestAppDbContext : IAppDbContext
{
    private readonly AppDbContext _context;

    public TestAppDbContext(AppDbContext context)
    {
        _context = context;
    }

    public DbSet<Category> Categories => _context.Categories;
    public DbSet<SupplierCategoryPreference> SupplierCategoryPreferences => _context.SupplierCategoryPreferences;
    public DbSet<SupplierOffer> SupplierOffers => _context.SupplierOffers;
    public DbSet<SupplierBatch> SupplierBatches => _context.SupplierBatches;
    public DbSet<BatchParticipant> BatchParticipants => _context.BatchParticipants;
    public DbSet<GroupRequest> GroupRequests => _context.GroupRequests;
    public DbSet<GroupRequestParticipant> GroupRequestParticipants => _context.GroupRequestParticipants;
    public DbSet<GroupRequestAlert> GroupRequestAlerts => _context.GroupRequestAlerts;
    public DbSet<GroupRequestOffer> GroupRequestOffers => _context.GroupRequestOffers;
    public DbSet<BuyerOfferResponse> BuyerOfferResponses => _context.BuyerOfferResponses;
    public DbSet<NegotiationLog> NegotiationLogs => _context.NegotiationLogs;
    public DbSet<Notification> Notifications => _context.Notifications;
    public DbSet<Order> Orders => _context.Orders;

    public DatabaseFacade Database => _context.Database;

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var entries = _context.ChangeTracker.Entries();
        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
            {
                var rowVersionProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "RowVersion");
                if (rowVersionProp != null && (rowVersionProp.CurrentValue == null || ((byte[])rowVersionProp.CurrentValue).Length == 0))
                {
                    rowVersionProp.CurrentValue = Guid.NewGuid().ToByteArray();
                }
            }
        }
        return _context.SaveChangesAsync(ct);
    }

    public Task<GroupRequest?> GetGroupRequestWithLockAsync(Guid id, CancellationToken ct)
    {
        return _context.GroupRequests.FirstOrDefaultAsync(r => r.Id == id, ct);
    }
}
