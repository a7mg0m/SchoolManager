namespace BuildingBlocks.Messaging
{
    /// <summary>Kafka topic names shared by producers and consumers.</summary>
    public static class Topics
    {
        /// <summary>Teacher created / updated / deleted in the Management service (key = teacher id).</summary>
        public const string ManagementTeachers = "management.teachers";

        /// <summary>Parent created / updated / deleted in the Management service (key = parent id).</summary>
        public const string ManagementParents = "management.parents";
        /// <summary>Bus fee of a student assigned / changed / ended in the Attendance service (key = bus assignment id).</summary>
        public const string AttendanceBusFees = "attendance.bus-fees";

        /// <summary>Bus (and its driver's login) created / updated / deleted in the Attendance service (key = bus id).</summary>
        public const string AttendanceBusDrivers = "attendance.bus-drivers";

        /// <summary>A bus trip started in the Attendance service (key = trip id).</summary>
        public const string AttendanceTripStarted = "attendance.trip-started";

        /// <summary>A homework published to classes in the ExamsHomework service (key = homework id).</summary>
        public const string ExamsHomeworkPublished = "examshomework.homework-published";

        /// <summary>A new exam saved in the ExamsHomework service (key = exam id).</summary>
        public const string ExamsExamCreated = "examshomework.exam-created";
    }
}
