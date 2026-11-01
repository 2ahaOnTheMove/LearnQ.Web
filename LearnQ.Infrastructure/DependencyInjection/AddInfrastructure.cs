using LearnQ.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LearnQ.Infrastructure.DependencyInjection;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // Validate and bind Api options
        services.Configure<ApiOptions>(config.GetRequiredSection(ApiOptions.SectionName));
        services.AddOptions<ApiOptions>(ApiOptions.SectionName)
            .Bind(config.GetRequiredSection(ApiOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSessionStorage(config);
        services.AddApiClients(config);

        return services;
    }
}
