using FluentMigrator;
using System.Data;

namespace WeightTracker.Migrations.Migrations;

/// <summary>
/// Stores the weeks defined inside each mesocycle. Weeks may be missing while the plan is being built.
/// </summary>
[Migration(202611051043)]
public sealed class CreateMesocycleWeeksTable_202611051043 : Migration
{
    public override void Up()
    {
        Create.Table("MesocycleWeeks")
            .WithColumn("Id").AsGuid().NotNullable().PrimaryKey("PK_MesocycleWeeks")
            .WithColumn("MesocycleId").AsGuid().NotNullable()
            .WithColumn("WeekNumber").AsInt32().NotNullable()
            .WithColumn("TrainingWeekType").AsInt32().NotNullable()
            .WithColumn("Objective").AsString(4000).NotNullable();

        Create.ForeignKey("FK_MesocycleWeeks_Mesocycle")
            .FromTable("MesocycleWeeks").ForeignColumn("MesocycleId")
            .ToTable("Mesocycles").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);

        // A week number appears at most once per mesocycle; workouts point to it
        Create.UniqueConstraint("UQ_MesocycleWeeks_MesocycleId_WeekNumber")
            .OnTable("MesocycleWeeks").Columns("MesocycleId", "WeekNumber");
    }

    public override void Down()
    {
        Delete.Table("MesocycleWeeks");
    }
}
