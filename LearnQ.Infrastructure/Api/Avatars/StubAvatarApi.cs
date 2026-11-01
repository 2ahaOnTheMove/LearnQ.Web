using LearnQ.Application.Common.Interfaces;
using LearnQ.Application.Common.Models;

namespace LearnQ.Infrastructure.Api.Avatars;

internal sealed class StubAvatarApi : IAvatarApi
{
    public async Task<Result<string>> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken ct = default)
    {
        await Task.Delay(200, ct);
        var fakeUrl = $"/avatars/stub-{Guid.NewGuid():N}.png";
        return Result<string>.Success(fakeUrl);
    }
}
