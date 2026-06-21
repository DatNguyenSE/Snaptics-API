using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUnitToTransactionDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "TransactionDetails",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Unit",
                table: "TransactionDetails");
        }
    }
}
