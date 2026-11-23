using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Management.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Students",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Students",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Students");
        }
    }
}
