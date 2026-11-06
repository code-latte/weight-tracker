using FluentMigrator;

namespace WeightTracker.Migrations.Migrations;

[Migration(202611061200)]
public sealed class CreateWorkoutTemplatesTable_202611061200 : Migration
{
    public override void Up()
    {
        // Same name and description columns as "Workouts"; a template has no owner, date, status or mesocycle
        Create.Table("WorkoutTemplates")
            .WithColumn("Id").AsGuid().NotNullable().PrimaryKey("PK_WorkoutTemplates")
            .WithColumn("TrainerId").AsGuid().NotNullable()
            .WithColumn("Name").AsString(256).NotNullable()
            .WithColumn("Description").AsString(1024).Nullable();

        Create.ForeignKey("FK_WorkoutTemplates_Trainer")
            .FromTable("WorkoutTemplates").ForeignColumn("TrainerId")
            .ToTable("Users").PrimaryColumn("Id");

        Create.Index("IX_WorkoutTemplates_TrainerId").OnTable("WorkoutTemplates").OnColumn("TrainerId").Ascending();
    }

    public override void Down()
    {
        Delete.Table("WorkoutTemplates");
    }
}
