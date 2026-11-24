using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Attendance.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBusDriverLogin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DriverLoginName",
                table: "Buses",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Buses_DriverLoginName",
                table: "Buses",
                column: "DriverLoginName",
                unique: true,
                filter: "[DriverLoginName] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Buses_DriverLoginName",
                table: "Buses");

            migrationBuilder.DropColumn(
                name: "DriverLoginName",
                table: "Buses");
        }
    }
}
