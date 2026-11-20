namespace SchoolManager.Attendance.Application
{
    public class BusAssignmentDto
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public Guid BusId { get; set; }
        public Guid AcademicYearId { get; set; }

        /// <summary>Where the student gets on / off; null = not set</summary>
        public Guid? BusRouteStopId { get; set; }

        /// <summary>The student's own bus fee</summary>
        public decimal FeeAmount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public bool IsActive { get; set; }

        /// <summary>The Transportation StudentFee in the Financial service, once linked</summary>
        public Guid? StudentFeeId { get; set; }
    }
}
