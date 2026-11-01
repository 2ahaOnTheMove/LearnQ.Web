using LearnQ.Infrastructure;
using LearnQ.Presentation.Components;
using LearnQ.Presentation.Routing;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(AppRoutes.Error, createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute(AppRoutes.NotFound, createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
