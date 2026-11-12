namespace SchoolManager.Management.Application
{
	/// <summary>
	/// Thrown when one or more student national IDs already belong to an existing student.
	/// </summary>
	public class DuplicateNationalIdException : Exception
	{
		public const string ErrorKey = "NationalIdAlreadyExists";

		public IReadOnlyCollection<string> NationalIds { get; }

		public DuplicateNationalIdException(IReadOnlyCollection<string> nationalIds)
			: base($"National ID already exists: {string.Join(", ", nationalIds)}")
		{
			NationalIds = nationalIds;
		}
	}
}
