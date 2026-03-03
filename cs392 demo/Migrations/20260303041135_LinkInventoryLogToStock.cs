using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace cs392_demo.Migrations
{
    /// <inheritdoc />
    public partial class LinkInventoryLogToStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Inventory_Activity_Log",
                table: "Inventory_Activity_Log");

            migrationBuilder.AlterColumn<string>(
                name: "Stock_ID_Log",
                table: "Inventory_Activity_Log",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Inventory_Activity_Log",
                table: "Inventory_Activity_Log",
                column: "Log_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Inventory_Activity_Log_Stock_ID_Log",
                table: "Inventory_Activity_Log",
                column: "Stock_ID_Log");

            migrationBuilder.AddForeignKey(
                name: "FK_Inventory_Activity_Log_Stock_Stock_ID_Log",
                table: "Inventory_Activity_Log",
                column: "Stock_ID_Log",
                principalTable: "Stock",
                principalColumn: "Stock_ID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Inventory_Activity_Log_Stock_Stock_ID_Log",
                table: "Inventory_Activity_Log");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Inventory_Activity_Log",
                table: "Inventory_Activity_Log");

            migrationBuilder.DropIndex(
                name: "IX_Inventory_Activity_Log_Stock_ID_Log",
                table: "Inventory_Activity_Log");

            migrationBuilder.AlterColumn<string>(
                name: "Stock_ID_Log",
                table: "Inventory_Activity_Log",
                type: "nvarchar(1)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Inventory_Activity_Log",
                table: "Inventory_Activity_Log",
                column: "Stock_ID_Log");
        }
    }
}
