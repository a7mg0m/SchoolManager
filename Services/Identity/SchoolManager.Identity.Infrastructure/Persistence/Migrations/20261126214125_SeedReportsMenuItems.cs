using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SchoolManager.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedReportsMenuItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "MenuItems",
                columns: new[] { "Id", "Icon", "IsActive", "Order", "ParentId", "Route", "CreatedBy", "CreatedOn", "UpdatedBy", "UpdatedOn", "NameAr", "NameEn" },
                values: new object[,]
                {
                    { new Guid("6d9f1c00-0000-4000-8000-000000000010"), "pe-7s-graph2", true, 10, null, null, new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "التقارير", "Reports" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000010001"), null, true, 1, new Guid("6d9f1c00-0000-4000-8000-000000000010"), "/reports/students-status", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "الطلاب حسب الحالة", "Students by Status" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000010001"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000000010"));
        }
    }
}
