namespace Jomla.Application.Jobs.Expiry
{
    public interface ISupplierOfferExpiryJob
    {
        Task ExecuteAsync(Guid offerId, CancellationToken ct);
    }
}
