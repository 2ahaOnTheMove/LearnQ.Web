using LearnQ.Application.Common.Interfaces;
using LearnQ.Infrastructure.Api.Clients;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LearnQ.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var baseUrl = config["Api:BaseUrl"]
                      ?? throw new InvalidOperationException("Api:BaseUrl is not configured.");

        services.AddHttpClient<IClientApi, ClientApi>(client =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        return services;
    }
}