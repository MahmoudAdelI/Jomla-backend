using System;
using System.Threading.Tasks;

namespace Jomla.Application.Jobs.Sync
{
    public interface INegotiationRoundIndexJob
    {
        Task ExcuteAsync(Guid offerId);
    }
}
