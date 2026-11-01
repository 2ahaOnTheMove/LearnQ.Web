using LearnQ.Infrastructure.DependencyInjection;
using LearnQ.Presentation.Startup;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddInfrastructure(builder.Configuration)
    .AddPresentation(builder.Configuration);

var app = builder.Build();

app.UseAppPipeline(app.Services.GetRequiredService<IWebHostEnvironment>());
app.MapAppEndpoints();

app.Run();
