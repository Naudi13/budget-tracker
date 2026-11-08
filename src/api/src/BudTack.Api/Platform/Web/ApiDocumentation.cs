using Microsoft.OpenApi;

namespace BudTack.Api.Platform.Web;

/// <summary>
/// Swagger UI at <c>/swagger-ui.html</c>, public (ADR-008); the document lists
/// every controller action, and "Authorize" takes a token from <c>POST /sessions</c>.
/// </summary>
internal static class ApiDocumentation
{
    private const string SecurityScheme = "bearer";
    private const string Document = "v1";

    public static IServiceCollection AddApiDocumentation(this IServiceCollection services) =>
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(Document, new OpenApiInfo { Title = "Budget Tracker API", Version = Document });
            options.AddSecurityDefinition(SecurityScheme, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = SecurityScheme,
                BearerFormat = "JWT",
            });
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(SecurityScheme, document)] = [],
            });
        });

    public static WebApplication UseApiDocumentation(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint($"/swagger/{Document}/swagger.json", "Budget Tracker API");
            options.RoutePrefix = "swagger";
        });
        app.MapGet("/swagger-ui.html", () => Results.Redirect("/swagger/index.html"));
        return app;
    }
}
