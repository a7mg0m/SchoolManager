namespace SchoolManager.Management.Domain
{
    public enum VaccinationStatus
    {
        Given = 1,          // تم إعطاؤه
        NotGiven = 2,       // لم يُعطَ
        Exempted = 3,       // معفى
        ParentRefused = 4   // رفض ولي الأمر
    }
}
