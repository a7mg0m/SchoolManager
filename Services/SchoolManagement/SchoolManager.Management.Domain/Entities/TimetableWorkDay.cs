namespace SchoolManager.Management.Domain
{
    /// <summary>
    /// One day of a timetable's week: is it a school day, its break, and how its periods are laid out
    /// (slot duration in minutes × number of slots, starting at the timetable's DayStartTime). Stored with the timetable.
    /// </summary>
    public sealed class TimetableWorkDay
    {
        public const int MinSlotDuration = 5;
        public const int MaxSlotDuration = 240;
        public const int MaxSlotCount = 20;

        public DayOfWeek Day { get; private set; }
        public bool IsWorkDay { get; private set; }
        public TimeOnly? BreakStart { get; private set; }
        public TimeOnly? BreakEnd { get; private set; }

        /// <summary>Minutes of one period</summary>
        public int SlotDuration { get; private set; }

        /// <summary>Number of periods in the day</summary>
        public int SlotCount { get; private set; }

        private TimetableWorkDay() { }

        public TimetableWorkDay(DayOfWeek day, bool isWorkDay, TimeOnly? breakStart, TimeOnly? breakEnd, int slotDuration, int slotCount)
        {
            if (breakStart.HasValue != breakEnd.HasValue)
                throw new ArgumentException("Break start and end must be given together.");
            if (breakStart is TimeOnly s && breakEnd is TimeOnly e && e <= s)
                throw new ArgumentException("The break must end after it starts.");

            Day = day;
            IsWorkDay = isWorkDay;
            BreakStart = breakStart;
            BreakEnd = breakEnd;
            SlotDuration = slotDuration;
            SlotCount = slotCount;
        }
    }
}
