using FluentMigrator;
using System.Data;

namespace WeightTracker.Migrations.Migrations;

[Migration(202611061201)]
public sealed class CreateWorkoutTemplateExercisesTable_202611061201 : Migration
{
    public override void Up()
    {
        // Mirrors "WorkoutExercises" with every column added to it since it was created
        Create.Table("WorkoutTemplateExercises")
            .WithColumn("Id").AsGuid().NotNullable().PrimaryKey("PK_WorkoutTemplateExercises")
            .WithColumn("WorkoutTemplateId").AsGuid().NotNullable()
            .WithColumn("ExerciseId").AsGuid().NotNullable()
            // The aggregate holds an ordered collection; the database would otherwise lose it
            .WithColumn("Position").AsInt32().NotNullable()
            .WithColumn("Effort").AsInt32().NotNullable()
            // Null means the exercise is not part of a superseries
            .WithColumn("SuperSeriesId").AsInt32().Nullable()
            .WithColumn("PerSide").AsBoolean().NotNullable()
            // Minutes between sets, null when no rest is prescribed
            .WithColumn("RestMinutes").AsDecimal(5, 2).Nullable();

        Create.ForeignKey("FK_WorkoutTemplateExercises_WorkoutTemplate")
            .FromTable("WorkoutTemplateExercises").ForeignColumn("WorkoutTemplateId")
            .ToTable("WorkoutTemplates").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);

        Create.ForeignKey("FK_WorkoutTemplateExercises_Exercise")
            .FromTable("WorkoutTemplateExercises").ForeignColumn("ExerciseId")
            .ToTable("Exercises").PrimaryColumn("Id");

        // An exercise appears at most once per template; its sets are grouped under it
        Create.UniqueConstraint("UQ_WorkoutTemplateExercises_WorkoutTemplateId_ExerciseId")
            .OnTable("WorkoutTemplateExercises").Columns("WorkoutTemplateId", "ExerciseId");
    }

    public override void Down()
    {
        Delete.Table("WorkoutTemplateExercises");
    }
}
