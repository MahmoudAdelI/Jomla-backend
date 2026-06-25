using Jomla.Domain.Entities;

namespace Jomla.Application.Common.Interfaces
{
    public interface INegotiationRoundIndexer
    {
        Task IndexAsync(GroupRequestOffer offer, string categoryName, int totalParticipants);
    }
}
