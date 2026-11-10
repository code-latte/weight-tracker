using FluentMigrator;

namespace WeightTracker.Migrations.Migrations;

/// <summary>
/// Records the last time a workout reached completed. Empty until it is first completed, and for
/// workouts completed before the column existed.
/// </summary>
[Migration(202611101200)]
public sealed class AddCompletedAtToWorkouts_202611101200 : Migration
{
    public override void Up()
    {
        Alter.Table("Workouts")
            .AddColumn("CompletedAtUtc").AsCustom("timestamptz").Nullable();
    }

    public override void Down()
    {
        Delete.Column("CompletedAtUtc").FromTable("Workouts");
    }
}
