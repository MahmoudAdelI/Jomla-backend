using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Jomla.Application.Features.Offers.Commands.UpdateOffer;

public sealed record UpdateOfferCommand : IRequest<bool>
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string? Description { get; init; }

    public Guid CategoryId { get; init; }

    public decimal UnitPrice { get; init; }

    public decimal DiscountPercentage { get; init; }

    public int BatchTargetQuantity { get; init; }

    public int TotalQuantityAvailable { get; init; }

    public int? MinFallbackQuantity { get; init; }

    public DateTime? ExpiresAt { get; init; }

    public List<IFormFile>? Images { get; init; }
}