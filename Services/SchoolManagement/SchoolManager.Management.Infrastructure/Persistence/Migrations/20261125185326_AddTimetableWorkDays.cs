using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Management.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTimetableWorkDays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeOnly>(
                name: "DayStartTime",
                table: "Timetables",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkDays",
                table: "Timetables",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DayStartTime",
                table: "Timetables");

            migrationBuilder.DropColumn(
                name: "WorkDays",
                table: "Timetables");
        }
    }
}
