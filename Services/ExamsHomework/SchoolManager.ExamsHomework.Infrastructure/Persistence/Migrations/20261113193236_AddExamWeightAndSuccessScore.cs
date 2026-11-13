using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.ExamsHomework.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExamWeightAndSuccessScore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "SuccessScore",
                table: "Exams",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Weight",
                table: "Exams",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            // Existing exams: full mark 100 (or the highest saved score if higher), pass at half
            migrationBuilder.Sql(@"
UPDATE e
SET e.Weight = CASE WHEN ISNULL(r.MaxScore, 0) > 100 THEN r.MaxScore ELSE 100 END
FROM Exams e
LEFT JOIN (SELECT ExamId, MAX(Score) AS MaxScore FROM ExamResults GROUP BY ExamId) r ON r.ExamId = e.Id;

UPDATE Exams SET SuccessScore = Weight / 2;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SuccessScore",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "Weight",
                table: "Exams");
        }
    }
}
