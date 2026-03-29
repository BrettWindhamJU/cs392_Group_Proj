using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace cs392_demo.Migrations
{
    /// <inheritdoc />
    public partial class ScopeStockToBusiness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Location_Stock_ID",
                table: "Stock",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "BusinessId",
                table: "Stock",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stock_BusinessId_Location_Stock_ID",
                table: "Stock",
                columns: new[] { "BusinessId", "Location_Stock_ID" });

            migrationBuilder.AddForeignKey(
                name: "FK_Stock_Business_BusinessId",
                table: "Stock",
                column: "BusinessId",
                principalTable: "Business",
                principalColumn: "Business_ID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Stock_Business_BusinessId",
                table: "Stock");

            migrationBuilder.DropIndex(
                name: "IX_Stock_BusinessId_Location_Stock_ID",
                table: "Stock");

            migrationBuilder.DropColumn(
                name: "BusinessId",
                table: "Stock");

            migrationBuilder.AlterColumn<string>(
                name: "Location_Stock_ID",
                table: "Stock",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
