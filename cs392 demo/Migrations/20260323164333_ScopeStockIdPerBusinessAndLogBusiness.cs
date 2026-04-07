using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace cs392_demo.Migrations
{
    /// <inheritdoc />
    public partial class ScopeStockIdPerBusinessAndLogBusiness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Inventory_Activity_Log_Stock_Stock_ID_Log",
                table: "Inventory_Activity_Log");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Stock",
                table: "Stock");

            migrationBuilder.DropIndex(
                name: "IX_Inventory_Activity_Log_Stock_ID_Log",
                table: "Inventory_Activity_Log");

            migrationBuilder.AddColumn<int>(
                name: "Stock_Key",
                table: "Stock",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<string>(
                name: "BusinessId",
                table: "Inventory_Activity_Log",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE l
SET l.BusinessId = s.BusinessId
FROM Inventory_Activity_Log l
INNER JOIN Stock s ON s.Stock_ID = l.Stock_ID_Log
WHERE l.BusinessId IS NULL AND s.BusinessId IS NOT NULL;");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Stock",
                table: "Stock",
                column: "Stock_Key");

            migrationBuilder.CreateIndex(
                name: "IX_Stock_BusinessId_Stock_ID",
                table: "Stock",
                columns: new[] { "BusinessId", "Stock_ID" },
                unique: true,
                filter: "[BusinessId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Inventory_Activity_Log_BusinessId_Stock_ID_Log",
                table: "Inventory_Activity_Log",
                columns: new[] { "BusinessId", "Stock_ID_Log" });

            migrationBuilder.AddForeignKey(
                name: "FK_Inventory_Activity_Log_Business_BusinessId",
                table: "Inventory_Activity_Log",
                column: "BusinessId",
                principalTable: "Business",
                principalColumn: "Business_ID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Inventory_Activity_Log_Business_BusinessId",
                table: "Inventory_Activity_Log");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Stock",
                table: "Stock");

            migrationBuilder.DropIndex(
                name: "IX_Stock_BusinessId_Stock_ID",
                table: "Stock");

            migrationBuilder.DropIndex(
                name: "IX_Inventory_Activity_Log_BusinessId_Stock_ID_Log",
                table: "Inventory_Activity_Log");

            migrationBuilder.DropColumn(
                name: "Stock_Key",
                table: "Stock");

            migrationBuilder.DropColumn(
                name: "BusinessId",
                table: "Inventory_Activity_Log");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Stock",
                table: "Stock",
                column: "Stock_ID");

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
    }
}
