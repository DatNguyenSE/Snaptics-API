using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldNormalizedEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-id",
                columns: new[] { "NormalizedEmail", "PasswordHash" },
                values: new object[] { "ADMIN@GMAIL.COM", "AQAAAAIAAYagAAAAEG401nKwBB5UH/vbQ8uY4S8pBeqM84rMSnuDBJGnX8xK6SVy6A5lHwoxtbfMXf3W9g==" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-id",
                columns: new[] { "NormalizedEmail", "PasswordHash" },
                values: new object[] { null, "AQAAAAIAAYagAAAAEFhH9Gf6K+Q4Scz7LGtgUWvg+ctEpuBPa/DNcJyrp9aYDk11pl/lhRKFhUhcC45qFw==" });
        }
    }
}
