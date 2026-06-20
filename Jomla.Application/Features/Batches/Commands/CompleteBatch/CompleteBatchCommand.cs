using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Application.Features.Batches.Commands.CompleteBatch
{
    public record CompleteBatchCommand(Guid BatchId) : IRequest;
}
