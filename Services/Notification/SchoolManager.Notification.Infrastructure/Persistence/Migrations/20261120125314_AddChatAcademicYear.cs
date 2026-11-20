using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Notification.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChatAcademicYear : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PrivateKey",
                table: "ChatRooms",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(80)",
                oldMaxLength: 80,
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AcademicYearId",
                table: "ChatRooms",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatRooms_AcademicYearId",
                table: "ChatRooms",
                column: "AcademicYearId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ChatRooms_AcademicYearId",
                table: "ChatRooms");

            migrationBuilder.DropColumn(
                name: "AcademicYearId",
                table: "ChatRooms");

            migrationBuilder.AlterColumn<string>(
                name: "PrivateKey",
                table: "ChatRooms",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(120)",
                oldMaxLength: 120,
                oldNullable: true);
        }
    }
}
