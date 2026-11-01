using LearnQ.Application.Common.Interfaces;
using LearnQ.Infrastructure.Api.Avatars;
using LearnQ.Infrastructure.Api.Clients;
using LearnQ.Infrastructure.Api.Languages;
using LearnQ.Infrastructure.Api.Onboarding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LearnQ.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var baseUrl = config["Api:BaseUrl"]
                      ?? throw new InvalidOperationException("Api:BaseUrl is not configured.");

        services.AddSingleton<ILanguageCatalog, LanguageCatalog>();

        services.AddHttpClient<IClientApi, ClientApi>(client =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        var useStub = config.GetValue<bool>("Onboarding:UseStub");
        if (useStub)
        {
            services.AddSingleton<IOnboardingApi, StubOnboardingApi>();
            services.AddSingleton<IAvatarApi, StubAvatarApi>();
        }
        else
        {
            services.AddHttpClient<IOnboardingApi, OnboardingApi>(client =>
            {
                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(15);
            });

            services.AddHttpClient<IAvatarApi, AvatarApi>(client =>
            {
                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            });
        }

        return services;
    }
}