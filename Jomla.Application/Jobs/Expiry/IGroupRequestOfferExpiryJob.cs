namespace Jomla.Application.Jobs.Expiry
{
    public interface IGroupRequestOfferExpiryJob
    {
        Task ExcuteAsync(Guid offerId);
    }
}
