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
DotEnv.LoadIfPresent(Path.Combine(Directory.GetCurrentDirectory(), ".env"));

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging
    .AddConsole(options => options.FormatterName = JsonLogFormatter.FormatterName)
    .AddConsoleFormatter<JsonLogFormatter, ConsoleFormatterOptions>();

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddApiErrors();
builder.Services.AddHealth();
builder.Services.AddControllers();

// Modules, in the order of 09-modules/module-catalog.md.
builder.Services.AddIdentityModule();
builder.Services.AddTransactionsModule();
builder.Services.AddDashboardModule();

// No CORS: the portal reaches the API through a same-origin proxy (ADR-006),
// so every cross-origin request is refused by the browser.

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseApiErrors();
app.MapHealth();
app.MapControllers();

app.Run();

/// <summary>Entry point; public so the end-to-end tests can host it.</summary>
public partial class Program;
