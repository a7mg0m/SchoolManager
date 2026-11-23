using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SchoolManager.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedAttendanceTransportMenuItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "MenuItems",
                columns: new[] { "Id", "Icon", "IsActive", "Order", "ParentId", "Route", "CreatedBy", "CreatedOn", "UpdatedBy", "UpdatedOn", "NameAr", "NameEn" },
                values: new object[,]
                {
                    { new Guid("6d9f1c00-0000-4000-8000-000000000008"), "pe-7s-check", true, 8, null, null, new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "الحضور والغياب", "Attendance" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000000009"), "pe-7s-car", true, 9, null, null, new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "النقل المدرسي", "School Transport" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000008001"), null, true, 1, new Guid("6d9f1c00-0000-4000-8000-000000000008"), "/attendance/daily", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "الحضور اليومي", "Daily Attendance" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000008002"), null, true, 2, new Guid("6d9f1c00-0000-4000-8000-000000000008"), "/attendance/evaluations", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "تقييمات الطلاب", "Student Evaluations" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000009001"), null, true, 1, new Guid("6d9f1c00-0000-4000-8000-000000000009"), "/transport/routes", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "خطوط السير", "Bus Routes" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000009002"), null, true, 2, new Guid("6d9f1c00-0000-4000-8000-000000000009"), "/transport/buses", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "الباصات", "Buses" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000009003"), null, true, 3, new Guid("6d9f1c00-0000-4000-8000-000000000009"), "/transport/assignments", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "توزيع الطلاب على الباصات", "Bus Assignments" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000009004"), null, true, 4, new Guid("6d9f1c00-0000-4000-8000-000000000009"), "/transport/trips", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "الرحلات", "Trips" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000008001"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000008002"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000009001"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000009002"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000009003"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000009004"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000000008"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000000009"));
        }
    }
}
