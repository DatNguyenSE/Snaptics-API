using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePasswordAdmin2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { "Id", "AccessFailedCount", "ConcurrencyStamp", "CreatedAt", "DefaultReminderTime", "DisplayName", "Email", "EmailConfirmed", "ImageUrl", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "RefreshToken", "RefreshTokenExpiry", "SecurityStamp", "Status", "TrackCalories", "TwoFactorEnabled", "UserName" },
                values: new object[] { "admin-id", 0, "STATIC-GUID-CON-67890", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 20, 0, 0, 0), null, "admin@gmail.com", true, null, false, null, null, "ADMIN", "AQAAAAIAAYagAAAAEA1zCUIVaVWFe1e1VFtSqlS188Re/1PhHgqfPgXgC6ZNX5TNllTzJZv7y/cGtjL3Yg==", null, false, null, null, "STATIC-GUID-SEC-12345", "Active", false, false, "admin" });

            migrationBuilder.InsertData(
                table: "AspNetUserRoles",
                columns: new[] { "RoleId", "UserId" },
                values: new object[] { "admin-id", "admin-id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "admin-id", "admin-id" });

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-id");
        }
    }
}
