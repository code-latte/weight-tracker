using FluentMigrator;
using System.Data;

namespace WeightTracker.Migrations.Migrations;

/// <summary>
/// Stores mesocycles: training plans from a trainer to an athlete.
/// </summary>
[Migration(202611051037)]
public sealed class CreateMesocyclesTable_202611051037 : Migration
{
    public override void Up()
    {
        Create.Table("Mesocycles")
            .WithColumn("Id").AsGuid().NotNullable().PrimaryKey("PK_Mesocycles")
            .WithColumn("TrainerId").AsGuid().NotNullable()
            .WithColumn("AthleteId").AsGuid().NotNullable()
            .WithColumn("Name").AsString(256).NotNullable()
            .WithColumn("Objective").AsString(4000).NotNullable()
            .WithColumn("StartDate").AsDate().NotNullable()
            .WithColumn("DurationInWeeks").AsInt32().NotNullable();

        // Users are never hard-deleted, so no foreign key needs to cascade
        Create.ForeignKey("FK_Mesocycles_Trainer")
            .FromTable("Mesocycles").ForeignColumn("TrainerId")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDelete(Rule.None);

        Create.ForeignKey("FK_Mesocycles_Athlete")
            .FromTable("Mesocycles").ForeignColumn("AthleteId")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDelete(Rule.None);

        // Mesocycles are read per trainer and per athlete
        Create.Index("IX_Mesocycles_TrainerId")
            .OnTable("Mesocycles").OnColumn("TrainerId").Ascending();

        Create.Index("IX_Mesocycles_AthleteId")
            .OnTable("Mesocycles").OnColumn("AthleteId").Ascending();
    }

    public override void Down()
    {
        Delete.Table("Mesocycles");
    }
}
