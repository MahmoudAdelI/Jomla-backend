using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Application.Features.Admin.Commands.RejectOffer
{
    public sealed record RejectOfferCommand(Guid OfferId, string Reason) : IRequest;
}
