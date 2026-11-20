using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SchoolManager.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedClinicMenuItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "MenuItems",
                columns: new[] { "Id", "Icon", "IsActive", "Order", "ParentId", "Route", "CreatedBy", "CreatedOn", "UpdatedBy", "UpdatedOn", "NameAr", "NameEn" },
                values: new object[,]
                {
                    { new Guid("6d9f1c00-0000-4000-8000-000000000007"), "pe-7s-like2", true, 7, null, null, new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "العيادة المدرسية", "School Clinic" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000007001"), null, true, 1, new Guid("6d9f1c00-0000-4000-8000-000000000007"), "/clinic/students", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "الملفات الصحية للطلاب", "Student Health Files" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000007002"), null, true, 2, new Guid("6d9f1c00-0000-4000-8000-000000000007"), "/clinic/visits", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "زيارات العيادة", "Clinic Visits" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000007003"), null, true, 3, new Guid("6d9f1c00-0000-4000-8000-000000000007"), "/clinic/vaccines", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "التطعيمات", "Vaccines" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000007004"), null, true, 4, new Guid("6d9f1c00-0000-4000-8000-000000000007"), "/clinic/vaccination-campaigns", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "حملات التطعيم", "Vaccination Campaigns" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000007001"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000007002"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000007003"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000007004"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000000007"));
        }
    }
}
