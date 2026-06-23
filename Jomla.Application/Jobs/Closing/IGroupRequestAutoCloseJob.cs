namespace Jomla.Application.Jobs.Closing
{
    public interface IGroupRequestAutoCloseJob
    {
        Task ExcuteAsync(Guid groupRequestId);
    }
}
