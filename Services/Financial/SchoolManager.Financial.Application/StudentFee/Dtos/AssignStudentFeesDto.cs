namespace SchoolManager.Financial.Application
{
    public class AssignStudentFeesDto
    {
        /// <summary>Students to assign the fees to (one StudentFee is created per student).</summary>
        public List<Guid> StudentIds { get; set; } = new();

        /// <summary>Fees (from /api/fee) to include. They must all use the same currency.</summary>
        public List<Guid> FeeIds { get; set; } = new();

        /// <summary>Discount applied to each student's total (same currency as the fees).</summary>
        public decimal DiscountAmount { get; set; }

        public DateTime DueDate { get; set; }
    }
}
