using Jomla.Application.Common.Interfaces;
using Jomla.Domain;
using Jomla.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using Jomla.Application.Jobs.JobDispatcher;
using Jomla.Application.Jobs.Agents;
using Jomla.Application.Jobs.Expiry;

namespace Jomla.Application.Features.Offers.Commands.CreateOffer
{
    public sealed class CreateOfferCommandHandler(
        IAppDbContext db,
        IIdentityService identityService,
        IImageService imageService,
        IBackgroundJobDispatcher jobDispatcher) : IRequestHandler<CreateOfferCommand, Guid>
    {
        public async Task<Guid> Handle(CreateOfferCommand request, CancellationToken cancellationToken)
        {
            var supplierId = identityService.GetCurrentUserId();

            if (supplierId == Guid.Empty)
                throw new UnauthorizedAccessException();

            var imageUrls = new List<string>();

            if (request.Images is not null)
            {
                foreach (var image in request.Images)
                {
                    var url = await imageService.UploadImageAsync(
                        image,
                        cancellationToken);

                    imageUrls.Add(url);
                }
            }

            var offer = new SupplierOffer
            {
                Id = Guid.NewGuid(),

                SupplierId = supplierId,

                CategoryId = request.CategoryId,

                Title = request.Title,

                Description = request.Description,

                UnitPrice = request.UnitPrice,

                DiscountPercentage = request.DiscountPercentage,

                BatchTargetQuantity = request.BatchTargetQuantity,

                TotalQuantityAvailable = request.TotalQuantityAvailable,

                MinFallbackQuantity = request.MinFallbackQuantity,

                ExpiresAt = request.ExpiresAt,

                Status = SupplierOfferStatus.Active,

                ModerationStatus = ModerationStatus.Pending,

                CreatedAt = DateTime.UtcNow,

                ImageUrls = JsonSerializer.Serialize(imageUrls)
            };

            db.SupplierOffers.Add(offer);

            await db.SaveChangesAsync(cancellationToken);

            // Trigger AI content moderation
            jobDispatcher.Enqueue<IModerateSupplierOfferJob>(
                j => j.ExecuteAsync(offer.Id, CancellationToken.None));

            // Schedule expiry check if ExpiresAt is set
            if (offer.ExpiresAt.HasValue)
            {
                var jobId = jobDispatcher.Schedule<ISupplierOfferExpiryJob>(
                    j => j.ExcuteAsync(offer.Id, CancellationToken.None),
                    new DateTimeOffset(offer.ExpiresAt.Value, TimeSpan.Zero));

                offer.JobId = jobId;
                await db.SaveChangesAsync(cancellationToken);
            }

            return offer.Id;
        }
    }
}
