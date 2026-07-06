using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBudgetAndTransactionFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BudgetId",
                table: "Transactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentAmount",
                table: "Budgets",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "Budgets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Budgets",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-id",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEFWEvuU7Mqlh+ZL5YoPzzCdZl4CSxjzl93QsB5jipfVaO6W74FycgyZ7z9TJ7VbUFQ==");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_BudgetId",
                table: "Transactions",
                column: "BudgetId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Budgets_BudgetId",
                table: "Transactions",
                column: "BudgetId",
                principalTable: "Budgets",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Budgets_BudgetId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_BudgetId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "BudgetId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "CurrentAmount",
                table: "Budgets");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "Budgets");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Budgets");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-id",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEG401nKwBB5UH/vbQ8uY4S8pBeqM84rMSnuDBJGnX8xK6SVy6A5lHwoxtbfMXf3W9g==");
        }
    }
}
