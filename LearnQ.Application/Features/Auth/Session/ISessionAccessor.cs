namespace LearnQ.Application.Features.Auth.Session;

public interface ISessionAccessor
{
    Task<string?> GetSessionIdAsync(CancellationToken cancellationToken = default);
}
