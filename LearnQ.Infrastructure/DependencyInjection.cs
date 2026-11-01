using LearnQ.Application.Common.Interfaces;
using LearnQ.Infrastructure.Api.Auth;
using LearnQ.Infrastructure.Api.Avatars;
using LearnQ.Infrastructure.Api.Clients;
using LearnQ.Infrastructure.Api.Languages;
using LearnQ.Infrastructure.Api.Onboarding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LearnQ.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration config,
        Action<IHttpClientBuilder>? configureAuthenticatedClient = null)
    {
        var baseUrl = config["Api:BaseUrl"]
                      ?? throw new InvalidOperationException("Api:BaseUrl is not configured.");

        services.AddSingleton<ILanguageCatalog, LanguageCatalog>();

        // Anonymous client for Register (no bearer token handler)
        services.AddHttpClient<IClientApi, ClientApi>(client =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        var useAuthStub = config.GetValue<bool>("Auth:UseStub");
        if (useAuthStub)
        {
            services.AddSingleton<IAuthApi, StubAuthApi>();
        }
        else
        {
            var authBuilder = services.AddHttpClient<IAuthApi, AuthApi>(client =>
            {
                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(15);
            });
            configureAuthenticatedClient?.Invoke(authBuilder);
        }

        var useOnboardingStub = config.GetValue<bool>("Onboarding:UseStub");
        if (useOnboardingStub)
        {
            services.AddSingleton<IOnboardingApi, StubOnboardingApi>();
            services.AddSingleton<IAvatarApi, StubAvatarApi>();
        }
        else
        {
            var onboardingBuilder = services.AddHttpClient<IOnboardingApi, OnboardingApi>(client =>
            {
                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(15);
            });
            configureAuthenticatedClient?.Invoke(onboardingBuilder);

            var avatarBuilder = services.AddHttpClient<IAvatarApi, AvatarApi>(client =>
            {
                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            });
            configureAuthenticatedClient?.Invoke(avatarBuilder);
        }

        return services;
    }
}
