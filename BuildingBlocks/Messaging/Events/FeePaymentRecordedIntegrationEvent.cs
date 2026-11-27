namespace BuildingBlocks.Messaging.Events
{
    /// <summary>
    /// A payment of a student's fee (school fee or bus fee) was recorded (Financial service). Published on
    /// <see cref="Topics.FinancialFeePaymentRecorded"/> with the fee id as key. The Notification service turns it into a
    /// notification for that student's parents (shown in the parent app, pushed to their phones; not listed in the
    /// admin notifications page).
    /// </summary>
    public sealed record FeePaymentRecordedIntegrationEvent : IntegrationEvent
    {
        public Guid PaymentId { get; init; }
        public Guid StudentFeeId { get; init; }
        public Guid StudentId { get; init; }
        public Guid? AcademicYearId { get; init; }

        /// <summary>The bus fee (else the school fee)</summary>
        public bool IsBusFee { get; init; }

        public decimal Amount { get; init; }
        public string Currency { get; init; } = string.Empty;
        public DateTime PaidOn { get; init; }

        /// <summary>What is still to pay on this fee after the payment (0: fully paid)</summary>
        public decimal Balance { get; init; }

        /// <summary>User who recorded it (for the audit columns)</summary>
        public Guid RecordedBy { get; init; }
    }
}
