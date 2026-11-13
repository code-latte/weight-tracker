using FluentMigrator;

namespace WeightTracker.Migrations.Migrations;

/// <summary>
/// Serves the read of the last finished workout of a user with a given exercise: the caller's workouts are walked
/// newest first, so the filter and the order come from the index and the read stops at the first match.
/// No column or data changes.
/// </summary>
[Migration(202611131200)]
public sealed class AddUserWorkoutDateIndexToWorkouts_202611131200 : Migration
{
    public override void Up()
    {
        Create.Index("IX_Workouts_UserId_WorkoutDate_Id")
            .OnTable("Workouts")
            .OnColumn("UserId").Ascending()
            .OnColumn("WorkoutDate").Descending()
            .OnColumn("Id").Descending();
    }

    public override void Down()
    {
        Delete.Index("IX_Workouts_UserId_WorkoutDate_Id").OnTable("Workouts");
    }
}
