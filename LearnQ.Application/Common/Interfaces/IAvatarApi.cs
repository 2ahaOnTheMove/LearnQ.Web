using LearnQ.Application.Common.Models;

namespace LearnQ.Application.Common.Interfaces;

public interface IAvatarApi
{
    Task<Result<string>> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default);
}
