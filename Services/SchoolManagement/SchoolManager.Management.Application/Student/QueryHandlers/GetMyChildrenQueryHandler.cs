using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
    public class GetMyChildrenQueryHandler : ICommandHandler<GetMyChildrenQuery, List<ChildDto>>
    {
        private readonly IStudentRepository _repository;

        public GetMyChildrenQueryHandler(IStudentRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<ChildDto>> Handle(GetMyChildrenQuery request, CancellationToken cancellationToken)
        {
            var students = await _repository.GetByParentWithAcademicInfoAsync(request.ParentId, cancellationToken);

            return students
                .OrderBy(s => s.DateOfBirth) // oldest child first
                .Select(s => new ChildDto
                {
                    Id = s.Id,
                    FullNameAr = s.FullName.Arabic,
                    FullNameEn = s.FullName.English,
                    DateOfBirth = s.DateOfBirth,
                    Gender = s.Gender,
                    StageId = s.StageId,
                    StageNameAr = s.Stage?.Name.Arabic ?? string.Empty,
                    StageNameEn = s.Stage?.Name.English ?? string.Empty,
                    GradeId = s.GradeId,
                    GradeNameAr = s.Grade?.Name.Arabic ?? string.Empty,
                    GradeNameEn = s.Grade?.Name.English ?? string.Empty,
                    SectionId = s.SectionId,
                    SectionNameAr = s.Section?.Name.Arabic ?? string.Empty,
                    SectionNameEn = s.Section?.Name.English ?? string.Empty,
                    AcademicYearId = s.AcademicYearId,
                    Latitude = s.Latitude,
                    Longitude = s.Longitude
                })
                .ToList();
        }
    }
}
