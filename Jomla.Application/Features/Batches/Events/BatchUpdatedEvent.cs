using Jomla.Application.Features.Batches.DTOs;
using MediatR;
using System;

namespace Jomla.Application.Features.Batches.Events
{
    public sealed record BatchUpdatedEvent(Guid OfferId, BatchUpdatedDto Update) : INotification;
}
