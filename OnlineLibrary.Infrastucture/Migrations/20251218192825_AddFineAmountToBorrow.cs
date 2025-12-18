using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineLibrary.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFineAmountToBorrow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "FineAmount",
                table: "BorrowTransactions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FineAmount",
                table: "BorrowTransactions");
        }
    }
}
