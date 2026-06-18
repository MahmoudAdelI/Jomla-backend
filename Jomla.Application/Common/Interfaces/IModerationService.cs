namespace Jomla.Application.Common.Interfaces
{
    public interface IModerationService
    {
        Task<ModerationResult> ModerateAsync(ModerationInput input, CancellationToken ct = default);
    }

    public sealed record ModerationInput(string Title, string? Description, IReadOnlyList<string> ImageUrls);

    public sealed record ModerationResult(bool IsApproved, string? Reason);
}
