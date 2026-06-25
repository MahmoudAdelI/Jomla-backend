namespace Jomla.Application.Jobs.Matching
{
    public interface ISupplierMatchingJob
    {
        Task ExecuteAsync(Guid groupRequestId, Guid categoryId, int currentQuantity);
    }
}
