using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using Jomla.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Jomla.Domain;
using Jomla.Application.Jobs.JobDispatcher;
using Jomla.Application.Jobs.Agents;

namespace Jomla.Application.Features.Offers.Commands.UpdateOffer;

public sealed class UpdateOfferCommandHandler
    : IRequestHandler<UpdateOfferCommand, bool>
{
    private readonly IAppDbContext _db;
    private readonly IIdentityService _identityService;
    private readonly IImageService _imageService;
    private readonly IBackgroundJobDispatcher _jobDispatcher;

    public UpdateOfferCommandHandler(
        IAppDbContext db,
        IIdentityService identityService,
        IImageService imageService,
        IBackgroundJobDispatcher jobDispatcher)
    {
        _db = db;
        _identityService = identityService;
        _imageService = imageService;
        _jobDispatcher = jobDispatcher;
    }

    public async Task<bool> Handle(
        UpdateOfferCommand request,
        CancellationToken cancellationToken)
    {
        var supplierId = _identityService.GetCurrentUserId();

        var offer = await _db.SupplierOffers
            .FirstOrDefaultAsync(
                x => x.Id == request.Id,
                cancellationToken);

        if (offer is null)
            throw new KeyNotFoundException("Offer not found.");

        if (offer.SupplierId != supplierId)
            throw new UnauthorizedAccessException();

        bool triggerModeration = false;

        if (offer.Status != SupplierOfferStatus.PendingReview)
        {
            if (offer.CategoryId != request.CategoryId ||
                offer.UnitPrice != request.UnitPrice ||
                offer.DiscountPercentage != request.DiscountPercentage ||
                offer.BatchTargetQuantity != request.BatchTargetQuantity ||
                offer.TotalQuantityAvailable != request.TotalQuantityAvailable ||
                offer.MinFallbackQuantity != request.MinFallbackQuantity ||
                offer.ExpiresAt != request.ExpiresAt)
            {
                throw new InvalidOperationException("Cannot modify pricing, category, target quantities, or expiry dates after an offer has gone live.");
            }

            bool isContentChanged = offer.Title != request.Title || 
                                    offer.Description != request.Description || 
                                    (request.Images != null && request.Images.Count > 0);

            if (isContentChanged)
            {
                offer.Status = SupplierOfferStatus.PendingReview;
                offer.ModerationStatus = ModerationStatus.Pending;
                offer.ModerationReason = null;
                triggerModeration = true;
            }
        }
        else
        {
            triggerModeration = true;
        }

        offer.Title = request.Title;
        offer.Description = request.Description;
        offer.CategoryId = request.CategoryId;
        offer.UnitPrice = request.UnitPrice;
        offer.DiscountPercentage = request.DiscountPercentage;
        offer.BatchTargetQuantity = request.BatchTargetQuantity;
        offer.TotalQuantityAvailable = request.TotalQuantityAvailable;
        offer.MinFallbackQuantity = request.MinFallbackQuantity;
        offer.ExpiresAt = request.ExpiresAt;

        if (request.Images is not null && request.Images.Count > 0)
        {
            var imageUrls = new List<string>();

            foreach (var image in request.Images)
            {
                var url = await _imageService.UploadImageAsync(
                    image,
                    cancellationToken);

                imageUrls.Add(url);
            }

            offer.ImageUrls =
                JsonSerializer.Serialize(imageUrls);
        }

        await _db.SaveChangesAsync(cancellationToken);

        if (triggerModeration)
        {
            _jobDispatcher.Enqueue<IModerateSupplierOfferJob>(job =>
                job.ExecuteAsync(offer.Id, CancellationToken.None));
        }

        return true;
    }
}