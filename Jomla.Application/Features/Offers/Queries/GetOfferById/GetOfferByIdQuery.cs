using Jomla.Application.Features.Offers.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Application.Features.Offers.Queries.GetOfferById;

public sealed record GetOfferByIdQuery(Guid Id): IRequest<OfferDto>;