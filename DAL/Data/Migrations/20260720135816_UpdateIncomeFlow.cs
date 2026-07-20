using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateIncomeFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IncomeHistories_IncomeSources_IncomeSourceId",
                table: "IncomeHistories");

            migrationBuilder.DropColumn(
                name: "Frequency",
                table: "IncomeSources");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "IncomeSources");

            migrationBuilder.AlterColumn<int>(
                name: "IncomeSourceId",
                table: "IncomeHistories",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "BudgetId",
                table: "IncomeHistories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-id",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEH/7FT89KP93qnqwh7xjnB4nkBw34UBcpB4ACPPzJIl2+R1Cjdt+r0szH/S4hFaCBQ==");

            migrationBuilder.CreateIndex(
                name: "IX_IncomeHistories_BudgetId",
                table: "IncomeHistories",
                column: "BudgetId");

            migrationBuilder.AddForeignKey(
                name: "FK_IncomeHistories_Budgets_BudgetId",
                table: "IncomeHistories",
                column: "BudgetId",
                principalTable: "Budgets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IncomeHistories_IncomeSources_IncomeSourceId",
                table: "IncomeHistories",
                column: "IncomeSourceId",
                principalTable: "IncomeSources",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IncomeHistories_Budgets_BudgetId",
                table: "IncomeHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_IncomeHistories_IncomeSources_IncomeSourceId",
                table: "IncomeHistories");

            migrationBuilder.DropIndex(
                name: "IX_IncomeHistories_BudgetId",
                table: "IncomeHistories");

            migrationBuilder.DropColumn(
                name: "BudgetId",
                table: "IncomeHistories");

            migrationBuilder.AddColumn<int>(
                name: "Frequency",
                table: "IncomeSources",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "IncomeSources",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "IncomeSourceId",
                table: "IncomeHistories",
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
                value: "AQAAAAIAAYagAAAAEJS+jdnpb7z43LBwUQ0TtB9JluFZu2Cqsrgnv8Ooe0JuRy9yyxj49/IoxCvs/VjDfQ==");

            migrationBuilder.AddForeignKey(
                name: "FK_IncomeHistories_IncomeSources_IncomeSourceId",
                table: "IncomeHistories",
                column: "IncomeSourceId",
                principalTable: "IncomeSources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
