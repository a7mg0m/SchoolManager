namespace SchoolManager.Management.Application
{
    /// <summary>Body of PUT /api/student/my-children/{studentId}/location (both values, or neither to clear)</summary>
    public class SetStudentLocationDto
    {
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}
