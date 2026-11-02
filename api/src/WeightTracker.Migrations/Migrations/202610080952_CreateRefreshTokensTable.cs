using FluentMigrator;
using System.Data;

namespace WeightTracker.Migrations.Migrations;

[Migration(202610080952)]
public sealed class CreateRefreshTokensTable_202610080952 : Migration
{
    public override void Up()
    {
        Create.Table("RefreshTokens")
            .WithColumn("Id").AsGuid().NotNullable().PrimaryKey("PK_RefreshTokens")
            .WithColumn("UserId").AsGuid().NotNullable()
            // Only the digest is stored; the raw token never reaches the database.
            // Length aligned with HashedTokens.MaxLength
            .WithColumn("TokenHash").AsString(128).NotNullable()
            .WithColumn("ExpiresAt").AsCustom("timestamptz").NotNullable()
            .WithColumn("RevokedAt").AsCustom("timestamptz").Nullable()
            .WithColumn("ReplacedByTokenId").AsGuid().Nullable()
            .WithColumn("CreatedAt").AsCustom("timestamptz").NotNullable();

        Create.ForeignKey("FK_RefreshTokens_User")
            .FromTable("RefreshTokens").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);

        Create.ForeignKey("FK_RefreshTokens_ReplacedBy")
            .FromTable("RefreshTokens").ForeignColumn("ReplacedByTokenId")
            .ToTable("RefreshTokens").PrimaryColumn("Id");

        // Lookup on redemption happens by hash
        Create.UniqueConstraint("UQ_RefreshTokens_TokenHash")
            .OnTable("RefreshTokens").Column("TokenHash");

        Create.Index("IX_RefreshTokens_UserId")
            .OnTable("RefreshTokens").OnColumn("UserId").Ascending();
    }

    public override void Down()
    {
        Delete.Table("RefreshTokens");
    }
}
