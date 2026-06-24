using Jomla.Application.Common.Interfaces;
using Jomla.Domain;
using Jomla.Domain.Entities;
using MediatR;
using System.Text.Json;
using Jomla.Application.Jobs.JobDispatcher;
using Jomla.Application.Jobs.Agents;

namespace Jomla.Application.Features.Offers.Commands.CreateOffer
{
    public sealed class CreateOfferCommandHandler(
        IAppDbContext db,
        IIdentityService identityService,
        IImageService imageService,
        IBackgroundJobDispatcher jobDispatcher) : IRequestHandler<CreateOfferCommand, Guid>
    {
        private readonly IAppDbContext _db = db;
        private readonly IBackgroundJobDispatcher _jobDispatcher = jobDispatcher;
        private readonly IIdentityService _identityService = identityService;
        private readonly IImageService _imageService = imageService;
        public async Task<Guid> Handle(CreateOfferCommand request, CancellationToken cancellationToken)
        {
            var supplierId = _identityService.GetCurrentUserId();

            if (supplierId == Guid.Empty)
                throw new UnauthorizedAccessException();

            var imageUrls = new List<string>();

            if (request.Images is not null)
            {
                foreach (var image in request.Images)
                {
                    var url = await _imageService.UploadImageAsync(
                        image,
                        cancellationToken);

                    imageUrls.Add(url);
                }
            }

            var offer = new SupplierOffer
            {
                SupplierId = supplierId,
                CategoryId = request.CategoryId,
                Title = request.Title,
                Description = request.Description,
                UnitPrice = request.UnitPrice,
                DiscountPercentage = request.DiscountPercentage,
                BatchTargetQuantity = request.BatchTargetQuantity,
                TotalQuantityAvailable = request.TotalQuantityAvailable,
                MinFallbackQuantity = request.MinFallbackQuantity,
                VariantAttributes = request.VariantAttributes,
                ExpiresAt = request.ExpiresAt,
                Status = SupplierOfferStatus.PendingReview,
                ModerationStatus = ModerationStatus.Pending,
                ImageUrls = imageUrls.Count > 0 ? JsonSerializer.Serialize(imageUrls) : null
            };

            _db.SupplierOffers.Add(offer);

            await _db.SaveChangesAsync(cancellationToken);

            // Trigger AI content moderation — approval will open the first batch and schedule expiry
            _jobDispatcher.Enqueue<IModerateSupplierOfferJob>(job =>
                job.ExecuteAsync(offer.Id, CancellationToken.None));
            // Trigger AI content moderation
            //jobDispatcher.Enqueue<IModerateSupplierOfferJob>(
            //    j => j.ExecuteAsync(offer.Id, CancellationToken.None));

            // Schedule expiry check if ExpiresAt is set
            //if (offer.ExpiresAt.HasValue)
            //{
            //    var jobId = jobDispatcher.Schedule<ISupplierOfferExpiryJob>(
            //        j => j.ExcuteAsync(offer.Id, CancellationToken.None),
            //        new DateTimeOffset(offer.ExpiresAt.Value, TimeSpan.Zero));

            //    offer.JobId = jobId;
            //    await _db.SaveChangesAsync(cancellationToken);
            //}

            return offer.Id;
        }
    }
}
