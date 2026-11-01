using LearnQ.Presentation.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace LearnQ.Presentation.Startup;

public static class PresentationExtensions
{
    public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration config)
    {
        services.AddAppSecurity(config);
        services.AddAppAuthentication(config);
        services.AddAppAuthorization();
        services.AddAppComponents();

        // Register RedisTicketStore and ITicketStore
        services.AddSingleton<RedisTicketStore>();
        services.AddSingleton<ITicketStore>(sp => sp.GetRequiredService<RedisTicketStore>());

        return services;
    }
}
