using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Notification.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationTargetBus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BusId",
                table: "NotificationTargets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationTargets_BusId",
                table: "NotificationTargets",
                column: "BusId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NotificationTargets_BusId",
                table: "NotificationTargets");

            migrationBuilder.DropColumn(
                name: "BusId",
                table: "NotificationTargets");
        }
    }
}
