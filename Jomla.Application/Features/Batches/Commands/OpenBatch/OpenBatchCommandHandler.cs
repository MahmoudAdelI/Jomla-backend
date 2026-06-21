using Jomla.Application.Features.Batches.Commands.CreateBatch;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Application.Features.Batches.Commands.OpenBatch
{
    public class OpenBatchCommandHandler : IRequestHandler<OpenBatchCommand>
    {
        private readonly IMediator _mediator;

        public OpenBatchCommandHandler(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task Handle(OpenBatchCommand request, CancellationToken cancellationToken)
        {
            await _mediator.Send(new CreateBatchCommand(request.OfferId), cancellationToken);
        }
    }
}
