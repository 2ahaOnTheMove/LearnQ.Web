using System.Text.Json.Serialization;

namespace LearnQ.Application.Features.Auth;

/// <summary>
/// Response from POST /api/clients/register.
/// The backend returns 201 Created with user info but NO tokens.
/// The user must log in to obtain access/refresh tokens.
/// See: docs/auth-api-contract.md section 3.1
/// </summary>
public sealed record RegisterResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("displayName")] string DisplayName,
    [property: JsonPropertyName("status")] string? Status);
