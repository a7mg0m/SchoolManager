using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Notification.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserDeviceTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PushSentAt",
                table: "Notifications",
                type: "datetime2",
                nullable: true);

            // Notifications saved before push existed count as pushed: the push job must not send old ones to every phone
            migrationBuilder.Sql("UPDATE Notifications SET PushSentAt = SYSUTCDATETIME() WHERE PushSentAt IS NULL");

            migrationBuilder.CreateTable(
                name: "UserDeviceTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserType = table.Column<int>(type: "int", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Platform = table.Column<int>(type: "int", nullable: false),
                    AppName = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDeviceTokens", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_PushSentAt_SentOn",
                table: "Notifications",
                columns: new[] { "PushSentAt", "SentOn" });

            migrationBuilder.CreateIndex(
                name: "IX_UserDeviceTokens_Token",
                table: "UserDeviceTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserDeviceTokens_UserId",
                table: "UserDeviceTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDeviceTokens_UserType",
                table: "UserDeviceTokens",
                column: "UserType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserDeviceTokens");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_PushSentAt_SentOn",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "PushSentAt",
                table: "Notifications");
        }
    }
}
