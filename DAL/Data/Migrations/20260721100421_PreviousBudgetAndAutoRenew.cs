using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Data.Migrations
{
    /// <inheritdoc />
    public partial class PreviousBudgetAndAutoRenew : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAutoRenew",
                table: "Budgets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PreviousBudgetId",
                table: "Budgets",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-id",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEMVMwTVYAKkOAnk1NnkYcyYmya+fX8+nqZb17JzjqoLS1DyiRorduwPhQ3iRfSZSww==");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAutoRenew",
                table: "Budgets");

            migrationBuilder.DropColumn(
                name: "PreviousBudgetId",
                table: "Budgets");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-id",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEH/7FT89KP93qnqwh7xjnB4nkBw34UBcpB4ACPPzJIl2+R1Cjdt+r0szH/S4hFaCBQ==");
        }
    }
}
