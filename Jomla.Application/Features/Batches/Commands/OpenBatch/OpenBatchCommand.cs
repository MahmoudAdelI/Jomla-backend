using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Application.Features.Batches.Commands.OpenBatch
{
    public record OpenBatchCommand(Guid OfferId) : IRequest;
}
