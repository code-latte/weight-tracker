using FluentMigrator;

namespace WeightTracker.Migrations.Migrations;

/// <summary>
/// Records the last time a workout reached started. Existing workouts get an estimate, because the real time was
/// never stored: completed and reviewed ones start 50 minutes to 1 h 15 min before <c>CompletedAtUtc</c>, started
/// ones 15 to 20 minutes before <c>ResultsUpdatedAtUtc</c>. Any other workout stays empty. The status is written
/// as its integer value (3 started, 4 completed, 5 reviewed) so the migration does not depend on the domain.
/// </summary>
[Migration(202611131300)]
public sealed class AddStartedAtToWorkouts_202611131300 : Migration
{
    public override void Up()
    {
        Alter.Table("Workouts")
            .AddColumn("StartedAtUtc").AsCustom("timestamptz").Nullable();

        // random() is volatile, so every row gets its own offset
        Execute.Sql("""
            UPDATE "Workouts"
            SET "StartedAtUtc" = "CompletedAtUtc" - (interval '50 minutes' + random() * interval '25 minutes')
            WHERE "Status" IN (4, 5) AND "CompletedAtUtc" IS NOT NULL
            """);

        Execute.Sql("""
            UPDATE "Workouts"
            SET "StartedAtUtc" = "ResultsUpdatedAtUtc" - (interval '15 minutes' + random() * interval '5 minutes')
            WHERE "Status" = 3 AND "ResultsUpdatedAtUtc" IS NOT NULL
            """);
    }

    /// <summary>
    /// Destructive: the estimated start dates are lost.
    /// </summary>
    public override void Down()
    {
        Delete.Column("StartedAtUtc").FromTable("Workouts");
    }
}
