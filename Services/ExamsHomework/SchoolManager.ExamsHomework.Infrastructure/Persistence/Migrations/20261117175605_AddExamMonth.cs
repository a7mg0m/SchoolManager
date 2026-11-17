using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.ExamsHomework.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExamMonth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Month",
                table: "Exams",
                type: "int",
                nullable: true);

            // Existing Monthly exams (ExamType = 2): take the month from the exam date
            migrationBuilder.Sql("UPDATE Exams SET Month = MONTH(ExamDate) WHERE ExamType = 2 AND Month IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Month",
                table: "Exams");
        }
    }
}
