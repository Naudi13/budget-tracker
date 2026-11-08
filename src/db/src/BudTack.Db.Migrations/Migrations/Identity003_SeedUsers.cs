using FluentMigrator;

namespace BudTack.Db.Migrations.Migrations;

/// <summary>
/// One test user per role, the seed the course guide asks for (HU-09). Their
/// passwords are public test values, listed in this repository's README, and
/// only their BCrypt hashes are stored here (ADR-008). A real deployment must
/// replace these users before opening to anyone (ADR-007).
/// </summary>
[Migration(202611082301, "Seed one test user per role")]
public sealed class Identity003_SeedUsers : Migration
{
    private const string AdminId = "00000000-0000-4000-8000-000000000001";
    private const string UserId = "00000000-0000-4000-8000-000000000002";

    public override void Up() =>
        Execute.Sql($"""
            INSERT INTO identity.users (id, name, email, password_hash, role) VALUES
              ('{AdminId}', 'Administrador', 'admin@budgettracker.test',
               '$2a$12$kWKh4iUORy6GAZEhMHINt.BdOo1UsDtMmPMqoXq8iVSpRkRTICIlK', 'ADMIN'),
              ('{UserId}', 'Usuario de prueba', 'usuario@budgettracker.test',
               '$2a$12$9lcnol7F.Pcvtzb.Nrf9AOvw09SIS8ZeoiQ3U34hmFMk01qYLf4KO', 'USER');
            """);

    public override void Down() =>
        Execute.Sql($"DELETE FROM identity.users WHERE id IN ('{AdminId}', '{UserId}');");
}
