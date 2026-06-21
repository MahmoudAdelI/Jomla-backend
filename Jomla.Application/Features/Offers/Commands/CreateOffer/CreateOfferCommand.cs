using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Jomla.Application.Features.Offers.Commands.CreateOffer;

public sealed record CreateOfferCommand(
    string Title,
    string? Description,
    Guid CategoryId,
    decimal UnitPrice,
    decimal DiscountPercentage,
    int BatchTargetQuantity,
    int TotalQuantityAvailable,
    int? MinFallbackQuantity,
    DateTime? ExpiresAt,
    List<IFormFile>? Images
) : IRequest<Guid>;
