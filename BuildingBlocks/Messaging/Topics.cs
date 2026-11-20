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
    }
}
