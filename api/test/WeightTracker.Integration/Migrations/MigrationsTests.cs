using System.Text.Json;
using FluentAssertions;
using Npgsql;
using WeightTracker.Domain.Exercises;
using WeightTracker.Domain.Users;
using WeightTracker.Infra.Services;
using WeightTracker.Integration.Bootstrap;
using WeightTracker.Migrations;
using WeightTracker.Migrations.Migrations;
using Xunit;

namespace WeightTracker.Integration.Migrations;

/// <summary>
/// Verifies the schema applies from scratch and reverses cleanly.
/// </summary>
/// <remarks>
/// Uses a throwaway database of its own on the suite's container, so it never disturbs the
/// database the rest of the suite shares.
/// </remarks>
[Collection(ApiCollection.Name)]
public class MigrationsTests : IAsyncLifetime
{
    private const string DatabaseName = "weight-tracker-migrations-test";

    /// <summary>Last migration before <c>InsertAdminUser_202610080919</c> that it depends on.</summary>
    private const long BeforeAdminUserVersion = 202610080917;

    /// <summary>Last migration before <c>AddEffortToWorkoutExercises_202610131437</c> that it depends on.</summary>
    private const long BeforeEffortVersion = 202610080944;

    /// <summary>Last migration before <c>AddStatusToWorkouts_202610182247</c> that it depends on.</summary>
    private const long BeforeStatusVersion = 202610161647;

    /// <summary>
    /// <c>AddEffortToWorkoutExercises_202610131437</c>. Its backfill test stops here because
    /// <c>AddExerciseTranslationsAndMuscle_202610270035</c> only runs on an empty Exercises table.
    /// </summary>
    private const long EffortVersion = 202610131437;

    /// <summary>Last migration before <c>AddExerciseTranslationsAndMuscle_202610270035</c>.</summary>
    private const long BeforeExerciseTranslationsVersion = 202610212330;

    /// <summary>Last migration before <c>ImportFreeExerciseCatalog_202610271200</c>.</summary>
    private const long BeforeCatalogVersion = 202610270035;

    /// <summary>Last migration before <c>AddTypeToExercises_202610281200</c>.</summary>
    private const long BeforeExerciseTypeVersion = 202610271847;

    /// <summary>Last migration before <c>AddActivationToUsers_202610291200</c>.</summary>
    private const long BeforeActivationVersion = 202610281200;

    /// <summary>Last migration before <c>CreateTrainerAthletesTable_202610301437</c>.</summary>
    private const long BeforeTrainerAthletesVersion = 202610291200;

    /// <summary>Two users for the trainer–athlete tests. Roles are not enforced by the schema.</summary>
    private const string TrainerAndAthlete = """
        INSERT INTO "Users" ("Id", "Email", "Name", "Surname", "DateBirth", "Role", "PasswordHash", "CreatedAt", "IsActive")
        VALUES ('00000000-0000-0000-0000-0000000000d1', 'trainer-migration@weighttracker.test', 'T', 'T', '1990-01-01', 2, 'hash', now(), true),
               ('00000000-0000-0000-0000-0000000000d2', 'athlete-migration@weighttracker.test', 'A', 'A', '1990-01-01', 1, 'hash', now(), true);
        """;

    /// <summary>Entries in <c>infra/exercises.json</c>.</summary>
    private const int CatalogSize = 876;

    private static readonly string[] ExpectedTables =
    [
        "Users",
        "Exercises",
        "ExerciseTranslations",
        "Workouts",
        "WorkoutExercises",
        "WorkoutExerciseSets",
        "RefreshTokens",
        "TrainerAthletes",
        "TrainerInvitations",
    ];

    private readonly string _maintenanceConnectionString;

    private readonly string _connectionString;

    public MigrationsTests(ApiFactory factory)
    {
        _maintenanceConnectionString = factory.ConnectionStringFor("postgres");

        // Pooling is off because dropping the database between tests terminates pooled
        // connections, which would otherwise poison the pool for the next test.
        _connectionString = factory.ConnectionStringFor(DatabaseName, pooling: false);
    }

