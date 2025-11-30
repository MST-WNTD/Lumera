// FILE: AddConversationPersistence Migration (CORRECTED)
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lumera.Migrations
{
    public partial class AddConversationPersistence : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Drop the old foreign key constraint first
            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_Events_EventID",
                table: "Conversations");

            // Step 2: Add new columns
            migrationBuilder.AddColumn<int>(
                name: "ClientID",
                table: "Conversations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrganizerID",
                table: "Conversations",
                type: "int",
                nullable: true);

            // Step 3: Create indexes for new columns
            migrationBuilder.CreateIndex(
                name: "IX_Conversations_ClientID",
                table: "Conversations",
                column: "ClientID");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_OrganizerID",
                table: "Conversations",
                column: "OrganizerID");

            // Step 4: Add new foreign key constraints
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

            // Step 5: Re-add Event foreign key with SetNull behavior
            migrationBuilder.AddForeignKey(
                name: "FK_Conversations_Events_EventID",
                table: "Conversations",
                column: "EventID",
                principalTable: "Events",
                principalColumn: "EventID",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse all changes
            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_Events_EventID",
                table: "Conversations");

            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_Clients_ClientID",
                table: "Conversations");

            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_Organizers_OrganizerID",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_ClientID",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_OrganizerID",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "ClientID",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "OrganizerID",
                table: "Conversations");

            // Restore original constraint
            migrationBuilder.AddForeignKey(
                name: "FK_Conversations_Events_EventID",
                table: "Conversations",
                column: "EventID",
                principalTable: "Events",
                principalColumn: "EventID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}