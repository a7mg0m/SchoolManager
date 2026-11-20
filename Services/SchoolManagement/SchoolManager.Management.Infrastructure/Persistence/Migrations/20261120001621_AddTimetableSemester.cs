using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Management.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTimetableSemester : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SemesterId",
                table: "Timetables",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Timetables_SemesterId",
                table: "Timetables",
                column: "SemesterId");

            migrationBuilder.AddForeignKey(
                name: "FK_Timetables_Semesters_SemesterId",
                table: "Timetables",
                column: "SemesterId",
                principalTable: "Semesters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Timetables_Semesters_SemesterId",
                table: "Timetables");

            migrationBuilder.DropIndex(
                name: "IX_Timetables_SemesterId",
                table: "Timetables");

            migrationBuilder.DropColumn(
                name: "SemesterId",
                table: "Timetables");
        }
    }
}
