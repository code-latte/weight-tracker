using FluentMigrator;

namespace WeightTracker.Migrations.Migrations;

/// <summary>
/// Lets a set record hold a duration in seconds instead of reps, and makes the weight optional (null means not tracked).
/// The kind of a record is read from which column is set, so a check constraint keeps exactly one of them not null.
/// </summary>
[Migration(202611121200)]
public sealed class AddDurationToSetRecords_202611121200 : Migration
{
    public override void Up()
    {
        Alter.Table("SetRecords")
            .AlterColumn("Reps").AsInt32().Nullable()
            .AlterColumn("Weight").AsDecimal(6, 2).Nullable()
            .AddColumn("DurationSeconds").AsInt32().Nullable();

        Execute.Sql("""
            ALTER TABLE "SetRecords"
            ADD CONSTRAINT "CK_SetRecords_RepsXorDuration"
            CHECK (("Reps" IS NULL) <> ("DurationSeconds" IS NULL))
            """);
    }

    /// <summary>
    /// Destructive: duration records have no place in the previous shape, so they are deleted, and an untracked weight becomes 0.
    /// </summary>
    public override void Down()
    {
        Execute.Sql("""DELETE FROM "SetRecords" WHERE "DurationSeconds" IS NOT NULL""");
        Execute.Sql("""UPDATE "SetRecords" SET "Weight" = 0 WHERE "Weight" IS NULL""");

        Execute.Sql("""ALTER TABLE "SetRecords" DROP CONSTRAINT "CK_SetRecords_RepsXorDuration" """);
        Delete.Column("DurationSeconds").FromTable("SetRecords");

        Alter.Table("SetRecords")
            .AlterColumn("Reps").AsInt32().NotNullable()
            .AlterColumn("Weight").AsDecimal(6, 2).NotNullable();
    }
}
