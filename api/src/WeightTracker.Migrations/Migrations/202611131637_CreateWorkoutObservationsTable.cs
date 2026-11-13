using FluentMigrator;
using System.Data;

namespace WeightTracker.Migrations.Migrations;

/// <summary>
/// Stores the observations generated when a prescribed workout is completed, one row per observation, and which of them
/// are highlighted. The rows of a workout are replaced as a whole when it is completed again.
/// </summary>
[Migration(202611131637)]
public sealed class CreateWorkoutObservationsTable_202611131637 : Migration
{
    public override void Up()
    {
        Create.Table("WorkoutObservations")
            .WithColumn("Id").AsGuid().NotNullable().PrimaryKey("PK_WorkoutObservations")
            .WithColumn("WorkoutId").AsGuid().NotNullable()
            // Null when the observation is about the whole workout
            .WithColumn("ExerciseId").AsGuid().Nullable()
            // Name of the ObservationCode, a stable public code
            .WithColumn("Code").AsString(64).NotNullable()
            .WithColumn("Severity").AsInt16().NotNullable()
            // Null when the observation is not eligible to be highlighted
            .WithColumn("Priority").AsInt16().Nullable()
            // Position among the highlights, 1 to 3. Null when not highlighted
            .WithColumn("HighlightRank").AsInt16().Nullable()
            // The numbers the observation was calculated from, as a flat JSON object
            .WithColumn("Data").AsCustom("jsonb").NotNullable().WithDefaultValue(RawSql.Insert("'{}'::jsonb"))
            .WithColumn("CreatedAt").AsCustom("timestamptz").NotNullable();

        Create.ForeignKey("FK_WorkoutObservations_Workouts")
            .FromTable("WorkoutObservations").ForeignColumn("WorkoutId")
            .ToTable("Workouts").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);

        Create.ForeignKey("FK_WorkoutObservations_Exercises")
            .FromTable("WorkoutObservations").ForeignColumn("ExerciseId")
            .ToTable("Exercises").PrimaryColumn("Id")
            .OnDelete(Rule.None);

        Execute.Sql(
            "ALTER TABLE \"WorkoutObservations\" ADD CONSTRAINT \"CK_WorkoutObservations_HighlightRank\" "
            + "CHECK (\"HighlightRank\" IS NULL OR \"HighlightRank\" BETWEEN 1 AND 3)");

        // One observation per code and exercise in a workout. NULLS NOT DISTINCT makes the workout-level ones (no exercise) unique too
        Execute.Sql(
            "CREATE UNIQUE INDEX \"UX_WorkoutObservations_WorkoutId_Code_ExerciseId\" "
            + "ON \"WorkoutObservations\" (\"WorkoutId\", \"Code\", \"ExerciseId\") NULLS NOT DISTINCT");

        // A highlight rank is used once per workout
        Execute.Sql(
            "CREATE UNIQUE INDEX \"UX_WorkoutObservations_WorkoutId_HighlightRank\" "
            + "ON \"WorkoutObservations\" (\"WorkoutId\", \"HighlightRank\") WHERE \"HighlightRank\" IS NOT NULL");

        Create.Index("IX_WorkoutObservations_ExerciseId")
            .OnTable("WorkoutObservations")
            .OnColumn("ExerciseId").Ascending();
    }

    public override void Down()
    {
        Delete.Table("WorkoutObservations");
    }
}
