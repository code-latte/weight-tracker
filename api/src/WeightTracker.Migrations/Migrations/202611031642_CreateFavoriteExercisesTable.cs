using FluentMigrator;
using System.Data;

namespace WeightTracker.Migrations.Migrations;

/// <summary>
/// Stores the exercises each user has marked as favorites, and when.
/// </summary>
[Migration(202611031642)]
public sealed class CreateFavoriteExercisesTable_202611031642 : Migration
{
    public override void Up()
    {
        Create.Table("FavoriteExercises")
            .WithColumn("Id").AsGuid().NotNullable().PrimaryKey("PK_FavoriteExercises")
            .WithColumn("UserId").AsGuid().NotNullable()
            .WithColumn("ExerciseId").AsGuid().NotNullable()
            .WithColumn("CreatedAtUtc").AsCustom("timestamptz").NotNullable();

        // Users and exercises are never hard-deleted, so no foreign key needs to cascade
        Create.ForeignKey("FK_FavoriteExercises_User")
            .FromTable("FavoriteExercises").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDelete(Rule.None);

        Create.ForeignKey("FK_FavoriteExercises_Exercise")
            .FromTable("FavoriteExercises").ForeignColumn("ExerciseId")
            .ToTable("Exercises").PrimaryColumn("Id")
            .OnDelete(Rule.None);

        // A user marks an exercise once. The index also serves the list of a user's favorites
        Create.Index("IX_FavoriteExercises_UserId_ExerciseId")
            .OnTable("FavoriteExercises")
            .OnColumn("UserId").Ascending()
            .OnColumn("ExerciseId").Ascending()
            .WithOptions().Unique();
    }

    public override void Down()
    {
        Delete.Table("FavoriteExercises");
    }
}
