using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace Jomla.Application.Features.Offers.Commands.DeleteOffer;

public sealed record DeleteOfferCommand(Guid Id)
    : IRequest<bool>;