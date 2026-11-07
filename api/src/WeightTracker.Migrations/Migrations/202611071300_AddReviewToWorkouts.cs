using FluentMigrator;

namespace WeightTracker.Migrations.Migrations;

/// <summary>
/// Records who reviewed a workout and when, and when its results last changed so a review never
/// rests on outdated results. All three are empty until they happen.
/// </summary>
[Migration(202611071300)]
public sealed class AddReviewToWorkouts_202611071300 : Migration
{
    public override void Up()
    {
        Alter.Table("Workouts")
            .AddColumn("ReviewedAtUtc").AsCustom("timestamptz").Nullable()
            .AddColumn("ReviewedBy").AsGuid().Nullable()
            .AddColumn("ResultsUpdatedAtUtc").AsCustom("timestamptz").Nullable();

        Create.ForeignKey("FK_Workouts_Reviewer")
            .FromTable("Workouts").ForeignColumn("ReviewedBy")
            .ToTable("Users").PrimaryColumn("Id");
    }

    public override void Down()
    {
        Delete.ForeignKey("FK_Workouts_Reviewer").OnTable("Workouts");
        Delete.Column("ReviewedAtUtc").FromTable("Workouts");
        Delete.Column("ReviewedBy").FromTable("Workouts");
        Delete.Column("ResultsUpdatedAtUtc").FromTable("Workouts");
    }
}
