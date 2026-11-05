using FluentMigrator;
using System.Data;

namespace WeightTracker.Migrations.Migrations;

/// <summary>
/// Lets a workout point to a week of a mesocycle. Both columns are null for a standalone workout,
/// so every existing row stays standalone.
/// </summary>
[Migration(202611051049)]
public sealed class AddMesocycleToWorkouts_202611051049 : Migration
{
    public override void Up()
    {
        Alter.Table("Workouts")
            .AddColumn("MesocycleId").AsGuid().Nullable()
            .AddColumn("MesocycleWeekNumber").AsInt32().Nullable();

        // No cascade: a workout keeps its history if mesocycles are ever deleted.
        // The week number has no foreign key because weeks are sparse and can be removed
        Create.ForeignKey("FK_Workouts_Mesocycle")
            .FromTable("Workouts").ForeignColumn("MesocycleId")
            .ToTable("Mesocycles").PrimaryColumn("Id")
            .OnDelete(Rule.None);

        // A workout is placed in a mesocycle week with both values, or with neither
        Execute.Sql(
            """
            ALTER TABLE "Workouts"
                ADD CONSTRAINT "CK_Workouts_MesocycleLink"
                CHECK (("MesocycleId" IS NULL) = ("MesocycleWeekNumber" IS NULL))
            """);

        // Serves future reads joining workouts with their mesocycle week
        Create.Index("IX_Workouts_MesocycleId_MesocycleWeekNumber")
            .OnTable("Workouts")
            .OnColumn("MesocycleId").Ascending()
            .OnColumn("MesocycleWeekNumber").Ascending();
    }

    public override void Down()
    {
        Execute.Sql("""ALTER TABLE "Workouts" DROP CONSTRAINT IF EXISTS "CK_Workouts_MesocycleLink" """);
        Delete.Index("IX_Workouts_MesocycleId_MesocycleWeekNumber").OnTable("Workouts");
        Delete.ForeignKey("FK_Workouts_Mesocycle").OnTable("Workouts");
        Delete.Column("MesocycleWeekNumber").FromTable("Workouts");
        Delete.Column("MesocycleId").FromTable("Workouts");
    }
}
