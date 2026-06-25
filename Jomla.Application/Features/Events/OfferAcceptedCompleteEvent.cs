using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Application.Features.Events
{
    public sealed record OfferAcceptedCompleteEvent(Guid OfferId) : INotification;
}
