using FluentMigrator;
using System.Data;

namespace WeightTracker.Migrations.Migrations;

/// <summary>
/// Stores the sets an athlete actually performed for each exercise of a workout: reps, weight in kilograms and when.
/// </summary>
[Migration(202611071200)]
public sealed class CreateSetRecordsTable_202611071200 : Migration
{
    public override void Up()
    {
        Create.Table("SetRecords")
            .WithColumn("Id").AsGuid().NotNullable().PrimaryKey("PK_SetRecords")
            .WithColumn("WorkoutExerciseId").AsGuid().NotNullable()
            // Order of the set records of one exercise: by registration time, ties in the order they were added
            .WithColumn("Position").AsInt32().NotNullable()
            .WithColumn("Reps").AsInt32().NotNullable()
            // Kilograms. Aligned with Weight.MaxValue
            .WithColumn("Weight").AsDecimal(6, 2).NotNullable()
            .WithColumn("RegisterDatetimeUtc").AsCustom("timestamptz").NotNullable();

        Create.ForeignKey("FK_SetRecords_WorkoutExercise")
            .FromTable("SetRecords").ForeignColumn("WorkoutExerciseId")
            .ToTable("WorkoutExercises").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);

        Create.Index("IX_SetRecords_WorkoutExerciseId")
            .OnTable("SetRecords")
            .OnColumn("WorkoutExerciseId").Ascending();
    }

    public override void Down()
    {
        Delete.Table("SetRecords");
    }
}
