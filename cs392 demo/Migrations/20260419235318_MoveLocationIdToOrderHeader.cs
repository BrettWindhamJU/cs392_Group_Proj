using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace cs392_demo.Migrations
{
    /// <inheritdoc />
    public partial class MoveLocationIdToOrderHeader : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "PurchaseOrderLineItem");

            migrationBuilder.AddColumn<string>(
                name: "LocationId",
                table: "PurchaseOrder",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "PurchaseOrder");

            migrationBuilder.AddColumn<string>(
                name: "LocationId",
                table: "PurchaseOrderLineItem",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
