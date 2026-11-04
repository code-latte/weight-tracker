using FluentMigrator;

namespace WeightTracker.Migrations.Migrations;

/// <summary>
/// Lets trainers and athletes own exercises: who created one, and since when it has been archived.
/// </summary>
[Migration(202611031200)]
public sealed class AddOwnerAndArchiveToExercises_202611031200 : Migration
{
    public override void Up()
    {
        // Null owner means a system exercise, so every existing row stays in the shared catalog
        Alter.Table("Exercises")
            .AddColumn("OwnerId").AsGuid().Nullable()
            .AddColumn("ArchivedAtUtc").AsCustom("timestamptz").Nullable();

        Create.ForeignKey("FK_Exercises_Owner")
            .FromTable("Exercises").ForeignColumn("OwnerId")
            .ToTable("Users").PrimaryColumn("Id");

        Create.Index("IX_Exercises_OwnerId").OnTable("Exercises").OnColumn("OwnerId").Ascending();
    }

    public override void Down()
    {
        Delete.Index("IX_Exercises_OwnerId").OnTable("Exercises");
        Delete.ForeignKey("FK_Exercises_Owner").OnTable("Exercises");
        Delete.Column("ArchivedAtUtc").FromTable("Exercises");
        Delete.Column("OwnerId").FromTable("Exercises");
    }
}
