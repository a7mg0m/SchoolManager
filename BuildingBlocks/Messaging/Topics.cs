namespace BuildingBlocks.Messaging
{
    /// <summary>Kafka topic names shared by producers and consumers.</summary>
    public static class Topics
    {
        /// <summary>Teacher created / updated / deleted in the Management service (key = teacher id).</summary>
        public const string ManagementTeachers = "management.teachers";
    }
}
