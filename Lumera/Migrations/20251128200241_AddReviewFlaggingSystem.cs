using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lumera.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewFlaggingSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_Clients_ClientID",
                table: "Conversations");

            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_Organizers_OrganizerID",
                table: "Conversations");

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    NotificationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Message = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NotificationType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReferenceID = table.Column<int>(type: "int", nullable: true),
                    ReferenceType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RedirectUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsRead = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.NotificationID);
                    table.ForeignKey(
                        name: "FK_notifications_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_UserID",
                table: "notifications",
                column: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Conversations_Clients_ClientID",
                table: "Conversations",
                column: "ClientID",
                principalTable: "Clients",
                principalColumn: "ClientID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Conversations_Organizers_OrganizerID",
                table: "Conversations",
                column: "OrganizerID",
                principalTable: "Organizers",
                principalColumn: "OrganizerID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_Clients_ClientID",
                table: "Conversations");

            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_Organizers_OrganizerID",
                table: "Conversations");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.AddForeignKey(
                name: "FK_Conversations_Clients_ClientID",
                table: "Conversations",
                column: "ClientID",
                principalTable: "Clients",
                principalColumn: "ClientID");

            migrationBuilder.AddForeignKey(
                name: "FK_Conversations_Organizers_OrganizerID",
                table: "Conversations",
                column: "OrganizerID",
                principalTable: "Organizers",
                principalColumn: "OrganizerID");
        }
    }
}
