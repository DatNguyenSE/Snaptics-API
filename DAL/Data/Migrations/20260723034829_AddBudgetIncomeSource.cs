using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBudgetIncomeSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IncomeSources_Budgets_BudgetId",
                table: "IncomeSources");

            migrationBuilder.AlterColumn<int>(
                name: "BudgetId",
                table: "IncomeSources",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "BudgetIncomeSources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BudgetId = table.Column<int>(type: "int", nullable: false),
                    IncomeSourceId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetIncomeSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BudgetIncomeSources_Budgets_BudgetId",
                        column: x => x.BudgetId,
                        principalTable: "Budgets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BudgetIncomeSources_IncomeSources_IncomeSourceId",
                        column: x => x.IncomeSourceId,
                        principalTable: "IncomeSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-id",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEIqSbq21fIhPcK9456vGwkMUIJUD6OKff1pJ/JHK0yjJv2Z7Wfjg2k3SgBonLbc05g==");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetIncomeSources_BudgetId",
                table: "BudgetIncomeSources",
                column: "BudgetId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetIncomeSources_IncomeSourceId",
                table: "BudgetIncomeSources",
                column: "IncomeSourceId");

            migrationBuilder.AddForeignKey(
                name: "FK_IncomeSources_Budgets_BudgetId",
                table: "IncomeSources",
                column: "BudgetId",
                principalTable: "Budgets",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IncomeSources_Budgets_BudgetId",
                table: "IncomeSources");

            migrationBuilder.DropTable(
                name: "BudgetIncomeSources");

            migrationBuilder.AlterColumn<int>(
                name: "BudgetId",
                table: "IncomeSources",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-id",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEN4BnTUB+PLSTsx8kcG1NEc7uiU72118n1zI1TaRv3+z5lthSgJQAY3Y7eHN+0N9uQ==");

            migrationBuilder.AddForeignKey(
                name: "FK_IncomeSources_Budgets_BudgetId",
                table: "IncomeSources",
                column: "BudgetId",
                principalTable: "Budgets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
