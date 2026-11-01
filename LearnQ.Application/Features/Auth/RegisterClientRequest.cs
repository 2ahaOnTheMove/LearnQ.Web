namespace LearnQ.Application.Features.Auth;

public sealed record RegisterClientRequest(
    string GivenName,
    string FamilyName,
    string DisplayName,
    string Email,
    string Password,
    bool TermsAccepted,
    bool PrivacyAccepted);
    