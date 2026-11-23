using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Financial.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentFeeBusAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BusAssignmentId",
                table: "StudentFees",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentFees_BusAssignmentId",
                table: "StudentFees",
                column: "BusAssignmentId",
                unique: true,
                filter: "[BusAssignmentId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StudentFees_BusAssignmentId",
                table: "StudentFees");

            migrationBuilder.DropColumn(
                name: "BusAssignmentId",
                table: "StudentFees");
        }
    }
}
