namespace SchoolManager.Attendance.Application
{
    public class BusDto
    {
        public Guid Id { get; set; }
        public string Number { get; set; } = string.Empty;

        /// <summary>رقم اللوحة</summary>
        public string PlateNumber { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public string DriverName { get; set; } = string.Empty;
        public string DriverPhone { get; set; } = string.Empty;
        public Guid BusRouteId { get; set; }
        public bool IsActive { get; set; }
    }
}
