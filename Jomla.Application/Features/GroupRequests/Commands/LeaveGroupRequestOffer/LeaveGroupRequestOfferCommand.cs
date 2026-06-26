using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Application.Features.GroupRequests.Commands.LeaveGroupRequestOffer
{
    public record LeaveGroupRequestOfferCommand(Guid OfferId, Guid BuyerId) : IRequest;
}
