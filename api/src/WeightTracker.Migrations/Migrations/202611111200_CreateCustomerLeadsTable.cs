using FluentMigrator;

namespace WeightTracker.Migrations.Migrations;

/// <summary>
/// Stores the emails of people interested in the product, and when they registered.
/// </summary>
[Migration(202611111200)]
public sealed class CreateCustomerLeadsTable_202611111200 : Migration
{
    public override void Up()
    {
        Create.Table("CustomerLeads")
            .WithColumn("Id").AsGuid().NotNullable().PrimaryKey("PK_CustomerLeads")
            .WithColumn("Email").AsString(254).NotNullable()
            .WithColumn("RegisteredAtUtc").AsCustom("timestamptz").NotNullable();

        // An email is registered once. The use case relies on this name to recognise a concurrent duplicate
        Create.UniqueConstraint("UQ_CustomerLeads_Email").OnTable("CustomerLeads").Column("Email");
    }

    public override void Down()
    {
        Delete.Table("CustomerLeads");
    }
}
