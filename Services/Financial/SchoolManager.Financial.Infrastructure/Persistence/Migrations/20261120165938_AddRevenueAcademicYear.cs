using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Financial.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRevenueAcademicYear : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AcademicYearId",
                table: "Revenues",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Revenues_AcademicYearId",
                table: "Revenues",
                column: "AcademicYearId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Revenues_AcademicYearId",
                table: "Revenues");

            migrationBuilder.DropColumn(
                name: "AcademicYearId",
                table: "Revenues");
        }
    }
}
