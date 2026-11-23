namespace SchoolManager.Management.Application
{
    /// <summary>A child of the logged-in parent, with the names the parent app shows (both languages).</summary>
    public class ChildDto
    {
        public Guid Id { get; set; }
        public string FullNameAr { get; set; } = string.Empty;
        public string FullNameEn { get; set; } = string.Empty;
        public DateOnly DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;

        public Guid StageId { get; set; }
        public string StageNameAr { get; set; } = string.Empty;
        public string StageNameEn { get; set; } = string.Empty;

        public Guid GradeId { get; set; }
        public string GradeNameAr { get; set; } = string.Empty;
        public string GradeNameEn { get; set; } = string.Empty;

        public Guid SectionId { get; set; }
        public string SectionNameAr { get; set; } = string.Empty;
        public string SectionNameEn { get; set; } = string.Empty;

        public Guid AcademicYearId { get; set; }

        /// <summary>Home location (map pin); null when not set</summary>
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}
