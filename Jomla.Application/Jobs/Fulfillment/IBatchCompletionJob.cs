namespace Jomla.Application.Jobs.Fulfillment
{
    public interface IBatchCompletionJob
    {
        // Write the signature later
        Task ExecuteAsync(Guid batchId);
    }
}
