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

        private static Guid Child(int group, int index) => new($"6d9f1c00-0000-4000-8000-0000000{group:D2}{index:D3}");

        public static readonly IReadOnlyList<Item> Items = new List<Item>
        {
            // Home
            new(Home, null, "الرئيسية", "Home", "pe-7s-rocket", "/home", 1),

            // Management
            new(Management, null, "الإدارة", "Management", "pe-7s-note2", null, 2),
            new(Child(2, 1), Management, "بيانات المدرسة", "School Info", null, "/management/school-info", 1),
            new(Child(2, 2), Management, "المراحل", "Stages", null, "/management/stages", 2),
            new(Child(2, 3), Management, "السنوات الدراسية", "Grades", null, "/management/grades", 3),
            new(Child(2, 4), Management, "الفصول", "Sections", null, "/management/sections", 4),
            new(Child(2, 5), Management, "السنوات الأكاديمية", "Academic Years", null, "/management/academic-years", 5),
            new(Child(2, 6), Management, "المعلمين", "Teachers", null, "/management/teachers", 6),
            new(Child(2, 7), Management, "المواد الدراسية", "Subjects", null, "/management/subjects", 7),
            new(Child(2, 8), Management, "الطلاب", "Students", null, "/management/students", 8),
            new(Child(2, 9), Management, "أولياء الأمور", "Parents", null, "/management/parents", 9),
            new(Child(2, 10), Management, "الجداول الدراسية", "Timetables", null, "/management/timetables", 10),

            // Financial
            new(Financial, null, "المالية", "Financial", "pe-7s-cash", null, 3),
            new(Child(3, 1), Financial, "الرسوم", "Fees", null, "/financial/fees", 1),
            new(Child(3, 2), Financial, "رسوم الطلاب", "Student Fees", null, "/financial/student-fees", 2),
            new(Child(3, 3), Financial, "المصروفات", "Expenses", null, "/financial/expenses", 3),
            new(Child(3, 4), Financial, "الإيرادات", "Revenues", null, "/financial/revenues", 4),
            new(Child(3, 5), Financial, "فئات المصروفات", "Expense Categories", null, "/financial/expense-categories", 5),
            new(Child(3, 6), Financial, "فئات الإيرادات", "Revenue Categories", null, "/financial/revenue-categories", 6),

            // Notification
            new(Notification, null, "الإشعارات", "Notification", "pe-7s-bell", null, 4),
            new(Child(4, 1), Notification, "الإشعارات", "Notifications", null, "/notification/notifications", 1),
            new(Child(4, 2), Notification, "فعاليات المدرسة", "School Events", null, "/notification/school-events", 2),

            // Exams & Homework
            new(Exam, null, "الامتحانات والواجبات", "Exams & Homework", "pe-7s-note2", null, 5),
            new(Child(5, 1), Exam, "الامتحانات", "Exams", null, "/exam/exams", 1),

            // Users
            new(Users, null, "المستخدمون", "Users", "pe-7s-users", null, 6),
            new(Child(6, 1), Users, "مجموعات الموظفين", "Employee Groups", null, "/users/employee-groups", 1),
            new(Child(6, 2), Users, "الموظفون", "Employees", null, "/users/employees", 2),
            new(Child(6, 3), Users, "صلاحيات المجموعات", "Group Permissions", null, "/users/group-permissions", 3),
        };
    }
}
