using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.Batches.Commands.FailBatch;
using Jomla.Application.Features.Notifications;
using Jomla.Application.Jobs.JobDispatcher;
using Jomla.Domain;
using Jomla.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jomla.Application.Features.Offers.Commands.DeactivateOffer;

public sealed class DeactivateOfferCommandHandler(
    IAppDbContext db,
    IIdentityService identityService,
    IBackgroundJobDispatcher jobDispatcher,
    IMediator mediator,
    ILogger<DeactivateOfferCommandHandler> logger
    ) : IRequestHandler<DeactivateOfferCommand, bool>
{
    private readonly IAppDbContext _db = db;
    private readonly IIdentityService _identityService = identityService;
    private readonly IBackgroundJobDispatcher _jobDispatcher = jobDispatcher;
    private readonly IMediator _mediator = mediator;
    private readonly ILogger<DeactivateOfferCommandHandler> _logger = logger;

    public async Task<bool> Handle(DeactivateOfferCommand request, CancellationToken cancellationToken)
    {
        var supplierId = _identityService.GetCurrentUserId();

        // 1️⃣ Load the offer
        var offer = await _db.SupplierOffers
            .FirstOrDefaultAsync(o => o.Id == request.OfferId, cancellationToken);

        if (offer is null)
            throw new KeyNotFoundException($"Offer '{request.OfferId}' was not found.");

        // 2️⃣ Ownership check
        if (offer.SupplierId != supplierId)
            throw new UnauthorizedAccessException("You are not the owner of this offer.");

        // 3️⃣ Only Active offers can be deactivated
        if (offer.Status != SupplierOfferStatus.Active)
            throw new InvalidOperationException($"Only Active offers can be deactivated. Current status: {offer.Status}.");

        // 4️⃣ Cancel the scheduled expiry Hangfire job (if one exists)
        if (!string.IsNullOrWhiteSpace(offer.JobId))
        {
            try
            {
                _jobDispatcher.Delete(offer.JobId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to delete expiry job '{JobId}' for offer {OfferId}. Proceeding with deactivation.",
                    offer.JobId, offer.Id);
            }
        }

        // 5️⃣ Mark offer as Inactive
        offer.Status = SupplierOfferStatus.Inactive;
        await _db.SaveChangesAsync(cancellationToken);

        // 6️⃣ Find and fail the open batch (if any) — FailBatchCommandHandler handles
        //     Stripe hold releases, per-buyer notifications, and SignalR broadcast
        var openBatch = await _db.SupplierBatches
            .FirstOrDefaultAsync(
                b => b.OfferId == offer.Id && b.Status == BatchStatus.Open,
                cancellationToken);

        if (openBatch is not null)
        {
            try
            {
                await _mediator.Send(new FailBatchCommand(openBatch.Id), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error while failing open batch {BatchId} during offer deactivation {OfferId}.",
                    openBatch.Id, offer.Id);
                // Do not rethrow — the offer is already marked Inactive; batch failure
                // is a best-effort side effect.
            }
        }

        // 7️⃣ Notify the supplier
        var supplierNotification = new Notification
        {
            UserId = offer.SupplierId,
            Type = NotificationType.OfferDeactivated,
            Title = "Your offer has been deactivated",
            Body = openBatch is not null
                ? "Your offer has been deactivated. The open batch was canceled and all participant holds have been released."
                : "Your offer has been deactivated successfully.",
            EntityId = offer.Id,
            EntityType = nameof(SupplierOffer),
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.Notifications.Add(supplierNotification);
        await _db.SaveChangesAsync(cancellationToken);

        try
        {
            await _mediator.Publish(
                new NotificationCreatedEvent(supplierNotification.UserId, supplierNotification.Id),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to push deactivation notification to supplier {SupplierId}.", offer.SupplierId);
        }

        return true;
    }
}
