using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.Notifications;
using Jomla.Domain;
using Jomla.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jomla.Application.Features.GroupRequests.Commands.MatchSuppliersForGroupRequest
{
    public class MatchSuppliersForGroupRequestCommandHandler(
        IAppDbContext db,
        IMediator mediator
        ) : IRequestHandler<MatchSuppliersForGroupRequestCommand>
    {
        private readonly IAppDbContext _db = db;
        private readonly IMediator _mediator = mediator;

        public async Task Handle(MatchSuppliersForGroupRequestCommand request, CancellationToken cancellationToken)
        {
            // 1. Find already-alerted supplier IDs for this group request to avoid duplicates
            var alreadyAlertedSupplierIds = await _db.GroupRequestAlerts
                .Where(a => a.GroupRequestId == request.GroupRequestId)
                .Select(a => a.SupplierId)
                .ToHashSetAsync(cancellationToken);

            // Find all matching category IDs (the request category itself and all of its ancestor categories)
            var matchedCategoryIds = new List<Guid> { request.CategoryId };
            var visited = new HashSet<Guid> { request.CategoryId };
            
            var currentCategory = await _db.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken);
            
            while (currentCategory?.ParentId != null && !visited.Contains(currentCategory.ParentId.Value))
            {
                var parentId = currentCategory.ParentId.Value;
                matchedCategoryIds.Add(parentId);
                visited.Add(parentId);
                
                currentCategory = await _db.Categories
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == parentId, cancellationToken);
            }

            // 2. Find suppliers whose category preference matches and min quantity threshold is satisfied
            var matchedSuppliers = await _db.SupplierCategoryPreferences
                .Where(p =>
                    matchedCategoryIds.Contains(p.CategoryId) &&
                    p.MinQuantity <= request.CurrentQuantity &&
                    !alreadyAlertedSupplierIds.Contains(p.SupplierId))
                .Select(p => p.SupplierId)
                .ToListAsync(cancellationToken);

            if (matchedSuppliers.Count == 0)
                return;

            // 3. Insert a GroupRequestAlert row for each newly matched supplier
            var alerts = matchedSuppliers.Select(supplierId => new GroupRequestAlert
            {
                SupplierId = supplierId,
                GroupRequestId = request.GroupRequestId,
                Status = GroupRequestAlertStatus.Pending,
            });

            // 4. Insert a Notification row for each matched supplier
            var notifications = matchedSuppliers.Select(supplierId => new Notification
            {
                UserId = supplierId,
                Type = NotificationType.GroupRequestMatched,
                Title = "New group request matches your preferences",
                Body = "A group request in one of your preferred categories has reached your minimum quantity threshold.",
                EntityId = request.GroupRequestId,
                EntityType = nameof(GroupRequest),
                IsRead = false,
            });

            _db.GroupRequestAlerts.AddRange(alerts);
            _db.Notifications.AddRange(notifications);

            await _db.SaveChangesAsync(cancellationToken);

            // 5. Push real-time notification to each matched supplier via SignalR
            foreach (var notification in notifications)
            {
                await _mediator.Publish(new NotificationCreatedEvent(notification.UserId, notification.Id), cancellationToken);
            }
        }
    }
}
