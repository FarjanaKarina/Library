using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineLibrary.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovedAtToMembership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "Memberships",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "Memberships");
        }
    }
}
