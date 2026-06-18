namespace Jomla.Application.Jobs.Expiry
{
    public interface ISupplierOfferExpiryJob
    {
        Task ExcuteAsync(Guid offerId, CancellationToken ct);
    }
}
