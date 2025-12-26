using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineLibrary.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBookCategoryMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BookCategories",
                columns: table => new
                {
                    BookCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookCategories", x => x.BookCategoryId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookCategories");
        }
    }
}
