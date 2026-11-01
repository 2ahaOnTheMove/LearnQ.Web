namespace LearnQ.Presentation.Startup;

public static class ComponentsExtensions
{
    public static IServiceCollection AddAppComponents(this IServiceCollection services)
    {
        services.AddRazorComponents()
            .AddInteractiveServerComponents();

        return services;
    }
}
