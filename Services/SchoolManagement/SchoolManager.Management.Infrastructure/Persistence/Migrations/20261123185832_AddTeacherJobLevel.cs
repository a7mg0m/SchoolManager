using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Management.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTeacherJobLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JobLevel",
                table: "Teachers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JobLevel",
                table: "Teachers");
        }
    }
}
