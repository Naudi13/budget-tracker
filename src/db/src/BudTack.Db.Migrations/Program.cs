using BudTack.Db.Migrations;
using BudTack.Db.Migrations.Configuration;
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;

// BudTack.Db.Migrations — the one migration sequence of the database (ADR-005).
//   up    apply every pending migration
//   down  roll back every applied migration, to version 0
//   list  show every migration and whether it is applied
// Connects with MIGRATOR_DATABASE_URL, never with the API's DATABASE_URL.

const string usage = "Usage: dotnet run --project src/BudTack.Db.Migrations -- <up|down|list>";

var command = args.Length == 1 ? args[0].ToLowerInvariant() : string.Empty;
if (command is not ("up" or "down" or "list"))
{
    Console.Error.WriteLine(usage);
    return 2;
}

DotEnv.LoadIfPresent(Path.Combine(Directory.GetCurrentDirectory(), ".env"));

var url = Environment.GetEnvironmentVariable("MIGRATOR_DATABASE_URL");
if (string.IsNullOrWhiteSpace(url))
{
    Console.Error.WriteLine("MIGRATOR_DATABASE_URL is not set. Copy .env.example to .env.");
    return 2;
}

string connectionString;
try
{
    connectionString = PostgresUrl.ToConnectionString(url);
}
catch (FormatException error)
{
    Console.Error.WriteLine(error.Message);
    return 2;
}

using var services = new ServiceCollection()
    .AddFluentMigratorCore()
    .ConfigureRunner(runner => runner
        .AddPostgres()
        .WithGlobalConnectionString(connectionString)
        .WithVersionTable(new SchemaVersionTable())
        .ScanIn(typeof(SchemaVersionTable).Assembly).For.Migrations())
    .AddLogging(logging => logging.AddFluentMigratorConsole())
    .BuildServiceProvider(validateScopes: false);

var migrator = services.GetRequiredService<IMigrationRunner>();

try
{
    switch (command)
    {
        case "up":
            migrator.MigrateUp();
            break;
        case "down":
            migrator.MigrateDown(0);
            break;
        default:
            migrator.ListMigrations();
            break;
    }
}
catch (Exception error)
{
    Console.Error.WriteLine($"Migration '{command}' failed: {error.Message}");
    return 1;
}

return 0;
