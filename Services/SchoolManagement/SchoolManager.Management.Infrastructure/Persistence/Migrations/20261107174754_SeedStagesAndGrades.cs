using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SchoolManager.Management.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedStagesAndGrades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Stages",
                columns: new[] { "Id", "IsActive", "CreatedBy", "CreatedOn", "UpdatedBy", "UpdatedOn", "NameAr", "NameEn" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-000000000001"), true, new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "مرحلة الحضانة", "Nursery Stage" },
                    { new Guid("11111111-1111-1111-1111-000000000002"), true, new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "المرحلة الابتدائية", "Primary Stage" },
                    { new Guid("11111111-1111-1111-1111-000000000003"), true, new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "المرحلة الإعدادية", "Preparatory Stage" },
                    { new Guid("11111111-1111-1111-1111-000000000004"), true, new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "المرحلة الثانوية", "Secondary Stage" }
                });

            migrationBuilder.InsertData(
                table: "Grades",
                columns: new[] { "Id", "IsActive", "StageId", "CreatedBy", "CreatedOn", "UpdatedBy", "UpdatedOn", "NameAr", "NameEn" },
                values: new object[,]
                {
                    { new Guid("22222222-2222-2222-2222-000000000001"), true, new Guid("11111111-1111-1111-1111-000000000001"), new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "KG 1", "KG 1" },
                    { new Guid("22222222-2222-2222-2222-000000000002"), true, new Guid("11111111-1111-1111-1111-000000000001"), new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "KG 2", "KG 2" },
                    { new Guid("22222222-2222-2222-2222-000000000003"), true, new Guid("11111111-1111-1111-1111-000000000002"), new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "الصف الأول", "Grade 1" },
                    { new Guid("22222222-2222-2222-2222-000000000004"), true, new Guid("11111111-1111-1111-1111-000000000002"), new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "الصف الثاني", "Grade 2" },
                    { new Guid("22222222-2222-2222-2222-000000000005"), true, new Guid("11111111-1111-1111-1111-000000000002"), new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "الصف الثالث", "Grade 3" },
                    { new Guid("22222222-2222-2222-2222-000000000006"), true, new Guid("11111111-1111-1111-1111-000000000002"), new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "الصف الرابع", "Grade 4" },
                    { new Guid("22222222-2222-2222-2222-000000000007"), true, new Guid("11111111-1111-1111-1111-000000000002"), new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "الصف الخامس", "Grade 5" },
                    { new Guid("22222222-2222-2222-2222-000000000008"), true, new Guid("11111111-1111-1111-1111-000000000002"), new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "الصف السادس", "Grade 6" },
                    { new Guid("22222222-2222-2222-2222-000000000009"), true, new Guid("11111111-1111-1111-1111-000000000003"), new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "الصف الأول", "Grade 7" },
                    { new Guid("22222222-2222-2222-2222-000000000010"), true, new Guid("11111111-1111-1111-1111-000000000003"), new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "الصف الثاني", "Grade 8" },
                    { new Guid("22222222-2222-2222-2222-000000000011"), true, new Guid("11111111-1111-1111-1111-000000000003"), new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "الصف الثالث", "Grade 9" },
                    { new Guid("22222222-2222-2222-2222-000000000012"), true, new Guid("11111111-1111-1111-1111-000000000004"), new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "الصف الأول", "Grade 10" },
                    { new Guid("22222222-2222-2222-2222-000000000013"), true, new Guid("11111111-1111-1111-1111-000000000004"), new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "الصف الثاني", "Grade 11" },
                    { new Guid("22222222-2222-2222-2222-000000000014"), true, new Guid("11111111-1111-1111-1111-000000000004"), new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "الصف الثالث", "Grade 12" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-000000000001"));

            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-000000000002"));

            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-000000000003"));

            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-000000000004"));

            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-000000000005"));

            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-000000000006"));

            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-000000000007"));

            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-000000000008"));

            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-000000000009"));

            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-000000000010"));

            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-000000000011"));

            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-000000000012"));

            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-000000000013"));

            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-000000000014"));

            migrationBuilder.DeleteData(
                table: "Stages",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-000000000001"));

            migrationBuilder.DeleteData(
                table: "Stages",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-000000000002"));

            migrationBuilder.DeleteData(
                table: "Stages",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-000000000003"));

            migrationBuilder.DeleteData(
                table: "Stages",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-000000000004"));
        }
    }
}
