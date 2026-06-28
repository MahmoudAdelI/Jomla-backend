using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Jomla.Application.Features.GroupRequests.Commands.PlaceGroupRequestOffer
{
    public sealed class PlaceGroupRequestOfferCommandValidator: AbstractValidator<PlaceGroupRequestOfferCommand>
    {
        public PlaceGroupRequestOfferCommandValidator()
        {
            RuleFor(x => x.GroupRequestId)
                 .NotEmpty()
                 .WithMessage("Group request is required.");

            RuleFor(x => x.UnitPrice)
                .GreaterThan(0)
                .WithMessage("Unit price must be greater than zero.");

            RuleFor(x => x.QuantityAvailable)
                .GreaterThan(0)
                .WithMessage("Quantity must be greater than zero.");

            RuleFor(x => x.ExpiresAt)
                 .Must(x => x > DateTime.UtcNow)
                   .WithMessage("Expiry date must be in the future.");

            RuleFor(x => x.MinUnitPrice)
                .GreaterThan(0)
                .When(x => x.MinUnitPrice.HasValue)
                .WithMessage("Minimum unit price must be greater than zero.");

            RuleFor(x => x)
                .Must(x =>
                    !x.MinUnitPrice.HasValue ||
                    x.MinUnitPrice.Value <= x.UnitPrice)
                .WithMessage("Minimum unit price cannot be greater than unit price.");

            RuleFor(x => x.MinFallbackQuantity)
                .GreaterThan(0)
                .When(x => x.MinFallbackQuantity.HasValue)
                .WithMessage("Minimum fallback quantity must be greater than zero.");

            RuleFor(x => x)
                .Must(x =>
                    !x.MinFallbackQuantity.HasValue ||
                    x.MinFallbackQuantity.Value <= x.QuantityAvailable)
                .WithMessage("Minimum fallback quantity cannot exceed available quantity.");

            RuleFor(x => x.VariantAttributes)
                .Must(BeValidJsonObject)
                .When(x => !string.IsNullOrWhiteSpace(x.VariantAttributes))
                .WithMessage("Variant attributes must be a valid JSON object.");
        }

        private static bool BeValidJsonObject(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return true;

            try
            {
                using var document = JsonDocument.Parse(json);

                return document.RootElement.ValueKind == JsonValueKind.Object;
            }
            catch
            {
                return false;
            }
        }
    }
}
