using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace cs392_demo.Migrations
{
    /// <inheritdoc />
    public partial class AllowSameLocationIdAcrossBusinesses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Inventory_Location",
                table: "Inventory_Location");

            migrationBuilder.DropIndex(
                name: "IX_Inventory_Location_BusinessId",
                table: "Inventory_Location");

            migrationBuilder.AddColumn<int>(
                name: "Location_Key",
                table: "Inventory_Location",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Inventory_Location",
                table: "Inventory_Location",
                column: "Location_Key");

            migrationBuilder.CreateIndex(
                name: "IX_Inventory_Location_BusinessId_location_id",
                table: "Inventory_Location",
                columns: new[] { "BusinessId", "location_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Inventory_Location",
                table: "Inventory_Location");

            migrationBuilder.DropIndex(
                name: "IX_Inventory_Location_BusinessId_location_id",
                table: "Inventory_Location");

            migrationBuilder.DropColumn(
                name: "Location_Key",
                table: "Inventory_Location");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Inventory_Location",
                table: "Inventory_Location",
                column: "location_id");

            migrationBuilder.CreateIndex(
                name: "IX_Inventory_Location_BusinessId",
                table: "Inventory_Location",
                column: "BusinessId");
        }
    }
}
