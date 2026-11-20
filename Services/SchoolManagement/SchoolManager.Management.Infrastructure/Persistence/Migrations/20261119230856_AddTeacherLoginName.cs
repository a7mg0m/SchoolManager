using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Management.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTeacherLoginName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LoginName",
                table: "Teachers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            // Teachers saved before: their login (Identity) uses the National ID as login name
            migrationBuilder.Sql(@"
                UPDATE t SET t.LoginName = LEFT(t.NationalId, 100)
                FROM Teachers t
                WHERE t.LoginName IS NULL AND LEN(t.NationalId) >= 3
                  AND NOT EXISTS (SELECT 1 FROM Parents p WHERE p.LoginName = t.NationalId);");

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_LoginName",
                table: "Teachers",
                column: "LoginName",
                unique: true,
                filter: "[LoginName] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Teachers_LoginName",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "LoginName",
                table: "Teachers");
        }
    }
}
