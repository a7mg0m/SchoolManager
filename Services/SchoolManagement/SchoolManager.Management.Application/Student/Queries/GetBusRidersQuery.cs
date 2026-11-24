using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
    /// <summary>
    /// Driver app: what a bus driver needs about the students on a trip (name, class, parent's phone, home location).
    /// The ids come from the Attendance service (the students of a trip of the driver's bus).
    /// </summary>
    public record GetBusRidersQuery(IReadOnlyCollection<Guid> StudentIds) : IQuery<List<BusRiderDto>>;

    public class BusRiderDto
    {
        public Guid Id { get; set; }
        public string FullNameAr { get; set; } = string.Empty;
        public string FullNameEn { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string GradeNameAr { get; set; } = string.Empty;
        public string GradeNameEn { get; set; } = string.Empty;
        public string SectionNameAr { get; set; } = string.Empty;
        public string SectionNameEn { get; set; } = string.Empty;
        public string ParentNameAr { get; set; } = string.Empty;
        public string ParentNameEn { get; set; } = string.Empty;
        public string ParentPhone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }

    public class GetBusRidersQueryHandler : IQueryHandler<GetBusRidersQuery, List<BusRiderDto>>
    {
        private readonly IStudentRepository _repository;

        public GetBusRidersQueryHandler(IStudentRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<BusRiderDto>> Handle(GetBusRidersQuery query, CancellationToken cancellationToken)
        {
            var students = await _repository.GetByIdsWithParentAsync(query.StudentIds.Distinct().ToList(), cancellationToken);
            return students.Select(s => new BusRiderDto
            {
                Id = s.Id,
                FullNameAr = s.FullName.Arabic,
                FullNameEn = s.FullName.English,
                Gender = s.Gender,
                GradeNameAr = s.Grade?.Name.Arabic ?? string.Empty,
                GradeNameEn = s.Grade?.Name.English ?? string.Empty,
                SectionNameAr = s.Section?.Name.Arabic ?? string.Empty,
                SectionNameEn = s.Section?.Name.English ?? string.Empty,
                ParentNameAr = s.Parent?.FullName.Arabic ?? string.Empty,
                ParentNameEn = s.Parent?.FullName.English ?? string.Empty,
                ParentPhone = s.Parent?.PhoneNumber ?? string.Empty,
                Address = s.Address,
                Latitude = s.Latitude,
                Longitude = s.Longitude
            }).ToList();
        }
    }
}
