namespace Jomla.Application.Jobs.Closing
{
    public interface IGroupRequestAutoCloseJob
    {
        Task ExecuteAsync(Guid groupRequestId);
    }
}
