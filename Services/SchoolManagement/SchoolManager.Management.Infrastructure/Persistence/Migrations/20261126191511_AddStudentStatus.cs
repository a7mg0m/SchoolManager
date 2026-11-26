using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Management.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Students",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateOnly>(
                name: "StatusDate",
                table: "Students",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StatusNote",
                table: "Students",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "StatusDate",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "StatusNote",
                table: "Students");
        }
    }
}
