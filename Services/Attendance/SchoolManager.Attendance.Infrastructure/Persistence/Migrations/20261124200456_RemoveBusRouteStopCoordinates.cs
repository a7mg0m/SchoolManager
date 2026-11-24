using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Attendance.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBusRouteStopCoordinates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "BusRouteStops");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "BusRouteStops");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "BusRouteStops",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "BusRouteStops",
                type: "float",
                nullable: true);
        }
    }
}
