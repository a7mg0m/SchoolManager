namespace SchoolManager.Identity.Infrastructure
{
    /// <summary>
    /// Initial menu (same items, order, icons and routes as the Angular sidebar).
    /// Ids are fixed so the seed migration is stable; change items through the API afterwards,
    /// or edit this list and add a migration.
    /// </summary>
    internal static class MenuItemSeed
    {
        internal sealed record Item(Guid Id, Guid? ParentId, string NameAr, string NameEn, string? Icon, string? Route, int Order);

        /// <summary>Seed date (fixed, so the model does not change on every build).</summary>
        public static readonly DateTime SeededOn = new(2026, 11, 14, 0, 0, 0, DateTimeKind.Unspecified);

        // Top-level items / groups
        public static readonly Guid Home = new("6d9f1c00-0000-4000-8000-000000000001");
        public static readonly Guid Management = new("6d9f1c00-0000-4000-8000-000000000002");
        public static readonly Guid Financial = new("6d9f1c00-0000-4000-8000-000000000003");
        public static readonly Guid Notification = new("6d9f1c00-0000-4000-8000-000000000004");
        public static readonly Guid Exam = new("6d9f1c00-0000-4000-8000-000000000005");
        public static readonly Guid Users = new("6d9f1c00-0000-4000-8000-000000000006");
        public static readonly Guid Clinic = new("6d9f1c00-0000-4000-8000-000000000007");
        public static readonly Guid Attendance = new("6d9f1c00-0000-4000-8000-000000000008");
        public static readonly Guid Transport = new("6d9f1c00-0000-4000-8000-000000000009");
        public static readonly Guid Reports = new("6d9f1c00-0000-4000-8000-000000000010");
        public static readonly Guid StudentManagement = new("6d9f1c00-0000-4000-8000-000000000011");

        private static Guid Child(int group, int index) => new($"6d9f1c00-0000-4000-8000-0000000{group:D2}{index:D3}");

        public static readonly IReadOnlyList<Item> Items = new List<Item>
        {
            // Home
            new(Home, null, "الرئيسية", "Home", "pe-7s-rocket", "/home", 1),

            // Users
            new(Users, null, "المستخدمون", "Users", "pe-7s-users", null, 2),
            new(Child(2, 1), Users, "مجموعات الموظفين", "Employee Groups", null, "/users/employee-groups", 1),
            new(Child(2, 2), Users, "الموظفون", "Employees", null, "/users/employees", 2),
            new(Child(2, 3), Users, "صلاحيات المجموعات", "Group Permissions", null, "/users/group-permissions", 3),

            // Management
            new(Management, null, "الإدارة", "Management", "pe-7s-note2", null, 3),
            new(Child(3, 1), Management, "بيانات المدرسة", "School Info", null, "/management/school-info", 1),
            new(Child(3, 2), Management, "المراحل", "Stages", null, "/management/stages", 2),
            new(Child(3, 3), Management, "السنوات الدراسية", "Grades", null, "/management/grades", 3),
            new(Child(3, 4), Management, "الفصول", "Sections", null, "/management/sections", 4),
            new(Child(3, 5), Management, "السنوات الأكاديمية", "Academic Years", null, "/management/academic-years", 5),
            new(Child(3, 6), Management, "المعلمين", "Teachers", null, "/management/teachers", 6),
            new(Child(3, 7), Management, "المواد الدراسية", "Subjects", null, "/management/subjects", 7),
            new(Child(3, 10), Management, "الجداول الدراسية", "Timetables", null, "/management/timetables", 10),

            
            
            // Student management (right after Management)
            new(StudentManagement, null, "إدارة الطلاب", "Student Management", "pe-7s-study", null, 4),
            new(Child(4, 8), StudentManagement, "الطلاب", "Students", null, "/management/students", 1),
            new(Child(4, 9), StudentManagement, "أولياء الأمور", "Parents", null, "/management/parents", 2),
            new(Child(4, 11), StudentManagement, "تسكين الطلاب في الفصول", "Assign Sections", null, "/management/section-assignment", 3),

            // Financial
            new(Financial, null, "المالية", "Financial", "pe-7s-cash", null, 5),
            new(Child(5, 1), Financial, "فئات الإيرادات", "Revenue Categories", null, "/financial/revenue-categories", 1),
            new(Child(5, 2), Financial, "فئات المصروفات", "Expense Categories", null, "/financial/expense-categories", 2),
            new(Child(5, 3), Financial, "الإيرادات", "Revenues", null, "/financial/revenues", 3),
            new(Child(5, 4), Financial, "المصروفات", "Expenses", null, "/financial/expenses", 4),
            new(Child(5, 5), Financial, "الرسوم", "Fees", null, "/financial/fees", 5),
            new(Child(5, 6), Financial, "رسوم الطلاب", "Student Fees", null, "/financial/student-fees", 6),

            // Notification
            new(Notification, null, "الإشعارات", "Notification", "pe-7s-bell", null, 6),
            new(Child(6, 1), Notification, "الإشعارات", "Notifications", null, "/notification/notifications", 1),
            new(Child(6, 2), Notification, "فعاليات المدرسة", "School Events", null, "/notification/school-events", 2),

            // Exams & Homework
            new(Exam, null, "الامتحانات والواجبات", "Exams & Homework", "pe-7s-note2", null, 7),
            new(Child(7, 1), Exam, "الامتحانات", "Exams", null, "/exam/exams", 1),
            new(Child(7, 2), Exam, "اعتماد النتائج والترفيع", "Results Approval & Promotion", null, "/exam/promotion", 2),

            // School clinic
            new(Clinic, null, "العيادة المدرسية", "School Clinic", "pe-7s-like2", null, 8),
            new(Child(8, 1), Clinic, "الملفات الصحية للطلاب", "Student Health Files", null, "/clinic/students", 1),
            new(Child(8, 2), Clinic, "زيارات العيادة", "Clinic Visits", null, "/clinic/visits", 2),
            new(Child(8, 3), Clinic, "التطعيمات", "Vaccines", null, "/clinic/vaccines", 3),
            new(Child(8, 4), Clinic, "حملات التطعيم", "Vaccination Campaigns", null, "/clinic/vaccination-campaigns", 4),

            // Attendance
            new(Attendance, null, "الحضور والغياب", "Attendance", "pe-7s-check", null, 9),
            new(Child(9, 1), Attendance, "الحضور اليومي", "Daily Attendance", null, "/attendance/daily", 1),
            new(Child(9, 2), Attendance, "تقييمات الطلاب", "Student Evaluations", null, "/attendance/evaluations", 2),

            // School transport
            new(Transport, null, "النقل المدرسي", "School Transport", "pe-7s-car", null, 10),
            new(Child(10, 1), Transport, "خطوط السير", "Bus Routes", null, "/transport/routes", 1),
            new(Child(10, 2), Transport, "الباصات", "Buses", null, "/transport/buses", 2),
            new(Child(10, 3), Transport, "توزيع الطلاب على الباصات", "Bus Assignments", null, "/transport/assignments", 3),
            new(Child(10, 4), Transport, "الرحلات", "Trips", null, "/transport/trips", 4),

            // Reports
            new(Reports, null, "التقارير", "Reports", "pe-7s-graph2", null, 11),
            new(Child(11, 1), Reports, "الطلاب حسب الحالة", "Students by Status", null, "/reports/students-status", 1),
            new(Child(11, 2), Reports, "تقرير المصروفات", "Expenses Report", null, "/reports/expenses", 2),
            new(Child(11, 3), Reports, "تقرير الإيرادات", "Income Report", null, "/reports/income", 3),
            new(Child(11, 4), Reports, "تقرير حضور الطلاب", "Attendance Report", null, "/reports/attendance", 4),
            new(Child(11, 5), Reports, "تقرير تقييمات الطلاب", "Reviews Report", null, "/reports/reviews", 5),
            new(Child(11, 6), Reports, "تقرير نتائج الامتحانات", "Exam Results Report", null, "/reports/exam-results", 6),
            new(Child(11, 7), Reports, "تقرير النقل المدرسي", "Transport Report", null, "/reports/transport", 7),
        };
    }
}
