using System.Text.Json;
using Jomla.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Jomla.Domain;
using Jomla.Application.Jobs.JobDispatcher;
using Jomla.Application.Jobs.Agents;
using Jomla.Application.Jobs.Expiry;
using Jomla.Application.Common.Exceptions;
using Jomla.Domain.Entities;
using Jomla.Application.Features.Batches.Commands.CreateBatch;

namespace Jomla.Application.Features.Offers.Commands.UpdateOffer;

public sealed class UpdateOfferCommandHandler(
    IAppDbContext db,
    IIdentityService identityService,
    IImageService imageService,
    IBackgroundJobDispatcher jobDispatcher,
    IMediator mediator)
        : IRequestHandler<UpdateOfferCommand, bool>
{
    private readonly IAppDbContext _db = db;
    private readonly IIdentityService _identityService = identityService;
    private readonly IImageService _imageService = imageService;
    private readonly IBackgroundJobDispatcher _jobDispatcher = jobDispatcher;
    private readonly IMediator _mediator = mediator;

    public async Task<bool> Handle(UpdateOfferCommand request, CancellationToken cancellationToken)
    {
        var supplierId = _identityService.GetCurrentUserId();

        if (supplierId == Guid.Empty)
            throw new UnauthorizedAccessException();

        var offer = await _db.SupplierOffers
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
                ?? throw new NotFoundException(nameof(SupplierOffer), request.Id);

        if (offer.SupplierId != supplierId)
            throw new UnauthorizedAccessException();

        var openBatch = await _db.SupplierBatches
            .FirstOrDefaultAsync(b =>
                b.OfferId == request.Id &&
                b.Status == BatchStatus.Open, cancellationToken);

        // Compute once — reused in both the open-batch guard below and the moderation branch.
        bool isContentChanged = offer.Title != request.Title ||
                                offer.Description != request.Description ||
                                (request.Images != null && request.Images.Count > 0);

        if (openBatch != null)
        {
            if (offer.UnitPrice != request.UnitPrice ||
                offer.DiscountPercentage != request.DiscountPercentage ||
                offer.BatchTargetQuantity != request.BatchTargetQuantity ||
                offer.CategoryId != request.CategoryId ||
                offer.VariantAttributes != request.VariantAttributes)
            {
                throw new ConflictException("Cannot modify pricing, category, variants, or target quantities while there is an open batch.");
            }

            if (isContentChanged)
            {
                throw new ConflictException("Cannot modify the title, description, or images of an offer while there is an open batch.");
            }

            if (request.TotalQuantityAvailable < openBatch.CurrentQuantity)
            {
                throw new ConflictException($"Total quantity available cannot be less than the current batch's committed quantity ({openBatch.CurrentQuantity}).");
            }
        }

        // Apply all field updates.
        // NOTE: isContentChanged was computed against the original offer values before this point,
        // so the evaluation above is correct. From here on, the offer is mutated with the new values.
        offer.Title = request.Title;
        offer.Description = request.Description;
        offer.CategoryId = request.CategoryId;
        offer.UnitPrice = request.UnitPrice;
        offer.DiscountPercentage = request.DiscountPercentage;
        offer.BatchTargetQuantity = request.BatchTargetQuantity;
        offer.TotalQuantityAvailable = request.TotalQuantityAvailable;
        offer.MinFallbackQuantity = request.MinFallbackQuantity;
        offer.VariantAttributes = request.VariantAttributes;
        offer.ExpiresAt = request.ExpiresAt;

        if (isContentChanged)
        {
            // Upload images only when we know they'll actually be saved.
            if (request.Images is not null && request.Images.Count > 0)
            {
                var imageUrls = new List<string>();

                foreach (var image in request.Images)
                {
                    var url = await _imageService.UploadImageAsync(image, cancellationToken);
                    imageUrls.Add(url);
                }

                offer.ImageUrls = JsonSerializer.Serialize(imageUrls);
            }

            offer.Status = SupplierOfferStatus.PendingReview;
            offer.ModerationStatus = ModerationStatus.Pending;
            offer.ModerationReason = null;

            // Cancel the existing expiry job — moderation will reschedule it upon approval.
            if (!string.IsNullOrWhiteSpace(offer.JobId))
            {
                _jobDispatcher.Delete(offer.JobId);
                offer.JobId = string.Empty;
            }

            await _db.SaveChangesAsync(cancellationToken);

            _jobDispatcher.Enqueue<IModerateSupplierOfferJob>(job =>
                job.ExecuteAsync(offer.Id, CancellationToken.None));
        }
        else
        {
            // No content change — check whether the offer qualifies for reactivation.
            // Reactivation uses the *new* TotalQuantityAvailable/MinFallbackQuantity values
            // (already assigned above) so the supplier can refill stock in the same request.
            bool wasJustReactivated = false;

            if (offer.Status == SupplierOfferStatus.Inactive && CanReactivate(offer))
            {
                wasJustReactivated = true;
                offer.Status = SupplierOfferStatus.Active;
            }

            if (offer.Status == SupplierOfferStatus.Active)
            {
                // Always reschedule the expiry job for active offers to pick up any ExpiresAt change.
                if (!string.IsNullOrWhiteSpace(offer.JobId))
                {
                    _jobDispatcher.Delete(offer.JobId);
                    offer.JobId = string.Empty;
                }

                if (offer.ExpiresAt.HasValue)
                {
                    offer.JobId = _jobDispatcher.Schedule<ISupplierOfferExpiryJob>(job =>
                        job.ExecuteAsync(offer.Id, CancellationToken.None),
                        new DateTimeOffset(offer.ExpiresAt.Value, TimeSpan.Zero));
                }
            }
            // NOTE: If the offer is Inactive and did NOT qualify for reactivation (e.g. stock is still
            // too low), or if it is in any other non-Active status (e.g. PendingReview, Flagged),
            // we still save the updated fields (e.g. new ExpiresAt) but do not schedule an expiry job.

            await _db.SaveChangesAsync(cancellationToken);

            if (wasJustReactivated)
            {
                await _mediator.Send(new CreateBatchCommand(offer.Id), cancellationToken);
            }
        }

        return true;
    }

    /// <summary>
    /// Returns true if an Inactive offer has enough stock and a valid expiry window to be reactivated.
    /// </summary>
    private static bool CanReactivate(SupplierOffer offer)
    {
        var minimumToOpen = offer.MinFallbackQuantity ?? offer.BatchTargetQuantity;

        return offer.TotalQuantityAvailable >= minimumToOpen &&
               (!offer.ExpiresAt.HasValue || offer.ExpiresAt.Value > DateTime.UtcNow);
    }
}
