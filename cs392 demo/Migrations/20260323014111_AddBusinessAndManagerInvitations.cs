using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace cs392_demo.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessAndManagerInvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BusinessId",
                table: "Inventory_Location",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessId",
                table: "AspNetUsers",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Business",
                columns: table => new
                {
                    Business_ID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Business_Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Invite_Code = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Business", x => x.Business_ID);
                });

            migrationBuilder.CreateTable(
                name: "ManagerInvitation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManagerInvitation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManagerInvitation_Business_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Business",
                        principalColumn: "Business_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Inventory_Location_BusinessId",
                table: "Inventory_Location",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_BusinessId",
                table: "AspNetUsers",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_Business_Invite_Code",
                table: "Business",
                column: "Invite_Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ManagerInvitation_BusinessId",
                table: "ManagerInvitation",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_ManagerInvitation_Token",
                table: "ManagerInvitation",
                column: "Token",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Business_BusinessId",
                table: "AspNetUsers",
                column: "BusinessId",
                principalTable: "Business",
                principalColumn: "Business_ID",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Inventory_Location_Business_BusinessId",
                table: "Inventory_Location",
                column: "BusinessId",
                principalTable: "Business",
                principalColumn: "Business_ID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Business_BusinessId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Inventory_Location_Business_BusinessId",
                table: "Inventory_Location");

            migrationBuilder.DropTable(
                name: "ManagerInvitation");

            migrationBuilder.DropTable(
                name: "Business");

            migrationBuilder.DropIndex(
                name: "IX_Inventory_Location_BusinessId",
                table: "Inventory_Location");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_BusinessId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "BusinessId",
                table: "Inventory_Location");

            migrationBuilder.DropColumn(
                name: "BusinessId",
                table: "AspNetUsers");
        }
    }
}
