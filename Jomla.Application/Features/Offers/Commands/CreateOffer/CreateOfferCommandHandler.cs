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

namespace Jomla.Application.Features.Offers.Commands.CreateOffer
{
    public sealed class CreateOfferCommandHandler(IAppDbContext db, IIdentityService identityService, IImageService imageService) : IRequestHandler<CreateOfferCommand, Guid>
    {
        public async Task<Guid> Handle(CreateOfferCommand request,CancellationToken cancellationToken)
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

            return offer.Id;
        }
    }
}
