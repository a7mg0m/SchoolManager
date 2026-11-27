using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SchoolManager.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MenuReorder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000002011"));

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
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000006003"));

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
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000009003"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000009004"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000010005"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000010006"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000010007"));

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000000002"),
                column: "Order",
                value: 3);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000000003"),
                column: "Order",
                value: 5);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000000004"),
                column: "Order",
                value: 6);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000000005"),
                column: "Order",
                value: 7);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000000006"),
                column: "Order",
                value: 2);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000000011"),
                column: "Order",
                value: 4);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000002001"),
                columns: new[] { "ParentId", "Route", "NameAr", "NameEn" },
                values: new object[] { new Guid("6d9f1c00-0000-4000-8000-000000000006"), "/users/employee-groups", "مجموعات الموظفين", "Employee Groups" });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000002002"),
                columns: new[] { "ParentId", "Route", "NameAr", "NameEn" },
                values: new object[] { new Guid("6d9f1c00-0000-4000-8000-000000000006"), "/users/employees", "الموظفون", "Employees" });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000002003"),
                columns: new[] { "ParentId", "Route", "NameAr", "NameEn" },
                values: new object[] { new Guid("6d9f1c00-0000-4000-8000-000000000006"), "/users/group-permissions", "صلاحيات المجموعات", "Group Permissions" });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000003001"),
                columns: new[] { "ParentId", "Route", "NameAr", "NameEn" },
                values: new object[] { new Guid("6d9f1c00-0000-4000-8000-000000000002"), "/management/school-info", "بيانات المدرسة", "School Info" });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000003002"),
                columns: new[] { "ParentId", "Route", "NameAr", "NameEn" },
                values: new object[] { new Guid("6d9f1c00-0000-4000-8000-000000000002"), "/management/stages", "المراحل", "Stages" });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000003003"),
                columns: new[] { "ParentId", "Route", "NameAr", "NameEn" },
                values: new object[] { new Guid("6d9f1c00-0000-4000-8000-000000000002"), "/management/grades", "السنوات الدراسية", "Grades" });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000003004"),
                columns: new[] { "ParentId", "Route", "NameAr", "NameEn" },
                values: new object[] { new Guid("6d9f1c00-0000-4000-8000-000000000002"), "/management/sections", "الفصول", "Sections" });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000003005"),
                columns: new[] { "ParentId", "Route", "NameAr", "NameEn" },
                values: new object[] { new Guid("6d9f1c00-0000-4000-8000-000000000002"), "/management/academic-years", "السنوات الأكاديمية", "Academic Years" });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000003006"),
                columns: new[] { "ParentId", "Route", "NameAr", "NameEn" },
                values: new object[] { new Guid("6d9f1c00-0000-4000-8000-000000000002"), "/management/teachers", "المعلمين", "Teachers" });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000005001"),
                columns: new[] { "ParentId", "Route", "NameAr", "NameEn" },
                values: new object[] { new Guid("6d9f1c00-0000-4000-8000-000000000003"), "/financial/revenue-categories", "فئات الإيرادات", "Revenue Categories" });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000005002"),
                columns: new[] { "ParentId", "Route", "NameAr", "NameEn" },
                values: new object[] { new Guid("6d9f1c00-0000-4000-8000-000000000003"), "/financial/expense-categories", "فئات المصروفات", "Expense Categories" });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000006001"),
                columns: new[] { "ParentId", "Route", "NameAr", "NameEn" },
                values: new object[] { new Guid("6d9f1c00-0000-4000-8000-000000000004"), "/notification/notifications", "الإشعارات", "Notifications" });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000006002"),
                columns: new[] { "ParentId", "Route", "NameAr", "NameEn" },
                values: new object[] { new Guid("6d9f1c00-0000-4000-8000-000000000004"), "/notification/school-events", "فعاليات المدرسة", "School Events" });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000007001"),
                columns: new[] { "ParentId", "Route", "NameAr", "NameEn" },
                values: new object[] { new Guid("6d9f1c00-0000-4000-8000-000000000005"), "/exam/exams", "الامتحانات", "Exams" });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000007002"),
                columns: new[] { "ParentId", "Route", "NameAr", "NameEn" },
                values: new object[] { new Guid("6d9f1c00-0000-4000-8000-000000000005"), "/exam/promotion", "اعتماد النتائج والترفيع", "Results Approval & Promotion" });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000008001"),
                columns: new[] { "ParentId", "Route", "NameAr", "NameEn" },
                values: new object[] { new Guid("6d9f1c00-0000-4000-8000-000000000007"), "/clinic/students", "الملفات الصحية للطلاب", "Student Health Files" });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000008002"),
                columns: new[] { "ParentId", "Route", "NameAr", "NameEn" },
                values: new object[] { new Guid("6d9f1c00-0000-4000-8000-000000000007"), "/clinic/visits", "زيارات العيادة", "Clinic Visits" });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000009001"),
                columns: new[] { "ParentId", "Route", "NameAr", "NameEn" },
                values: new object[] { new Guid("6d9f1c00-0000-4000-8000-000000000008"), "/attendance/daily", "الحضور اليومي", "Daily Attendance" });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000009002"),
                columns: new[] { "ParentId", "Route", "NameAr", "NameEn" },
                values: new object[] { new Guid("6d9f1c00-0000-4000-8000-000000000008"), "/attendance/evaluations", "تقييمات الطلاب", "Student Evaluations" });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000010001"),
                columns: new[] { "ParentId", "Route", "NameAr", "NameEn" },
                values: new object[] { new Guid("6d9f1c00-0000-4000-8000-000000000009"), "/transport/routes", "خطوط السير", "Bus Routes" });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000010002"),
                columns: new[] { "ParentId", "Route", "NameAr", "NameEn" },
                values: new object[] { new Guid("6d9f1c00-0000-4000-8000-000000000009"), "/transport/buses", "الباصات", "Buses" });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000010003"),
                columns: new[] { "ParentId", "Route", "NameAr", "NameEn" },
                values: new object[] { new Guid("6d9f1c00-0000-4000-8000-000000000009"), "/transport/assignments", "توزيع الطلاب على الباصات", "Bus Assignments" });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000010004"),
                columns: new[] { "ParentId", "Route", "NameAr", "NameEn" },
                values: new object[] { new Guid("6d9f1c00-0000-4000-8000-000000000009"), "/transport/trips", "الرحلات", "Trips" });

            migrationBuilder.InsertData(
                table: "MenuItems",
                columns: new[] { "Id", "Icon", "IsActive", "Order", "ParentId", "Route", "CreatedBy", "CreatedOn", "UpdatedBy", "UpdatedOn", "NameAr", "NameEn" },
                values: new object[,]
                {
                    { new Guid("6d9f1c00-0000-4000-8000-000000003007"), null, true, 7, new Guid("6d9f1c00-0000-4000-8000-000000000002"), "/management/subjects", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "المواد الدراسية", "Subjects" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000003010"), null, true, 10, new Guid("6d9f1c00-0000-4000-8000-000000000002"), "/management/timetables", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "الجداول الدراسية", "Timetables" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000004008"), null, true, 1, new Guid("6d9f1c00-0000-4000-8000-000000000011"), "/management/students", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "الطلاب", "Students" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000004009"), null, true, 2, new Guid("6d9f1c00-0000-4000-8000-000000000011"), "/management/parents", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "أولياء الأمور", "Parents" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000004011"), null, true, 3, new Guid("6d9f1c00-0000-4000-8000-000000000011"), "/management/section-assignment", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "تسكين الطلاب في الفصول", "Assign Sections" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000005003"), null, true, 3, new Guid("6d9f1c00-0000-4000-8000-000000000003"), "/financial/revenues", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "الإيرادات", "Revenues" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000005004"), null, true, 4, new Guid("6d9f1c00-0000-4000-8000-000000000003"), "/financial/expenses", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "المصروفات", "Expenses" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000005005"), null, true, 5, new Guid("6d9f1c00-0000-4000-8000-000000000003"), "/financial/fees", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "الرسوم", "Fees" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000005006"), null, true, 6, new Guid("6d9f1c00-0000-4000-8000-000000000003"), "/financial/student-fees", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "رسوم الطلاب", "Student Fees" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000008003"), null, true, 3, new Guid("6d9f1c00-0000-4000-8000-000000000007"), "/clinic/vaccines", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "التطعيمات", "Vaccines" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000008004"), null, true, 4, new Guid("6d9f1c00-0000-4000-8000-000000000007"), "/clinic/vaccination-campaigns", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "حملات التطعيم", "Vaccination Campaigns" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000011001"), null, true, 1, new Guid("6d9f1c00-0000-4000-8000-000000000010"), "/reports/students-status", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "الطلاب حسب الحالة", "Students by Status" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000011002"), null, true, 2, new Guid("6d9f1c00-0000-4000-8000-000000000010"), "/reports/expenses", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "تقرير المصروفات", "Expenses Report" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000011003"), null, true, 3, new Guid("6d9f1c00-0000-4000-8000-000000000010"), "/reports/income", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "تقرير الإيرادات", "Income Report" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000011004"), null, true, 4, new Guid("6d9f1c00-0000-4000-8000-000000000010"), "/reports/attendance", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "تقرير حضور الطلاب", "Attendance Report" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000011005"), null, true, 5, new Guid("6d9f1c00-0000-4000-8000-000000000010"), "/reports/reviews", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "تقرير تقييمات الطلاب", "Reviews Report" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000011006"), null, true, 6, new Guid("6d9f1c00-0000-4000-8000-000000000010"), "/reports/exam-results", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "تقرير نتائج الامتحانات", "Exam Results Report" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000011007"), null, true, 7, new Guid("6d9f1c00-0000-4000-8000-000000000010"), "/reports/transport", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "تقرير النقل المدرسي", "Transport Report" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000003007"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000003010"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000004008"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000004009"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000004011"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000005003"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000005004"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000005005"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000005006"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000008003"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000008004"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000011001"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000011002"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000011003"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000011004"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000011005"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000011006"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000011007"));

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000000002"),
                column: "Order",
                value: 2);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000000003"),
                column: "Order",
                value: 4);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000000004"),
                column: "Order",
                value: 5);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000000005"),
                column: "Order",
                value: 6);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000000006"),
                column: "Order",
                value: 7);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000000011"),
                column: "Order",
                value: 3);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000002001"),
                columns: new[] { "ParentId", "Route", "NameAr", "NameEn" },
                values: new object[] { new Guid("6d9f1c00-0000-4000-8000-000000000002"), "/management/school-info", "بيانات المدرسة", "School Info" });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000002002"),
                columns: new[] { "ParentId", "Route", "NameAr", "NameEn" },
                values: new object[] { new Guid("6d9f1c00-0000-4000-8000-000000000002"), "/management/stages", "المراحل", "Stages" });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000002003"),
                columns: new[] { "ParentId", "Route", "NameAr", "NameEn" },
                values: new object[] { new Guid("6d9f1c00-0000-4000-8000-000000000002"), "/management/grades", "السنوات الدراسية", "Grades" });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000003001"),
                columns: new[] { "ParentId", "Route", "NameAr", "NameEn" },
                values: new object[] { new Guid("6d9f1c00-0000-4000-8000-000000000003"), "/financial/fees", "الرسوم", "Fees" });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000003002"),
                columns: new[] { "ParentId", "Route", "NameAr", "NameEn" },
                values: new object[] { new Guid("6d9f1c00-0000-4000-8000-000000000003"), "/financial/student-fees", "رسوم الطلاب", "Student Fees" });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000003003"),
                columns: new[] { "ParentId", "Route", "NameAr", "NameEn" },
                values: new object[] { new Guid("6d9f1c00-0000-4000-8000-000000000003"), "/financial/expenses", "المصروفات", "Expenses" });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000003004"),
                columns: new[] { "ParentId", "Route", "NameAr", "NameEn" },
                values: new object[] { new Guid("6d9f1c00-0000-4000-8000-000000000003"), "/financial/revenues", "الإيرادات", "Revenues" });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000003005"),
                columns: new[] { "ParentId", "Route", "NameAr", "NameEn" },
                values: new object[] { new Guid("6d9f1c00-0000-4000-8000-000000000003"), "/financial/expense-categories", "فئات المصروفات", "Expense Categories" });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000003006"),
                columns: new[] { "ParentId", "Route", "NameAr", "NameEn" },
                values: new object[] { new Guid("6d9f1c00-0000-4000-8000-000000000003"), "/financial/revenue-categories", "فئات الإيرادات", "Revenue Categories" });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000005001"),
                columns: new[] { "ParentId", "Route", "NameAr", "NameEn" },
                values: new object[] { new Guid("6d9f1c00-0000-4000-8000-000000000005"), "/exam/exams", "الامتحانات", "Exams" });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000005002"),
                columns: new[] { "ParentId", "Route", "NameAr", "NameEn" },
                values: new object[] { new Guid("6d9f1c00-0000-4000-8000-000000000005"), "/exam/promotion", "اعتماد النتائج والترفيع", "Results Approval & Promotion" });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000006001"),
                columns: new[] { "ParentId", "Route", "NameAr", "NameEn" },
                values: new object[] { new Guid("6d9f1c00-0000-4000-8000-000000000006"), "/users/employee-groups", "مجموعات الموظفين", "Employee Groups" });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000006002"),
                columns: new[] { "ParentId", "Route", "NameAr", "NameEn" },
                values: new object[] { new Guid("6d9f1c00-0000-4000-8000-000000000006"), "/users/employees", "الموظفون", "Employees" });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000007001"),
                columns: new[] { "ParentId", "Route", "NameAr", "NameEn" },
                values: new object[] { new Guid("6d9f1c00-0000-4000-8000-000000000007"), "/clinic/students", "الملفات الصحية للطلاب", "Student Health Files" });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000007002"),
                columns: new[] { "ParentId", "Route", "NameAr", "NameEn" },
                values: new object[] { new Guid("6d9f1c00-0000-4000-8000-000000000007"), "/clinic/visits", "زيارات العيادة", "Clinic Visits" });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000008001"),
                columns: new[] { "ParentId", "Route", "NameAr", "NameEn" },
                values: new object[] { new Guid("6d9f1c00-0000-4000-8000-000000000008"), "/attendance/daily", "الحضور اليومي", "Daily Attendance" });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000008002"),
                columns: new[] { "ParentId", "Route", "NameAr", "NameEn" },
                values: new object[] { new Guid("6d9f1c00-0000-4000-8000-000000000008"), "/attendance/evaluations", "تقييمات الطلاب", "Student Evaluations" });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000009001"),
                columns: new[] { "ParentId", "Route", "NameAr", "NameEn" },
                values: new object[] { new Guid("6d9f1c00-0000-4000-8000-000000000009"), "/transport/routes", "خطوط السير", "Bus Routes" });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000009002"),
                columns: new[] { "ParentId", "Route", "NameAr", "NameEn" },
                values: new object[] { new Guid("6d9f1c00-0000-4000-8000-000000000009"), "/transport/buses", "الباصات", "Buses" });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000010001"),
                columns: new[] { "ParentId", "Route", "NameAr", "NameEn" },
                values: new object[] { new Guid("6d9f1c00-0000-4000-8000-000000000010"), "/reports/students-status", "الطلاب حسب الحالة", "Students by Status" });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000010002"),
                columns: new[] { "ParentId", "Route", "NameAr", "NameEn" },
                values: new object[] { new Guid("6d9f1c00-0000-4000-8000-000000000010"), "/reports/expenses", "تقرير المصروفات", "Expenses Report" });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000010003"),
                columns: new[] { "ParentId", "Route", "NameAr", "NameEn" },
                values: new object[] { new Guid("6d9f1c00-0000-4000-8000-000000000010"), "/reports/income", "تقرير الإيرادات", "Income Report" });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("6d9f1c00-0000-4000-8000-000000010004"),
                columns: new[] { "ParentId", "Route", "NameAr", "NameEn" },
                values: new object[] { new Guid("6d9f1c00-0000-4000-8000-000000000010"), "/reports/attendance", "تقرير حضور الطلاب", "Attendance Report" });

            migrationBuilder.InsertData(
                table: "MenuItems",
                columns: new[] { "Id", "Icon", "IsActive", "Order", "ParentId", "Route", "CreatedBy", "CreatedOn", "UpdatedBy", "UpdatedOn", "NameAr", "NameEn" },
                values: new object[,]
                {
                    { new Guid("6d9f1c00-0000-4000-8000-000000002004"), null, true, 4, new Guid("6d9f1c00-0000-4000-8000-000000000002"), "/management/sections", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "الفصول", "Sections" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000002005"), null, true, 5, new Guid("6d9f1c00-0000-4000-8000-000000000002"), "/management/academic-years", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "السنوات الأكاديمية", "Academic Years" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000002006"), null, true, 6, new Guid("6d9f1c00-0000-4000-8000-000000000002"), "/management/teachers", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "المعلمين", "Teachers" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000002007"), null, true, 7, new Guid("6d9f1c00-0000-4000-8000-000000000002"), "/management/subjects", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "المواد الدراسية", "Subjects" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000002008"), null, true, 1, new Guid("6d9f1c00-0000-4000-8000-000000000011"), "/management/students", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "الطلاب", "Students" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000002009"), null, true, 2, new Guid("6d9f1c00-0000-4000-8000-000000000011"), "/management/parents", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "أولياء الأمور", "Parents" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000002010"), null, true, 10, new Guid("6d9f1c00-0000-4000-8000-000000000002"), "/management/timetables", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "الجداول الدراسية", "Timetables" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000002011"), null, true, 3, new Guid("6d9f1c00-0000-4000-8000-000000000011"), "/management/section-assignment", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "تسكين الطلاب في الفصول", "Assign Sections" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000004001"), null, true, 1, new Guid("6d9f1c00-0000-4000-8000-000000000004"), "/notification/notifications", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "الإشعارات", "Notifications" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000004002"), null, true, 2, new Guid("6d9f1c00-0000-4000-8000-000000000004"), "/notification/school-events", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "فعاليات المدرسة", "School Events" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000006003"), null, true, 3, new Guid("6d9f1c00-0000-4000-8000-000000000006"), "/users/group-permissions", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "صلاحيات المجموعات", "Group Permissions" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000007003"), null, true, 3, new Guid("6d9f1c00-0000-4000-8000-000000000007"), "/clinic/vaccines", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "التطعيمات", "Vaccines" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000007004"), null, true, 4, new Guid("6d9f1c00-0000-4000-8000-000000000007"), "/clinic/vaccination-campaigns", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "حملات التطعيم", "Vaccination Campaigns" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000009003"), null, true, 3, new Guid("6d9f1c00-0000-4000-8000-000000000009"), "/transport/assignments", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "توزيع الطلاب على الباصات", "Bus Assignments" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000009004"), null, true, 4, new Guid("6d9f1c00-0000-4000-8000-000000000009"), "/transport/trips", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "الرحلات", "Trips" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000010005"), null, true, 5, new Guid("6d9f1c00-0000-4000-8000-000000000010"), "/reports/reviews", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "تقرير تقييمات الطلاب", "Reviews Report" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000010006"), null, true, 6, new Guid("6d9f1c00-0000-4000-8000-000000000010"), "/reports/exam-results", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "تقرير نتائج الامتحانات", "Exam Results Report" },
                    { new Guid("6d9f1c00-0000-4000-8000-000000010007"), null, true, 7, new Guid("6d9f1c00-0000-4000-8000-000000000010"), "/reports/transport", new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2026, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "تقرير النقل المدرسي", "Transport Report" }
                });
        }
    }
}
