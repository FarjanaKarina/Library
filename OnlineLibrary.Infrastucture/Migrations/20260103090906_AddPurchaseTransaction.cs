using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineLibrary.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Table already exists - skip creation
            // migrationBuilder.CreateTable(
            //     name: "PurchaseTransactions",
            //     columns: table => new
            //     {
            //         PurchaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            //         BookId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            //         UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            //         Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
            //         PurchaseDate = table.Column<DateTime>(type: "datetime2", nullable: false),
            //         TransactionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //         SessionKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //         PaymentStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
            //         BankTransactionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //         CardType = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //         PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: true)
            //     },
            //     constraints: table =>
            //     {
            //         table.PrimaryKey("PK_PurchaseTransactions", x => x.PurchaseId);
            //     });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PurchaseTransactions");
        }
    }
}
