using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddShareBudgetFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BudgetMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BudgetId = table.Column<int>(type: "int", nullable: false),
                    MemberId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BudgetMembers_AspNetUsers_MemberId",
                        column: x => x.MemberId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BudgetMembers_Budgets_BudgetId",
                        column: x => x.BudgetId,
                        principalTable: "Budgets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-id",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEN4BnTUB+PLSTsx8kcG1NEc7uiU72118n1zI1TaRv3+z5lthSgJQAY3Y7eHN+0N9uQ==");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetMembers_BudgetId",
                table: "BudgetMembers",
                column: "BudgetId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetMembers_MemberId",
                table: "BudgetMembers",
                column: "MemberId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BudgetMembers");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-id",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEMVMwTVYAKkOAnk1NnkYcyYmya+fX8+nqZb17JzjqoLS1DyiRorduwPhQ3iRfSZSww==");
        }
    }
}
