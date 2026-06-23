namespace Jomla.Application.Jobs.Matching
{
    public interface ISupplierMatchingJob
    {
        Task ExcuteAsync(Guid groupRequestId, Guid categoryId, int currentQuantity);
    }
}
