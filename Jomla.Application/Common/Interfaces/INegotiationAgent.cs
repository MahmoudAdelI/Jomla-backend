using Jomla.Domain.Entities;

namespace Jomla.Application.Common.Interfaces
{
    public interface INegotiationAgent
    {
        Task<decimal> GetNextPriceAsync(GroupRequestOffer offer, string categoryName);
    }
}
