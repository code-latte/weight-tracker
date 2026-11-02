using FluentMigrator;
using System.Data;

namespace WeightTracker.Migrations.Migrations;

[Migration(202610301452)]
public sealed class CreateTrainerInvitationsTable_202610301452 : Migration
{
    public override void Up()
    {
        Create.Table("TrainerInvitations")
            .WithColumn("Id").AsGuid().NotNullable().PrimaryKey("PK_TrainerInvitations")
            .WithColumn("TrainerId").AsGuid().NotNullable()
            // Length aligned with Email.MaxLength
            .WithColumn("InviteEmail").AsString(254).NotNullable()
            // Null when the email was not registered as an athlete at invitation time
            .WithColumn("UserId").AsGuid().Nullable()
            // Only the digest is stored; the raw token never reaches the database.
            // Length aligned with HashedTokens.MaxLength
            .WithColumn("TokenHash").AsString(128).NotNullable()
            // Integer value of TrainerInvitationStatus: 1 PENDING, 2 ACCEPTED, 3 REJECTED, 4 REVOKED
            .WithColumn("Status").AsInt32().NotNullable()
            .WithColumn("CreatedAtUtc").AsCustom("timestamptz").NotNullable()
            .WithColumn("ExpiresAtUtc").AsCustom("timestamptz").NotNullable()
            .WithColumn("AcceptedAtUtc").AsCustom("timestamptz").Nullable();

        Create.ForeignKey("FK_TrainerInvitations_Trainer")
            .FromTable("TrainerInvitations").ForeignColumn("TrainerId")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDelete(Rule.None);

        Create.ForeignKey("FK_TrainerInvitations_User")
            .FromTable("TrainerInvitations").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDelete(Rule.None);

        Create.UniqueConstraint("UQ_TrainerInvitations_TokenHash")
            .OnTable("TrainerInvitations").Column("TokenHash");

        // The duplicate pending invitation check looks up by trainer and email
        Create.Index("IX_TrainerInvitations_TrainerId_InviteEmail")
            .OnTable("TrainerInvitations")
            .OnColumn("TrainerId").Ascending()
            .OnColumn("InviteEmail").Ascending();
    }

    public override void Down()
    {
        Delete.Table("TrainerInvitations");
    }
}
