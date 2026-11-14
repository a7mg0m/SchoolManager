using AutoMapper;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    internal static class EmployeeGroupMapper
    {
        /// <summary>Maps groups to DTOs (sorted by English name) and fills EmployeesCount.</summary>
        public static async Task<List<EmployeeGroupDto>> MapWithCountsAsync(
            IEnumerable<EmployeeGroup> groups, IEmployeeGroupRepository repository, IMapper mapper)
        {
            var counts = await repository.GetEmployeeCountsAsync();
            return groups
                .OrderBy(g => g.Name.English)
                .Select(g =>
                {
                    var dto = mapper.Map<EmployeeGroupDto>(g);
                    dto.EmployeesCount = counts.GetValueOrDefault(g.Id);
                    return dto;
                })
                .ToList();
        }
    }
}
