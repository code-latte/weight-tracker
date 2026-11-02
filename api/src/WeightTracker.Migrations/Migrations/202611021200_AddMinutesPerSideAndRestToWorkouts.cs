using FluentMigrator;

namespace WeightTracker.Migrations.Migrations;

[Migration(202611021200)]
public sealed class AddMinutesPerSideAndRestToWorkouts_202611021200 : Migration
{
    public override void Up()
    {
        // The default only backfills the rows that already exist: none was per side
        Alter.Table("WorkoutExercises")
            .AddColumn("PerSide").AsBoolean().NotNullable().WithDefaultValue(false);

        // Minutes between sets, null when no rest is prescribed
        Alter.Table("WorkoutExercises")
            .AddColumn("RestMinutes").AsDecimal(5, 2).Nullable();

        // Null for every target type except Minutes
        Alter.Table("WorkoutExerciseSets")
            .AddColumn("TargetMinutes").AsDecimal(5, 2).Nullable();
    }

    public override void Down()
    {
        Delete.Column("TargetMinutes").FromTable("WorkoutExerciseSets");
        Delete.Column("RestMinutes").FromTable("WorkoutExercises");
        Delete.Column("PerSide").FromTable("WorkoutExercises");
    }
}
