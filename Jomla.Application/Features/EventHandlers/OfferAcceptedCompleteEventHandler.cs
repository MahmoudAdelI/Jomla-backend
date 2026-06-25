using Jomla.Application.Features.Events;
using Jomla.Application.Features.GroupRequests.Commands.AcceptGroupRequestOffer;
using Jomla.Application.Features.GroupRequests.Commands.CompleteGroupRequestOffer;
using MediatR;

namespace Jomla.Application.Features.GroupRequests.EventHandlers
{

    public class OfferAcceptedCompleteEventHandler : INotificationHandler<OfferAcceptedCompleteEvent>
    {
        private readonly IMediator _mediator;

        public OfferAcceptedCompleteEventHandler(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task Handle(OfferAcceptedCompleteEvent notification, CancellationToken cancellationToken)
        {
            // أول ما الجرس يضرب.. بنادي على ال Complete Command عشان يسحب الفلوس
            await _mediator.Send(new CompleteGroupRequestOfferCommand(notification.OfferId), cancellationToken);
        }
    }
}