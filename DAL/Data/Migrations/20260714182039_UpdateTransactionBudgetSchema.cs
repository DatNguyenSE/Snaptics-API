using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTransactionBudgetSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsExpense",
                table: "Transactions",
                type: "bit",
                nullable: false,
                defaultValue: true);

            // Data Migration: Cập nhật IsExpense dựa trên CategoryType cũ (1 = Income, 0 = Expense)
            migrationBuilder.Sql(@"
                UPDATE Transactions
                SET IsExpense = 
                    CASE 
                        WHEN EXISTS (
                            SELECT 1 FROM TransactionDetails td 
                            JOIN Categories c ON td.CategoryId = c.Id 
                            WHERE td.TransactionId = Transactions.Id AND c.Type = 1
                        ) THEN 0
                        ELSE 1
                    END;
            ");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Categories");

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Budgets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-id",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEIuRxyUcBqUF+ckxm38AuaigNOb3qF9hHGTVP9nnjh8Qyn6AUGBIbbv9sCQ0b+sQvw==");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsExpense",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Budgets");

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Categories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-id",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAENizfqLk4a3uNx28FmCqdVu9IKAnfzOXmkB17WcRlAjg8tO4zXFiqpa8w1phj6n8vw==");
        }
    }
}