    public async Task InitializeAsync()
    {
        await DropDatabase();
        await Execute(_maintenanceConnectionString, $"""CREATE DATABASE "{DatabaseName}" """);
    }

    public Task DisposeAsync() => DropDatabase();

    [Fact]
    public void MigrateUp_ShouldCreateEveryTableFromAnEmptyDatabase()
    {
        // Arrange & Act
        DatabaseMigrator.MigrateUp(_connectionString, ApiFactory.AdminUser);

        // Assert
        foreach (var table in ExpectedTables)
            TableExists(table).Should().BeTrue($"migration should have created '{table}'");
    }

    [Fact]
    public void MigrateUp_ShouldBeIdempotentWhenRunTwice()
    {
        // Arrange
        DatabaseMigrator.MigrateUp(_connectionString, ApiFactory.AdminUser);

        // Act
        var act = () => DatabaseMigrator.MigrateUp(_connectionString, ApiFactory.AdminUser);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void MigrateDown_ShouldRemoveEveryTable()
    {
        // Arrange
        DatabaseMigrator.MigrateUp(_connectionString, ApiFactory.AdminUser);

        // Act
        DatabaseMigrator.MigrateDown(_connectionString);

        // Assert
        foreach (var table in ExpectedTables)
            TableExists(table).Should().BeFalse($"rollback should have dropped '{table}'");
    }

    [Fact]
    public async Task AddEffort_ShouldBackfillExistingRowsWithMid()
    {
        // Arrange
        DatabaseMigrator.MigrateUp(_connectionString, ApiFactory.AdminUser, BeforeEffortVersion);
        await Execute(_connectionString, """
            INSERT INTO "Users" ("Id", "Email", "Name", "Surname", "DateBirth", "Role", "PasswordHash", "CreatedAt")
            VALUES ('00000000-0000-0000-0000-0000000000a1', 'legacy@weighttracker.test', 'Legacy', 'User',
                    '1990-01-01', 1, 'hash', now());
            INSERT INTO "Exercises" ("Id", "Name", "VideoUrl", "BodyPart")
            VALUES ('00000000-0000-0000-0000-0000000000a2', 'Press Banca', NULL, 3);
            INSERT INTO "Workouts" ("Id", "UserId", "TrainerId", "WorkoutDate")
            VALUES ('00000000-0000-0000-0000-0000000000a3', '00000000-0000-0000-0000-0000000000a1', NULL, '2026-10-01');
            INSERT INTO "WorkoutExercises" ("Id", "WorkoutId", "ExerciseId", "Position")
            VALUES ('00000000-0000-0000-0000-0000000000a4', '00000000-0000-0000-0000-0000000000a3',
                    '00000000-0000-0000-0000-0000000000a2', 0);
            """);

        // Act
        DatabaseMigrator.MigrateUp(_connectionString, ApiFactory.AdminUser, EffortVersion);

        // Assert
        (await Scalar<int>("SELECT \"Effort\" FROM \"WorkoutExercises\"")).Should().Be(3);
    }

    [Fact]
    public async Task AddEffort_ShouldLeaveNoDefaultForNewRows()
    {
        // Arrange & Act
        DatabaseMigrator.MigrateUp(_connectionString, ApiFactory.AdminUser);

        // Assert
        (await Scalar<bool>("""
            SELECT column_default IS NULL FROM information_schema.columns
            WHERE table_name = 'WorkoutExercises' AND column_name = 'Effort'
            """)).Should().BeTrue();
    }

    [Fact]
    public async Task AddEffort_Down_ShouldRemoveTheColumn()
    {
        // Arrange
        DatabaseMigrator.MigrateUp(_connectionString, ApiFactory.AdminUser);

        // Act
        DatabaseMigrator.MigrateDown(_connectionString, BeforeEffortVersion);

        // Assert
        (await Scalar<bool>("""
            SELECT EXISTS (SELECT 1 FROM information_schema.columns
            WHERE table_name = 'WorkoutExercises' AND column_name = 'Effort')
            """)).Should().BeFalse();
    }

    [Fact]
    public async Task AddStatus_ShouldBackfillExistingRowsWithDraft()
    {
        // Arrange
        DatabaseMigrator.MigrateUp(_connectionString, ApiFactory.AdminUser, BeforeStatusVersion);
        await Execute(_connectionString, """
            INSERT INTO "Users" ("Id", "Email", "Name", "Surname", "DateBirth", "Role", "PasswordHash", "CreatedAt")
            VALUES ('00000000-0000-0000-0000-0000000000b1', 'legacy-status@weighttracker.test', 'Legacy', 'User',
                    '1990-01-01', 1, 'hash', now());
            INSERT INTO "Workouts" ("Id", "UserId", "TrainerId", "WorkoutDate")
            VALUES ('00000000-0000-0000-0000-0000000000b2', '00000000-0000-0000-0000-0000000000b1', NULL, '2026-10-01');
            """);

        // Act
        DatabaseMigrator.MigrateUp(_connectionString, ApiFactory.AdminUser);

        // Assert
        (await Scalar<int>("SELECT \"Status\" FROM \"Workouts\"")).Should().Be(1);
    }

    [Fact]
    public async Task AddStatus_ShouldLeaveNoDefaultForNewRows()
    {
        // Arrange & Act
        DatabaseMigrator.MigrateUp(_connectionString, ApiFactory.AdminUser);

        // Assert
        (await Scalar<bool>("""
            SELECT column_default IS NULL FROM information_schema.columns
            WHERE table_name = 'Workouts' AND column_name = 'Status'
            """)).Should().BeTrue();
    }

    [Fact]
    public async Task AddStatus_Down_ShouldRemoveTheColumn()
    {
        // Arrange
        DatabaseMigrator.MigrateUp(_connectionString, ApiFactory.AdminUser);

        // Act
        DatabaseMigrator.MigrateDown(_connectionString, BeforeStatusVersion);

        // Assert
        (await Scalar<bool>("""
            SELECT EXISTS (SELECT 1 FROM information_schema.columns
            WHERE table_name = 'Workouts' AND column_name = 'Status')
            """)).Should().BeFalse();
    }

    [Fact]
    public async Task AddExerciseTranslations_ShouldReplaceNameAndBodyPartWithMuscleAndFreeExerciseDbId()
    {
        // Arrange & Act
        DatabaseMigrator.MigrateUp(_connectionString, ApiFactory.AdminUser);

        // Assert
        (await ColumnsOf("Exercises")).Should().BeEquivalentTo(["Id", "VideoUrl", "Muscle", "Type", "FreeExerciseDbId"]);
        (await ColumnsOf("ExerciseTranslations")).Should().BeEquivalentTo(["Id", "ExerciseId", "LanguageCode", "Name"]);
        (await Scalar<int>("""
            SELECT character_maximum_length FROM information_schema.columns
            WHERE table_name = 'Exercises' AND column_name = 'FreeExerciseDbId'
            """)).Should().Be(128);
        (await Scalar<int>("""
            SELECT character_maximum_length FROM information_schema.columns
            WHERE table_name = 'ExerciseTranslations' AND column_name = 'LanguageCode'
            """)).Should().Be(2);
    }

    private const string ExerciseWithSpanishTranslation = """
        INSERT INTO "Exercises" ("Id", "VideoUrl", "Muscle", "Type", "FreeExerciseDbId")
        VALUES ('00000000-0000-0000-0000-0000000000c1', NULL, 6, 1, 'Test_Exercise_With_Translation');
        INSERT INTO "ExerciseTranslations" ("Id", "ExerciseId", "LanguageCode", "Name")
        VALUES ('00000000-0000-0000-0000-0000000000c2', '00000000-0000-0000-0000-0000000000c1', 'es', 'Press Banca');
        """;

    [Fact]
    public async Task AddExerciseTranslations_ShouldRejectASecondTranslationForTheSameLanguage()
    {
        // Arrange
        DatabaseMigrator.MigrateUp(_connectionString, ApiFactory.AdminUser);
        await Execute(_connectionString, ExerciseWithSpanishTranslation);

        // Act
        var act = () => Execute(_connectionString, """
            INSERT INTO "ExerciseTranslations" ("Id", "ExerciseId", "LanguageCode", "Name")
            VALUES ('00000000-0000-0000-0000-0000000000c3', '00000000-0000-0000-0000-0000000000c1', 'es', 'Press de Banca');
            """);

        // Assert
        (await act.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
    }

    [Fact]
    public async Task AddExerciseTranslations_ShouldDeleteTranslationsWithTheirExercise()
    {
        // Arrange
        DatabaseMigrator.MigrateUp(_connectionString, ApiFactory.AdminUser);
        await Execute(_connectionString, ExerciseWithSpanishTranslation);

        // Act
        await Execute(_connectionString, """DELETE FROM "Exercises" WHERE "Id" = '00000000-0000-0000-0000-0000000000c1'""");

        // Assert
        (await Scalar<long>("""
            SELECT count(*) FROM "ExerciseTranslations" WHERE "ExerciseId" = '00000000-0000-0000-0000-0000000000c1'
            """)).Should().Be(0);
    }

    [Fact]
    public async Task AddExerciseTranslations_ShouldRejectADuplicateFreeExerciseDbIdButAllowSeveralNulls()
    {
        // Arrange
        DatabaseMigrator.MigrateUp(_connectionString, ApiFactory.AdminUser);
        await Execute(_connectionString, """
            INSERT INTO "Exercises" ("Id", "VideoUrl", "Muscle", "Type", "FreeExerciseDbId") VALUES
                ('00000000-0000-0000-0000-0000000000d1', NULL, 6, 1, 'Test_Exercise_Duplicate'),
                ('00000000-0000-0000-0000-0000000000d2', NULL, 6, 1, NULL),
                ('00000000-0000-0000-0000-0000000000d3', NULL, 6, 1, NULL);
            """);

        // Act
        var act = () => Execute(_connectionString, """
            INSERT INTO "Exercises" ("Id", "VideoUrl", "Muscle", "Type", "FreeExerciseDbId")
            VALUES ('00000000-0000-0000-0000-0000000000d4', NULL, 6, 1, 'Test_Exercise_Duplicate');
            """);

        // Assert
        (await act.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
    }

    [Fact]
    public async Task AddExerciseTranslations_Down_ShouldRestoreNameAndBodyPartOnATableWithRows()
    {
        // Arrange
        DatabaseMigrator.MigrateUp(_connectionString, ApiFactory.AdminUser);
        await Execute(_connectionString, """
            INSERT INTO "Exercises" ("Id", "VideoUrl", "Muscle", "Type", "FreeExerciseDbId")
            VALUES ('00000000-0000-0000-0000-0000000000e1', NULL, 6, 1, NULL);
            """);

        // Act
        DatabaseMigrator.MigrateDown(_connectionString, BeforeExerciseTranslationsVersion);

        // Assert
        TableExists("ExerciseTranslations").Should().BeFalse();
        (await ColumnsOf("Exercises")).Should().BeEquivalentTo(["Id", "Name", "VideoUrl", "BodyPart"]);
        (await Scalar<long>("SELECT count(*) FROM \"Exercises\"")).Should().Be(1);
    }

    [Fact]
    public async Task ImportCatalog_ShouldInsertEveryDatasetExerciseWithAnEnglishAndASpanishName()
    {
        // Arrange & Act
        DatabaseMigrator.MigrateUp(_connectionString, ApiFactory.AdminUser);

        // Assert
        (await Scalar<long>("SELECT count(*) FROM \"Exercises\"")).Should().Be(CatalogSize);
        (await Scalar<long>("""
            SELECT count(*) FROM "Exercises" e
            WHERE (SELECT count(*) FROM "ExerciseTranslations" t WHERE t."ExerciseId" = e."Id" AND t."LanguageCode" = 'en') = 1
              AND (SELECT count(*) FROM "ExerciseTranslations" t WHERE t."ExerciseId" = e."Id" AND t."LanguageCode" = 'es') = 1
            """)).Should().Be(CatalogSize);
        (await Scalar<long>("SELECT count(*) FROM \"ExerciseTranslations\"")).Should().Be(CatalogSize * 2);
    }

    [Fact]
    public async Task ImportCatalog_ShouldStoreEveryDatasetIdOfTheSourceFile()
    {
        // Arrange
        var expected = DatasetIds();

        // Act
        DatabaseMigrator.MigrateUp(_connectionString, ApiFactory.AdminUser);

        // Assert
        (await Strings("SELECT \"FreeExerciseDbId\" FROM \"Exercises\"")).Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task ImportCatalog_ShouldStoreOnlyDefinedMusclesAndValidNames()
    {
        // Arrange & Act
        DatabaseMigrator.MigrateUp(_connectionString, ApiFactory.AdminUser);

        // Assert
        (await Strings("SELECT DISTINCT \"Muscle\"::text FROM \"Exercises\""))
            .Should().OnlyContain(muscle => Enum.IsDefined(typeof(Muscles), int.Parse(muscle)));
        (await Strings("SELECT \"Name\" FROM \"ExerciseTranslations\""))
            .Should().OnlyContain(name => name == name.Trim()
                && name.Length >= ExerciseName.MinLength && name.Length <= ExerciseName.MaxLength);
    }

    [Fact]
    public async Task ImportCatalog_ShouldStoreOnlyVideoUrlsThatPassTheDomainRules()
    {
        // Arrange & Act
        DatabaseMigrator.MigrateUp(_connectionString, ApiFactory.AdminUser);

        // Assert
        var stored = await Strings("SELECT \"VideoUrl\" FROM \"Exercises\" WHERE \"VideoUrl\" IS NOT NULL");
        stored.Should().NotBeEmpty();
        stored.Should().OnlyContain(videoUrl => videoUrl.StartsWith("https://www.youtube.com/watch?v="));
        foreach (var videoUrl in stored)
            Url.From(videoUrl).Value.Should().Be(videoUrl);
    }

    [Fact]
    public async Task ImportCatalog_ShouldStoreTheVideoOfAnEntryThatHasOne()
    {
        // Arrange
        DatabaseMigrator.MigrateUp(_connectionString, ApiFactory.AdminUser);

        // Act
        var videoUrl = await Scalar<string>("""SELECT "VideoUrl" FROM "Exercises" WHERE "FreeExerciseDbId" = 'Barbell_Squat'""");

        // Assert
        videoUrl.Should().Be("https://www.youtube.com/watch?v=eFYv8Skf66g");
    }

    [Theory]
    [InlineData("Barbell_Squat", Muscles.QUADRICEPS, "Barbell Squat", "Sentadilla con barra")]
    [InlineData("Barbell_Bench_Press_-_Medium_Grip", Muscles.CHEST, "Barbell Bench Press - Medium Grip", "Press de banca")]
    [InlineData("Kettlebell_Halo_With_Overhead_Extension", Muscles.SHOULDERS, "Kettlebell Halo with Overhead Extension", "Halo con kettlebell y extensión por encima de la cabeza")]
    public async Task ImportCatalog_ShouldStoreTheMuscleAndNamesOfAnEntry(
        string freeExerciseDbId, Muscles muscle, string english, string spanish)
    {
        // Arrange
        DatabaseMigrator.MigrateUp(_connectionString, ApiFactory.AdminUser);

        // Act
        var stored = await Scalar<int>($"""SELECT "Muscle" FROM "Exercises" WHERE "FreeExerciseDbId" = '{freeExerciseDbId}'""");

        // Assert
        stored.Should().Be((int)muscle);
        (await TranslationOf(freeExerciseDbId, "en")).Should().Be(english);
        (await TranslationOf(freeExerciseDbId, "es")).Should().Be(spanish);
    }

    [Fact]
    public async Task ImportCatalog_Down_ShouldRemoveEveryCatalogRowAndKeepTheTables()
    {
        // Arrange
        DatabaseMigrator.MigrateUp(_connectionString, ApiFactory.AdminUser);

        // Act
        DatabaseMigrator.MigrateDown(_connectionString, BeforeCatalogVersion);

        // Assert
        (await Scalar<long>("SELECT count(*) FROM \"Exercises\"")).Should().Be(0);
        (await Scalar<long>("SELECT count(*) FROM \"ExerciseTranslations\"")).Should().Be(0);
        TableExists("ExerciseTranslations").Should().BeTrue();
    }

    [Fact]
    public async Task InsertAdminUser_ShouldInsertTheConfiguredAdmin()
    {
        // Arrange & Act
        DatabaseMigrator.MigrateUp(_connectionString, ApiFactory.AdminUser);

        // Assert
        (await Scalar<string>(AdminColumn("Email"))).Should().Be(ApiFactory.AdminUser.Email);
        (await Scalar<int>(AdminColumn("Role"))).Should().Be((int)Role.ADMIN);
        new PasswordHasher().Verify(ApiFactory.AdminUser.Password, PasswordHash.From(await Scalar<string>(AdminColumn("PasswordHash"))))
            .Should().BeTrue("login must be able to verify the stored hash");
    }

    [Fact]
    public void InsertAdminUser_ShouldFailWithoutConfiguredCredentials()
    {
        // Arrange
        var missingCredentials = new AdminUserOptions();

        // Act
        var act = () => DatabaseMigrator.MigrateUp(_connectionString, missingCredentials);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*AdminUser:Email*");
    }

    [Fact]
    public async Task InsertAdminUser_Down_ShouldRemoveTheAdmin()
    {
        // Arrange
        DatabaseMigrator.MigrateUp(_connectionString, ApiFactory.AdminUser);

        // Act
        DatabaseMigrator.MigrateDown(_connectionString, BeforeAdminUserVersion);

        // Assert
        (await Scalar<long>("SELECT count(*) FROM \"Users\"")).Should().Be(0);
    }

    [Fact]
    public async Task AddActivation_ShouldBackfillExistingUsersAsActive()
    {
        // Arrange
        DatabaseMigrator.MigrateUp(_connectionString, ApiFactory.AdminUser, BeforeActivationVersion);
        await Execute(_connectionString, """
            INSERT INTO "Users" ("Id", "Email", "Name", "Surname", "DateBirth", "Role", "PasswordHash", "CreatedAt")
            VALUES ('00000000-0000-0000-0000-0000000000c1', 'legacy-activation@weighttracker.test', 'Legacy', 'User',
                    '1990-01-01', 1, 'hash', now());
            """);

        // Act
        DatabaseMigrator.MigrateUp(_connectionString, ApiFactory.AdminUser);

        // Assert
        (await Scalar<long>("SELECT count(*) FROM \"Users\"")).Should().Be(2);
        (await Scalar<bool>("SELECT bool_and(\"IsActive\") FROM \"Users\"")).Should().BeTrue();
        (await Scalar<bool>("SELECT bool_and(\"DeactivatedAtUtc\" IS NULL) FROM \"Users\"")).Should().BeTrue();
    }

    [Fact]
    public async Task AddActivation_ShouldLeaveNoDefaultForNewRows()
    {
        // Arrange & Act
        DatabaseMigrator.MigrateUp(_connectionString, ApiFactory.AdminUser);

        // Assert
        (await Scalar<bool>("""
            SELECT column_default IS NULL FROM information_schema.columns
            WHERE table_name = 'Users' AND column_name = 'IsActive'
            """)).Should().BeTrue();
    }

    [Fact]
    public async Task AddActivation_Down_ShouldRemoveBothColumns()
    {
        // Arrange
        DatabaseMigrator.MigrateUp(_connectionString, ApiFactory.AdminUser);

        // Act
        DatabaseMigrator.MigrateDown(_connectionString, BeforeActivationVersion);

        // Assert
        (await Scalar<bool>("""
            SELECT EXISTS (SELECT 1 FROM information_schema.columns
            WHERE table_name = 'Users' AND column_name IN ('IsActive', 'DeactivatedAtUtc'))
            """)).Should().BeFalse();
    }

    private static string AdminColumn(string column) =>
        $"SELECT \"{column}\" FROM \"Users\" WHERE \"Id\" = '{InsertAdminUser_202610080919.AdminId}'";

    private Task<string> TranslationOf(string freeExerciseDbId, string languageCode)
        => Scalar<string>($"""
            SELECT t."Name" FROM "ExerciseTranslations" t JOIN "Exercises" e ON e."Id" = t."ExerciseId"
            WHERE e."FreeExerciseDbId" = '{freeExerciseDbId}' AND t."LanguageCode" = '{languageCode}'
            """);

    /// <summary>The <c>id</c> of every entry of <c>infra/exercises.json</c>, found by walking up from the test binaries.</summary>
    private static IReadOnlyList<string> DatasetIds()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && File.Exists(Path.Combine(directory.FullName, "infra", "exercises.json")) == false)
            directory = directory.Parent;

        directory.Should().NotBeNull("infra/exercises.json must be reachable from the test binaries");

        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory!.FullName, "infra", "exercises.json")));

