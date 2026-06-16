using Jomla.Application.Common.Interfaces;
using Jomla.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Jomla.Infrastructure.Persistance
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) 
        : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options), IAppDbContext
    {
        public DbSet<Category> Categories => Set<Category>();

        public DbSet<SupplierCategoryPreference> SupplierCategoryPreferences => Set<SupplierCategoryPreference>();

        public DbSet<SupplierOffer> SupplierOffers => Set<SupplierOffer>();

        public DbSet<SupplierBatch> SupplierBatches => Set<SupplierBatch>();

        public DbSet<BatchParticipant> BatchParticipants => Set<BatchParticipant>();

        public DbSet<GroupRequest> GroupRequests => Set<GroupRequest>();

        public DbSet<GroupRequestParticipant> GroupRequestParticipants => Set<GroupRequestParticipant>();

        public DbSet<GroupRequestAlert> GroupRequestAlerts => Set<GroupRequestAlert>();

        public DbSet<GroupRequestOffer> GroupRequestOffers => Set<GroupRequestOffer>();

        public DbSet<BuyerOfferResponse> BuyerOfferResponses => Set<BuyerOfferResponse>();

        public DbSet<NegotiationLog> NegotiationLogs => Set<NegotiationLog>();

        public DbSet<Notification> Notifications => Set<Notification>();

        public DbSet<Order> Orders => Set<Order>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            foreach (var entity in builder.Model.GetEntityTypes())
            {
                var idProperty = entity.FindProperty("Id");
                if (idProperty != null && idProperty.ClrType == typeof(Guid))
                {
                    idProperty.SetDefaultValueSql("newsequentialid()");
                }
            }

            builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}
