using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedAttendanceReportMenuItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "MenuItems",
                columns: new[] { "Id", "Icon", "IsActive", "Order", "ParentId", "Route", "CreatedBy", "CreatedOn", "UpdatedBy", "UpdatedOn", "NameAr", "NameEn" },
                values: new object[] { new Guid("6d9f1c00-0000-4000-8000-000000010004"), null, true, 4, new Guid("6d9f1c00-0000-4000-8000-000000000010"), "/reports/attendance", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "تقرير حضور الطلاب", "Attendance Report" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000010004"));
        }
    }
}
