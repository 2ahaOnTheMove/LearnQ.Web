using LearnQ.Presentation.Components;
using LearnQ.Presentation.Endpoints;

namespace LearnQ.Presentation.Startup;

public static class WebApplicationExtensions
{
    public static IApplicationBuilder UseAppPipeline(this IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseAppSecurity(env);
        return app;
    }

    public static IEndpointRouteBuilder MapAppEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapStaticAssets().AllowAnonymous();
        routes.MapLogoutEndpoint();
        routes.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();
        return routes;
    }
}
