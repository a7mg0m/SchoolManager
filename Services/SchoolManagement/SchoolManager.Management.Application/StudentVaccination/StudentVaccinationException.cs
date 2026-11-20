using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
    /// <summary>A dose that cannot be saved (the controller returns <see cref="ErrorKey"/>).</summary>
    public class StudentVaccinationException : Exception
    {
        public const string DoseAlreadyRecorded = "DoseAlreadyRecorded";
        public const string VaccineNotFound = "VaccineNotFound";
        public const string DoseNumberExceedsTotalDoses = "DoseNumberExceedsTotalDoses";

        public StudentVaccinationException(string errorKey) : base(errorKey)
        {
            ErrorKey = errorKey;
        }

        public string ErrorKey { get; }

        /// <summary>The vaccine exists, the dose is within its doses, and the student has no other record of this dose</summary>
        public static async Task CheckAsync(Guid? id, Guid studentId, Guid vaccineId, int doseNumber,
            IVaccineRepository vaccines, IStudentVaccinationRepository vaccinations)
        {
            var vaccine = await vaccines.GetByIdAsync(vaccineId)
                ?? throw new StudentVaccinationException(VaccineNotFound);
            if (doseNumber > vaccine.TotalDoses)
                throw new StudentVaccinationException(DoseNumberExceedsTotalDoses);

            var same = await vaccinations.SearchAsync(x => x.StudentId == studentId && x.VaccineId == vaccineId && x.DoseNumber == doseNumber);
            if (same.Any(x => x.Id != id))
                throw new StudentVaccinationException(DoseAlreadyRecorded);
        }
    }
}