        return document.RootElement.EnumerateArray().Select(entry => entry.GetProperty("id").GetString()!).ToList();
    }

    private async Task<IReadOnlyList<string>> Strings(string sql)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);

        var values = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
            values.Add(reader.GetString(0));

        return values;
    }

    private async Task<IReadOnlyList<string>> ColumnsOf(string table)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            "SELECT column_name FROM information_schema.columns WHERE table_name = @table", connection);
        command.Parameters.AddWithValue("table", table);

        var columns = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
            columns.Add(reader.GetString(0));

        return columns;
    }

    private async Task<T> Scalar<T>(string sql)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);

        return (T)(await command.ExecuteScalarAsync())!;
    }

    private bool TableExists(string table)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();

        using var command = new NpgsqlCommand(
            "SELECT to_regclass(@table) IS NOT NULL", connection);
        command.Parameters.AddWithValue("table", $"public.\"{table}\"");

        return (bool)command.ExecuteScalar()!;
    }

    private async Task DropDatabase()
    {
        NpgsqlConnection.ClearAllPools();

        await Execute(
            _maintenanceConnectionString,
            $"""DROP DATABASE IF EXISTS "{DatabaseName}" WITH (FORCE)""");
    }

    private static async Task Execute(string connectionString, string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task AddType_ShouldTypeEveryCatalogExerciseFromTheCatalog()
    {
        // Arrange & Act
        DatabaseMigrator.MigrateUp(_connectionString, ApiFactory.AdminUser);

        // Assert
        (await Scalar<long>("SELECT COUNT(*) FROM \"Exercises\" WHERE \"Type\" IS NULL")).Should().Be(0);
        (await Scalar<long>("SELECT COUNT(*) FROM \"Exercises\" WHERE \"Type\" = 1")).Should().Be(739);
        (await Scalar<long>("SELECT COUNT(*) FROM \"Exercises\" WHERE \"Type\" = 2")).Should().Be(14);
        (await Scalar<long>("SELECT COUNT(*) FROM \"Exercises\" WHERE \"Type\" = 3")).Should().Be(123);
        (await Scalar<bool>("""
            SELECT is_nullable = 'NO' FROM information_schema.columns
            WHERE table_name = 'Exercises' AND column_name = 'Type'
            """)).Should().BeTrue();
    }

    [Fact]
    public async Task AddType_ShouldKeepTheMuscleOfACardioExercise()
    {
        // Arrange & Act
        DatabaseMigrator.MigrateUp(_connectionString, ApiFactory.AdminUser);

        // Assert
        (await Scalar<int>("SELECT \"Type\" FROM \"Exercises\" WHERE \"FreeExerciseDbId\" = 'Running_Treadmill'")).Should().Be(2);
        (await Scalar<int>("SELECT \"Muscle\" FROM \"Exercises\" WHERE \"FreeExerciseDbId\" = 'Running_Treadmill'")).Should().Be(14);
    }

    [Fact]
    public async Task AddType_Down_ShouldRemoveTheColumn()
    {
        // Arrange
        DatabaseMigrator.MigrateUp(_connectionString, ApiFactory.AdminUser);

        // Act
        DatabaseMigrator.MigrateDown(_connectionString, BeforeExerciseTypeVersion);

        // Assert
        (await Scalar<bool>("""
            SELECT EXISTS (SELECT 1 FROM information_schema.columns
            WHERE table_name = 'Exercises' AND column_name = 'Type')
            """)).Should().BeFalse();
        (await Scalar<long>("SELECT COUNT(*) FROM \"Exercises\"")).Should().Be(CatalogSize);
    }

    [Fact]
    public async Task CreateTrainerAthletes_ShouldRejectASecondActiveRelationshipForTheSameAthlete()
    {
        // Arrange
        DatabaseMigrator.MigrateUp(_connectionString, ApiFactory.AdminUser);
        await Execute(_connectionString, TrainerAndAthlete);
        await Execute(_connectionString, """
            INSERT INTO "TrainerAthletes" ("Id", "TrainerId", "UserId", "Status", "StartedAtUtc")
            VALUES ('00000000-0000-0000-0000-0000000000e1', '00000000-0000-0000-0000-0000000000d1',
                    '00000000-0000-0000-0000-0000000000d2', 1, now());
            """);

        // Act
        var act = () => Execute(_connectionString, """
            INSERT INTO "TrainerAthletes" ("Id", "TrainerId", "UserId", "Status", "StartedAtUtc")
            VALUES ('00000000-0000-0000-0000-0000000000e2', '00000000-0000-0000-0000-0000000000d1',
                    '00000000-0000-0000-0000-0000000000d2', 1, now());
            """);

        // Assert
        (await act.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
    }

    [Fact]
    public async Task CreateTrainerAthletes_ShouldAllowEndedRelationshipsAlongsideAnActiveOne()
    {
        // Arrange
        DatabaseMigrator.MigrateUp(_connectionString, ApiFactory.AdminUser);
        await Execute(_connectionString, TrainerAndAthlete);

        // Act
        await Execute(_connectionString, """
            INSERT INTO "TrainerAthletes" ("Id", "TrainerId", "UserId", "Status", "StartedAtUtc", "FinishedAtUtc")
            VALUES ('00000000-0000-0000-0000-0000000000e1', '00000000-0000-0000-0000-0000000000d1',
                    '00000000-0000-0000-0000-0000000000d2', 2, now(), now()),
                   ('00000000-0000-0000-0000-0000000000e2', '00000000-0000-0000-0000-0000000000d1',
                    '00000000-0000-0000-0000-0000000000d2', 2, now(), now()),
                   ('00000000-0000-0000-0000-0000000000e3', '00000000-0000-0000-0000-0000000000d1',
                    '00000000-0000-0000-0000-0000000000d2', 1, now(), NULL);
            """);

        // Assert
        (await Scalar<long>("SELECT COUNT(*) FROM \"TrainerAthletes\"")).Should().Be(3);
    }

    [Fact]
    public async Task CreateTrainerInvitations_ShouldRejectADuplicatedTokenHash()
    {
        // Arrange
        DatabaseMigrator.MigrateUp(_connectionString, ApiFactory.AdminUser);
        await Execute(_connectionString, TrainerAndAthlete);
        const string insert = """
            INSERT INTO "TrainerInvitations" ("Id", "TrainerId", "InviteEmail", "TokenHash", "Status", "CreatedAtUtc", "ExpiresAtUtc")
            VALUES ('{0}', '00000000-0000-0000-0000-0000000000d1', 'someone@weighttracker.test', 'same-hash', 1, now(), now() + interval '7 days');
            """;
        await Execute(_connectionString, string.Format(insert, "00000000-0000-0000-0000-0000000000f1"));

        // Act
        var act = () => Execute(_connectionString, string.Format(insert, "00000000-0000-0000-0000-0000000000f2"));

        // Assert
        (await act.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
    }

    [Fact]
    public void CreateTrainerTables_Down_ShouldRemoveBothTables()
    {
        // Arrange
        DatabaseMigrator.MigrateUp(_connectionString, ApiFactory.AdminUser);

        // Act
        DatabaseMigrator.MigrateDown(_connectionString, BeforeTrainerAthletesVersion);

        // Assert
        TableExists("TrainerAthletes").Should().BeFalse();
        TableExists("TrainerInvitations").Should().BeFalse();
        TableExists("Users").Should().BeTrue();
    }
}
