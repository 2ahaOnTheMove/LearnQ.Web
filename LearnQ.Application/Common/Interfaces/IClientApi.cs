using LearnQ.Application.Common.Models;
using LearnQ.Application.Features.Auth;

namespace LearnQ.Application.Common.Interfaces;

public interface IClientApi
{
    Task<Result> RegisterAsync(RegisterClientRequest request, CancellationToken ct = default);
}