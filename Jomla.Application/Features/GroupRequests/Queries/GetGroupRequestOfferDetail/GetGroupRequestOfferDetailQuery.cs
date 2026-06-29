using Jomla.Application.Features.GroupRequests.Dtos;
using MediatR;
using System;

namespace Jomla.Application.Features.GroupRequests.Queries.GetGroupRequestOfferDetail;

public sealed record GetGroupRequestOfferDetailQuery(Guid OfferId) : IRequest<SellerGroupRequestOfferDetailDto>;
