namespace Jomla.Application.Jobs.Agents
{
    public interface IModerateSupplierOfferJob
    {
        Task ExecuteAsync(Guid offerId, CancellationToken ct);
    }

    public interface IModerateGroupRequestJob
    {
        Task ExecuteAsync(Guid groupRequestId, CancellationToken ct);
    }
}
