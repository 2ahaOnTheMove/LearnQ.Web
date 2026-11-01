using LearnQ.Application.Common.Interfaces;
using LearnQ.Infrastructure.Api.Auth;
using LearnQ.Infrastructure.Api.Avatars;
using LearnQ.Infrastructure.Api.Clients;
using LearnQ.Infrastructure.Api.Http;
using LearnQ.Infrastructure.Api.Languages;
using LearnQ.Infrastructure.Api.Onboarding;
using LearnQ.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LearnQ.Infrastructure.DependencyInjection;

public static class ApiClientsExtensions
{
    public static IServiceCollection AddApiClients(this IServiceCollection services, IConfiguration config)
    {
        var apiOptions = config.GetRequiredSection(ApiOptions.SectionName).Get<ApiOptions>()
                      ?? throw new InvalidOperationException($"{ApiOptions.SectionName}:{nameof(ApiOptions.BaseUrl)} is not configured.");

        services.AddSingleton(apiOptions);

        services.AddSingleton<ILanguageCatalog, LanguageCatalog>();

        // Anonymous client for Register (no bearer token handler)
        services.AddHttpClient<IClientApi, ClientApi>(client =>
        {
            client.BaseAddress = new Uri(apiOptions.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        // Anonymous client for Auth (login, refresh, logout — must NOT use bearer handler to avoid recursion)
        services.AddHttpClient<IAuthApi, AuthApi>(client =>
        {
            client.BaseAddress = new Uri(apiOptions.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        // Authenticated client for Onboarding (uses BearerTokenHandler)
        var onboardingBuilder = services.AddHttpClient<IOnboardingApi, OnboardingApi>(client =>
        {
            client.BaseAddress = new Uri(apiOptions.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(15);
        });
        onboardingBuilder.AddHttpMessageHandler<BearerTokenHandler>();

        // Authenticated client for Avatars (uses BearerTokenHandler)
        var avatarBuilder = services.AddHttpClient<IAvatarApi, AvatarApi>(client =>
        {
            client.BaseAddress = new Uri(apiOptions.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        avatarBuilder.AddHttpMessageHandler<BearerTokenHandler>();

        return services;
    }
}
