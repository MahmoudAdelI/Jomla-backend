using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Application.Features.GroupRequests.Commands.JoinGroupRequest
{
    public sealed record JoinGroupRequestCommand(
      Guid GroupRequestId,
      Guid BuyerId,
      int Quantity
  ) : IRequest<JoinGroupRequestResponse>;

    public sealed record JoinGroupRequestResponse(
        bool Success,
        string? Error
    );
}
