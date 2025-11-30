using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lumera.Migrations
{
    /// <inheritdoc />
    public partial class RemoveReviewFlagging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FlagReason",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "FlaggedAt",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "IsFlagged",
                table: "Reviews");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FlagReason",
                table: "Reviews",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "FlaggedAt",
                table: "Reviews",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFlagged",
                table: "Reviews",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }
    }
}
