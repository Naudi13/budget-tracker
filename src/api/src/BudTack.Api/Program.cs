using BudTack.Api.Platform.Config;
using BudTack.Api.Platform.Errors;
using BudTack.Api.Platform.Logging;
using BudTack.Api.Platform.Web;
using BudTack.Dashboard;
using BudTack.Identity;
using BudTack.Transactions;
using Microsoft.Extensions.Logging.Console;

// Local development reads .env; staging and production inject the same
// variables from the platform's secret store, which always wins.
DotEnv.LoadIfPresent(DotEnv.Find(Directory.GetCurrentDirectory()));

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging
    .AddConsole(options => options.FormatterName = JsonLogFormatter.FormatterName)
    .AddConsoleFormatter<JsonLogFormatter, ConsoleFormatterOptions>();

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddApiErrors();
builder.Services.AddHealth();
builder.Services.AddBearerAuthentication();
builder.Services.AddPortalCors(builder.Configuration);
builder.Services.AddApiDocumentation();
InternalControllers.FindInternalControllers(
    builder.Services.AddControllers(options => options.Filters.Add<ModuleErrorFilter>()));

// Modules, in the order of 09-modules/module-catalog.md.
builder.Services.AddIdentityModule(IdentityConfiguration.Read(builder.Configuration));
builder.Services.AddTransactionsModule();
builder.Services.AddDashboardModule();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseApiErrors();
app.UseCors();
app.UseApiDocumentation();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealth();
app.MapProtectedControllers();

app.Run();

/// <summary>Entry point; public so the end-to-end tests can host it.</summary>
public partial class Program;
