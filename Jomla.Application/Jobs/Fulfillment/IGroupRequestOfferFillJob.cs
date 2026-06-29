namespace Jomla.Application.Jobs.Fulfillment
{
    public interface IGroupRequestOfferFillJob
    {
        Task ExecuteAsync(Guid offerId);
    }
}
