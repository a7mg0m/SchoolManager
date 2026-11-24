using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBusDriverAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BusId",
                table: "UserProfiles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Description", "NameArabic", "NameEnglish" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000006"), "Bus driver", "سائق", "Driver" });

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_BusId",
                table: "UserProfiles",
                column: "BusId",
                unique: true,
                filter: "[BusId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_BusId",
                table: "UserProfiles");

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000006"));

            migrationBuilder.DropColumn(
                name: "BusId",
                table: "UserProfiles");
        }
    }
}
