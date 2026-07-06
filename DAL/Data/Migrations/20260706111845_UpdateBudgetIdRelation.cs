using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBudgetIdRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-id",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAENizfqLk4a3uNx28FmCqdVu9IKAnfzOXmkB17WcRlAjg8tO4zXFiqpa8w1phj6n8vw==");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-id",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEFWEvuU7Mqlh+ZL5YoPzzCdZl4CSxjzl93QsB5jipfVaO6W74FycgyZ7z9TJ7VbUFQ==");
        }
    }
}
