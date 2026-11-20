using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Financial.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFeesAcademicYear : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AcademicYearId",
                table: "StudentFees",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AcademicYearId",
                table: "Fees",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcademicYearId",
                table: "StudentFees");

            migrationBuilder.DropColumn(
                name: "AcademicYearId",
                table: "Fees");
        }
    }
}
