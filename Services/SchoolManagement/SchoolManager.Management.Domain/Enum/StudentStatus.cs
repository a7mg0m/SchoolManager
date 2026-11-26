namespace SchoolManager.Management.Domain
{
    /// <summary>Where the student stands with the school</summary>
    public enum StudentStatus
    {
        /// <summary>Enrolled now (حالي)</summary>
        Current = 1,

        /// <summary>Finished school (متخرج)</summary>
        Graduated = 2,

        /// <summary>Moved to another school (منقول)</summary>
        Transferred = 3
    }
}
