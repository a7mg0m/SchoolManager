using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SchoolManager.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedMenuItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "MenuItems",
                columns: new[] { "Id", "Icon", "IsActive", "Order", "ParentId", "Route", "CreatedBy", "CreatedOn", "UpdatedBy", "UpdatedOn", "NameAr", "NameEn" },
                values: new object[,]
                {
                    { new Guid("6d9f1c00-0000-4000-8000-000000000001"), "pe-7s-rocket", true, 1, null, "/home", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "الرئيسية", "Home" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000000002"), "pe-7s-note2", true, 2, null, null, new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "الإدارة", "Management" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000000003"), "pe-7s-cash", true, 3, null, null, new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "المالية", "Financial" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000000004"), "pe-7s-bell", true, 4, null, null, new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "الإشعارات", "Notification" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000000005"), "pe-7s-note2", true, 5, null, null, new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "الامتحانات والواجبات", "Exams & Homework" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000000006"), "pe-7s-users", true, 6, null, null, new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "المستخدمون", "Users" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000002001"), null, true, 1, new Guid("6d9f1c00-0000-4000-8000-000000000002"), "/management/school-info", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "بيانات المدرسة", "School Info" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000002002"), null, true, 2, new Guid("6d9f1c00-0000-4000-8000-000000000002"), "/management/stages", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "المراحل", "Stages" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000002003"), null, true, 3, new Guid("6d9f1c00-0000-4000-8000-000000000002"), "/management/grades", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "السنوات الدراسية", "Grades" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000002004"), null, true, 4, new Guid("6d9f1c00-0000-4000-8000-000000000002"), "/management/sections", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "الفصول", "Sections" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000002005"), null, true, 5, new Guid("6d9f1c00-0000-4000-8000-000000000002"), "/management/academic-years", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "السنوات الأكاديمية", "Academic Years" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000002006"), null, true, 6, new Guid("6d9f1c00-0000-4000-8000-000000000002"), "/management/teachers", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "المعلمين", "Teachers" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000002007"), null, true, 7, new Guid("6d9f1c00-0000-4000-8000-000000000002"), "/management/subjects", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "المواد الدراسية", "Subjects" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000002008"), null, true, 8, new Guid("6d9f1c00-0000-4000-8000-000000000002"), "/management/students", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "الطلاب", "Students" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000002009"), null, true, 9, new Guid("6d9f1c00-0000-4000-8000-000000000002"), "/management/parents", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "أولياء الأمور", "Parents" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000002010"), null, true, 10, new Guid("6d9f1c00-0000-4000-8000-000000000002"), "/management/timetables", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "الجداول الدراسية", "Timetables" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000003001"), null, true, 1, new Guid("6d9f1c00-0000-4000-8000-000000000003"), "/financial/fees", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "الرسوم", "Fees" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000003002"), null, true, 2, new Guid("6d9f1c00-0000-4000-8000-000000000003"), "/financial/student-fees", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "رسوم الطلاب", "Student Fees" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000003003"), null, true, 3, new Guid("6d9f1c00-0000-4000-8000-000000000003"), "/financial/expenses", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "المصروفات", "Expenses" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000003004"), null, true, 4, new Guid("6d9f1c00-0000-4000-8000-000000000003"), "/financial/revenues", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "الإيرادات", "Revenues" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000003005"), null, true, 5, new Guid("6d9f1c00-0000-4000-8000-000000000003"), "/financial/expense-categories", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "فئات المصروفات", "Expense Categories" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000003006"), null, true, 6, new Guid("6d9f1c00-0000-4000-8000-000000000003"), "/financial/revenue-categories", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "فئات الإيرادات", "Revenue Categories" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000004001"), null, true, 1, new Guid("6d9f1c00-0000-4000-8000-000000000004"), "/notification/notifications", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "الإشعارات", "Notifications" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000004002"), null, true, 2, new Guid("6d9f1c00-0000-4000-8000-000000000004"), "/notification/school-events", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "فعاليات المدرسة", "School Events" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000005001"), null, true, 1, new Guid("6d9f1c00-0000-4000-8000-000000000005"), "/exam/exams", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "الامتحانات", "Exams" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000006001"), null, true, 1, new Guid("6d9f1c00-0000-4000-8000-000000000006"), "/users/employee-groups", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "مجموعات الموظفين", "Employee Groups" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000006002"), null, true, 2, new Guid("6d9f1c00-0000-4000-8000-000000000006"), "/users/employees", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "الموظفون", "Employees" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000000001"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000002001"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000002002"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000002003"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000002004"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000002005"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000002006"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000002007"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000002008"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000002009"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000002010"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000003001"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000003002"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000003003"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000003004"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000003005"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000003006"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000004001"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000004002"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000005001"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000006001"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000006002"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000000002"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000000003"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000000004"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000000005"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000000006"));
        }
    }
}
