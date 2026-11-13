using FluentMigrator;

namespace WeightTracker.Migrations.Migrations;

/// <summary>
/// Serves the reads that start from an exercise (the history of a user's sessions of it, its previous execution and its
/// last performance): for an exercise that is rare or never done, Postgres finds its few rows through the index instead
/// of scanning every workout exercise of every user. No column or data changes.
/// </summary>
[Migration(202611131400)]
public sealed class AddExerciseIndexToWorkoutExercises_202611131400 : Migration
{
    public override void Up()
    {
        Create.Index("IX_WorkoutExercises_ExerciseId_WorkoutId")
            .OnTable("WorkoutExercises")
            .OnColumn("ExerciseId").Ascending()
            .OnColumn("WorkoutId").Ascending();
    }

    public override void Down()
    {
        Delete.Index("IX_WorkoutExercises_ExerciseId_WorkoutId").OnTable("WorkoutExercises");
    }
}
