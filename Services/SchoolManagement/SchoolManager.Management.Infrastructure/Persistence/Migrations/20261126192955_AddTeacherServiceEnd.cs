using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Management.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTeacherServiceEnd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsServiceEnded",
                table: "Teachers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ServiceEndDate",
                table: "Teachers",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsServiceEnded",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "ServiceEndDate",
                table: "Teachers");
        }
    }
}
