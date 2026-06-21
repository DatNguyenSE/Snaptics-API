using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PendingItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Keyword = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NormalizedKeyword = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HitCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingItems", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PendingItems");
        }
    }
}
