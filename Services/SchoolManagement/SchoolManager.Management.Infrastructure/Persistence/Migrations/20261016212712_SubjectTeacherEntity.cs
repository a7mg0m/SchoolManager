using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Management.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SubjectTeacherEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubjectTeachers_Subjects_SubjectsId",
                table: "SubjectTeachers");

            migrationBuilder.DropForeignKey(
                name: "FK_SubjectTeachers_Teachers_TeachersId",
                table: "SubjectTeachers");

            migrationBuilder.RenameColumn(
                name: "TeachersId",
                table: "SubjectTeachers",
                newName: "TeacherId");

            migrationBuilder.RenameColumn(
                name: "SubjectsId",
                table: "SubjectTeachers",
                newName: "SubjectId");

            migrationBuilder.RenameIndex(
                name: "IX_SubjectTeachers_TeachersId",
                table: "SubjectTeachers",
                newName: "IX_SubjectTeachers_TeacherId");

            migrationBuilder.AddForeignKey(
                name: "FK_SubjectTeachers_Subjects_SubjectId",
                table: "SubjectTeachers",
                column: "SubjectId",
                principalTable: "Subjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SubjectTeachers_Teachers_TeacherId",
                table: "SubjectTeachers",
                column: "TeacherId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubjectTeachers_Subjects_SubjectId",
                table: "SubjectTeachers");

            migrationBuilder.DropForeignKey(
                name: "FK_SubjectTeachers_Teachers_TeacherId",
                table: "SubjectTeachers");

            migrationBuilder.RenameColumn(
                name: "TeacherId",
                table: "SubjectTeachers",
                newName: "TeachersId");

            migrationBuilder.RenameColumn(
                name: "SubjectId",
                table: "SubjectTeachers",
                newName: "SubjectsId");

            migrationBuilder.RenameIndex(
                name: "IX_SubjectTeachers_TeacherId",
                table: "SubjectTeachers",
                newName: "IX_SubjectTeachers_TeachersId");

            migrationBuilder.AddForeignKey(
                name: "FK_SubjectTeachers_Subjects_SubjectsId",
                table: "SubjectTeachers",
                column: "SubjectsId",
                principalTable: "Subjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SubjectTeachers_Teachers_TeachersId",
                table: "SubjectTeachers",
                column: "TeachersId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
