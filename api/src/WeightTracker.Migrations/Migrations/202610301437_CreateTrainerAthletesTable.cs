using FluentMigrator;
using System.Data;

namespace WeightTracker.Migrations.Migrations;

[Migration(202610301437)]
public sealed class CreateTrainerAthletesTable_202610301437 : Migration
{
    public override void Up()
    {
        Create.Table("TrainerAthletes")
            .WithColumn("Id").AsGuid().NotNullable().PrimaryKey("PK_TrainerAthletes")
            .WithColumn("TrainerId").AsGuid().NotNullable()
            .WithColumn("UserId").AsGuid().NotNullable()
            // Integer value of TrainerAthleteStatus: 1 ACTIVE, 2 ENDED
            .WithColumn("Status").AsInt32().NotNullable()
            .WithColumn("StartedAtUtc").AsCustom("timestamptz").NotNullable()
            .WithColumn("FinishedAtUtc").AsCustom("timestamptz").Nullable();

        // Relationships are history: deleting a user must not silently erase them
        Create.ForeignKey("FK_TrainerAthletes_Trainer")
            .FromTable("TrainerAthletes").ForeignColumn("TrainerId")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDelete(Rule.None);

        Create.ForeignKey("FK_TrainerAthletes_User")
            .FromTable("TrainerAthletes").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDelete(Rule.None);

        Create.Index("IX_TrainerAthletes_TrainerId")
            .OnTable("TrainerAthletes").OnColumn("TrainerId").Ascending();

        // An athlete has at most one active trainer. Ended rows are kept and do not count
        Execute.Sql(
            """
            CREATE UNIQUE INDEX "UQ_TrainerAthletes_UserId_Active"
                ON "TrainerAthletes" ("UserId")
                WHERE "Status" = 1
            """);
    }

    public override void Down()
    {
        Execute.Sql("""DROP INDEX IF EXISTS "UQ_TrainerAthletes_UserId_Active" """);
        Delete.Table("TrainerAthletes");
    }
}
