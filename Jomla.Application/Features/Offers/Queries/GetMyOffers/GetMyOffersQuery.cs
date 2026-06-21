using Jomla.Application.Features.Offers.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Application.Features.Offers.Queries.GetMyOffers;

public sealed record GetMyOffersQuery(): IRequest<List<MyOfferDto>>;