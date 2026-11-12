using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Financial.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentFeeWaiver : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WaiveReason",
                table: "StudentFees",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WaivedAmount",
                table: "StudentFees",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "WaivedAt",
                table: "StudentFees",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WaiveReason",
                table: "StudentFees");

            migrationBuilder.DropColumn(
                name: "WaivedAmount",
                table: "StudentFees");

            migrationBuilder.DropColumn(
                name: "WaivedAt",
                table: "StudentFees");
        }
    }
}
