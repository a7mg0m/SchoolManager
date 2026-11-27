using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Attendance.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReportDateIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_StudentEvaluations_Date",
                table: "StudentEvaluations",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_DailyAttendances_Date",
                table: "DailyAttendances",
                column: "Date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StudentEvaluations_Date",
                table: "StudentEvaluations");

            migrationBuilder.DropIndex(
                name: "IX_DailyAttendances_Date",
                table: "DailyAttendances");
        }
    }
}
