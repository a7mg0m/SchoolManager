using BuildingBlocks.CQRS;

namespace SchoolManager.Identity.Application
{
    public class GetMyProfileQueryHandler : IQueryHandler<GetMyProfileQuery, MyProfileDto>
    {
        private readonly MyProfileReader _reader;

        public GetMyProfileQueryHandler(MyProfileReader reader)
        {
            _reader = reader;
        }

        public async Task<MyProfileDto> Handle(GetMyProfileQuery query, CancellationToken cancellationToken)
        {
            var user = await _reader.GetCurrentUserAsync();
            var profile = await _reader.GetProfileAsync(user.Id);
            var isEmployee = await _reader.GetEmployeeAsync(user.Id) is not null;

            return await _reader.BuildAsync(user, profile, isEmployee);
        }
    }
}
