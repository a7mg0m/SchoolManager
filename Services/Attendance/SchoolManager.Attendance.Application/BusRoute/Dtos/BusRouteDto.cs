namespace SchoolManager.Attendance.Application
{
    public class BusRouteDto
    {
        public Guid Id { get; set; }
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }

        /// <summary>In the order the bus passes them</summary>
        public List<BusRouteStopDto> Stops { get; set; } = new();
    }
}
