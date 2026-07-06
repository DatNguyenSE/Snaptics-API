using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSeedingAdminAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { "Id", "AccessFailedCount", "ConcurrencyStamp", "CreatedAt", "DefaultReminderTime", "DisplayName", "Email", "EmailConfirmed", "ImageUrl", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "RefreshToken", "RefreshTokenExpiry", "SecurityStamp", "Status", "TrackCalories", "TwoFactorEnabled", "UserName" },
                values: new object[] { "admin-id", 0, "STATIC-GUID-CON-67890", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 20, 0, 0, 0), null, "admin@gmail.com", true, null, false, null, null, "ADMIN", "AQAAAAIAAYagAAAAELuWf8X8+7J8J8+J8+J8+J8+J8+J8+J8+J8+J8+J8+J8+J8+A==", null, false, null, null, "STATIC-GUID-SEC-12345", "Active", false, false, "admin" });

            migrationBuilder.InsertData(
                table: "AspNetUserRoles",
                columns: new[] { "RoleId", "UserId" },
                values: new object[] { "admin-id", "admin-id" });
        }
    }
}
