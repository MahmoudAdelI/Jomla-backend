using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Application.Features.Admin.Commands.ApproveOffer
{
    public sealed record ApproveOfferCommand(Guid OfferId) : IRequest;
}
