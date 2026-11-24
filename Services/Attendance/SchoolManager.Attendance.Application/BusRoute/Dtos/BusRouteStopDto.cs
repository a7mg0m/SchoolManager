namespace SchoolManager.Attendance.Application
{
    /// <summary>A stop of a route (input of create / update, and output)</summary>
    public class BusRouteStopDto
    {
        /// <summary>Set in results; ignored in create / update (the stops are replaced)</summary>
        public Guid Id { get; set; }
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;

        /// <summary>Position on the route (1, 2, 3 …), unique within the route</summary>
        public int Order { get; set; }
    }
}
