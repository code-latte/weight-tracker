using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System.Linq.Expressions;
using Testcontainers.PostgreSql;
using WeightTracker.Infra.Persistence;
using WeightTracker.Infra.Seed;
using WeightTracker.Migrations;
using Xunit;

namespace WeightTracker.Integration.Bootstrap;

/// <summary>
/// Hosts the API in process against a real Postgres database.
/// </summary>
/// <remarks>
/// Each test run starts its own throwaway Postgres container through Testcontainers, migrates
/// it once, and removes it when the run ends, so every run begins from a fresh database and
/// never touches the development data from <c>infra/docker-compose.yml</c>.
/// </remarks>
public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string DatabaseName = "weight-tracker-test";

    private const string SigningKey = "integration-test-signing-key-at-least-32-bytes";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17")
        .WithDatabase(DatabaseName)
        .Build();

    static ApiFactory()
    {
        // The API validates its configuration at the top of Program, before the host is
        // built, so the overrides have to be in place through the environment variable
        // provider rather than through ConfigureAppConfiguration.
        Environment.SetEnvironmentVariable("Jwt__SigningKey", SigningKey);
    }

    /// <summary>
    /// Credentials of the administrator the migrations insert. <see cref="ResetDatabase"/>
    /// truncates that row, so tests authenticate as the seeded users instead.
    /// </summary>
    public static readonly AdminUserOptions AdminUser = new()
    {
        Email = "owner@weighttracker.test",
        Password = "Owner1234!",
    };

    /// <summary>
    /// Connection string of the suite's database. Only valid once the container has started.
    /// </summary>
    public string ConnectionString => _postgres.GetConnectionString();

    /// <summary>
    /// Connection string pointing at another database on the suite's container, for tests that
    /// need a throwaway database of their own.
    /// </summary>
    public string ConnectionStringFor(string database, bool pooling = true) =>
        new NpgsqlConnectionStringBuilder(ConnectionString)
        {
            Database = database,
            Pooling = pooling,
        }.ConnectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Not Development: the fixture owns migrating and seeding, not application startup
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = ConnectionString,
                ["Jwt:SigningKey"] = SigningKey,
                // High enough that the limiter, whose state survives ResetDatabase, never rejects a test.
                // Tests of the limit itself build a derived factory with a lower one.
                ["RateLimiting:CustomerLeads:PermitLimit"] = "10000",
            }));
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // The container's port is only known once it is running. The host is built lazily on
        // the first CreateClient, after this fixture is initialised, so Program still sees it.
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", ConnectionString);

        DatabaseMigrator.MigrateUp(ConnectionString, AdminUser);
    }

    /// <summary>
    /// Runs an action against a fresh dependency injection scope, so repository tests use
    /// the same wiring the API uses.
    /// </summary>
    public async Task<TResult> WithScope<TResult>(Func<IServiceProvider, Task<TResult>> action)
    {
        using var scope = Services.CreateScope();

        return await action(scope.ServiceProvider);
    }

    /// <summary>
    /// Counts the stored rows of a table that match a condition, for tests that must prove rows are gone
    /// when no repository exposes them, such as the children of an aggregate deleted by cascade.
    /// </summary>
    public Task<int> CountRows<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : class
        => WithScope(services => services.GetRequiredService<WeightTrackerDbContext>().Set<TEntity>().CountAsync(predicate));

    /// <summary>
    /// Empties every table and reapplies the seeded dataset, so each test starts from the
    /// same known state.
    /// </summary>
    public async Task ResetDatabase()
    {
        using var scope = Services.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<WeightTrackerDbContext>();

        await context.Database.ExecuteSqlRawAsync(
            """
            TRUNCATE TABLE
                "CustomerLeads",
                "FavoriteExercises",
                "MesocycleWeeks",
                "Mesocycles",
                "TrainerInvitations",
                "TrainerAthletes",
                "RefreshTokens",
                "WorkoutTemplateExerciseSets",
                "WorkoutTemplateExercises",
                "WorkoutTemplates",
                "WorkoutExerciseSets",
                "WorkoutExercises",
                "Workouts",
                "Users",
                "Exercises"
            RESTART IDENTITY CASCADE
            """);

        await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().Seed(CancellationToken.None);
    }

    /// <summary>
    /// Deletes a seeded user, and the workouts, workout templates, mesocycles, trainer–athlete relationships, trainer invitations and
    /// favorite exercises they take part in, so a test can exercise a
    /// token whose user no longer exists. No endpoint removes users. The next
    /// <see cref="ResetDatabase"/> restores the baseline.
    /// </summary>
    public async Task RemoveUser(string email)
    {
        using var scope = Services.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<WeightTrackerDbContext>();

        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            DELETE FROM "FavoriteExercises"
            WHERE "UserId" IN (SELECT "Id" FROM "Users" WHERE "Email" = {email.ToLowerInvariant()})
            """);

        // A reviewed workout cannot lose its reviewer, so the workouts the user reviewed go as well
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            DELETE FROM "Workouts"
            WHERE "UserId" IN (SELECT "Id" FROM "Users" WHERE "Email" = {email.ToLowerInvariant()})
               OR "TrainerId" IN (SELECT "Id" FROM "Users" WHERE "Email" = {email.ToLowerInvariant()})
               OR "ReviewedBy" IN (SELECT "Id" FROM "Users" WHERE "Email" = {email.ToLowerInvariant()})
            """);

        // Workouts of other users placed in the user's mesocycles become standalone before those mesocycles go
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE "Workouts" SET "MesocycleId" = NULL, "MesocycleWeekNumber" = NULL
            WHERE "MesocycleId" IN (
                SELECT "Id" FROM "Mesocycles"
                WHERE "AthleteId" IN (SELECT "Id" FROM "Users" WHERE "Email" = {email.ToLowerInvariant()})
                   OR "TrainerId" IN (SELECT "Id" FROM "Users" WHERE "Email" = {email.ToLowerInvariant()}))
            """);

        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            DELETE FROM "Mesocycles"
            WHERE "AthleteId" IN (SELECT "Id" FROM "Users" WHERE "Email" = {email.ToLowerInvariant()})
               OR "TrainerId" IN (SELECT "Id" FROM "Users" WHERE "Email" = {email.ToLowerInvariant()})
            """);

        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            DELETE FROM "WorkoutTemplates"
            WHERE "TrainerId" IN (SELECT "Id" FROM "Users" WHERE "Email" = {email.ToLowerInvariant()})
            """);

        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            DELETE FROM "TrainerAthletes"
            WHERE "UserId" IN (SELECT "Id" FROM "Users" WHERE "Email" = {email.ToLowerInvariant()})
               OR "TrainerId" IN (SELECT "Id" FROM "Users" WHERE "Email" = {email.ToLowerInvariant()})
            """);

        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            DELETE FROM "TrainerInvitations"
            WHERE "UserId" IN (SELECT "Id" FROM "Users" WHERE "Email" = {email.ToLowerInvariant()})
               OR "TrainerId" IN (SELECT "Id" FROM "Users" WHERE "Email" = {email.ToLowerInvariant()})
            """);

        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""DELETE FROM "Users" WHERE "Email" = {email.ToLowerInvariant()}""");
    }

    // Removing the container drops the database with it, so the next run starts fresh
    async Task IAsyncLifetime.DisposeAsync() => await _postgres.DisposeAsync();
}
