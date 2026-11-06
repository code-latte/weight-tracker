using FluentMigrator;
using System.Data;

namespace WeightTracker.Migrations.Migrations;

[Migration(202611061202)]
public sealed class CreateWorkoutTemplateExerciseSetsTable_202611061202 : Migration
{
    public override void Up()
    {
        // Mirrors "WorkoutExerciseSets" with every column added to it since it was created
        Create.Table("WorkoutTemplateExerciseSets")
            .WithColumn("Id").AsGuid().NotNullable().PrimaryKey("PK_WorkoutTemplateExerciseSets")
            .WithColumn("WorkoutTemplateExerciseId").AsGuid().NotNullable()
            .WithColumn("Position").AsInt32().NotNullable()
            .WithColumn("Count").AsInt32().NotNullable()
            .WithColumn("TargetType").AsString(20).NotNullable()
            // Null for the target types that carry no value
            .WithColumn("TargetValue").AsInt32().Nullable()
            // Null for every target type except RepsRange, whose min stays in TargetValue
            .WithColumn("TargetMaxValue").AsInt32().Nullable()
            // Null for every target type except Minutes
            .WithColumn("TargetMinutes").AsDecimal(5, 2).Nullable();

        Create.ForeignKey("FK_WorkoutTemplateExerciseSets_WorkoutTemplateExercise")
            .FromTable("WorkoutTemplateExerciseSets").ForeignColumn("WorkoutTemplateExerciseId")
            .ToTable("WorkoutTemplateExercises").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);
    }

    public override void Down()
    {
        Delete.Table("WorkoutTemplateExerciseSets");
    }
}
