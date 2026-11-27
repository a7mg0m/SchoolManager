using AutoMapper;
using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    /// <summary>
    /// An app opened with a saved session (parent / teacher / driver app): the user is checked again as at sign-in
    /// and, while still allowed, gets a fresh token (so the session lasts as long as the app is used).
    /// TokenLoginName is the login name the current token was issued for: a changed login name (e.g. a bus's) ends
    /// the session. Throws DomainException with the reason key otherwise.
    /// </summary>
    public record RenewSessionCommand(Guid UserId, string? TokenLoginName) : ICommand<LoginResultDto>;

    public class RenewSessionCommandHandler(
        IUserRepository users,
        IUserProfileRepository profiles,
        IJwtTokenGenerator jwtTokenGenerator,
        IMapper mapper,
        IParentStudentsChecker parentStudents,
        ITeacherServiceChecker teacherService) : ICommandHandler<RenewSessionCommand, LoginResultDto>
    {
        public async Task<LoginResultDto> Handle(RenewSessionCommand request, CancellationToken cancellationToken)
        {
            var user = await users.GetByIdAsync(request.UserId) ?? throw new DomainException(ErrorKeys.UserInactive);

            // Signed in with another login name (changed by the school since): sign in again with the new one
            if (!string.Equals(user.LoginName, request.TokenLoginName?.Trim(), StringComparison.OrdinalIgnoreCase))
                throw new DomainException(ErrorKeys.LoginNameChanged);

            var profile = (await profiles.SearchAsync(p => p.UserId == user.Id)).FirstOrDefault();

            // The same rules as signing in
            if (user.RoleId == RoleIds.Teacher)
            {
                var inService = await teacherService.IsInServiceAsync(profile?.TeacherId ?? user.Id, cancellationToken);
                if (inService is null) throw new DomainException(ErrorKeys.TeacherCheckUnavailable);
                if (inService == false) throw new DomainException(ErrorKeys.ServiceEnded);
            }

            if (!user.IsActive)
                throw new DomainException(ErrorKeys.UserInactive);

            if (user.RoleId == RoleIds.Parent)
            {
                var hasCurrent = await parentStudents.HasCurrentStudentsAsync(profile?.ParentId ?? user.Id, cancellationToken);
                if (hasCurrent is null) throw new DomainException(ErrorKeys.ParentCheckUnavailable);
                if (hasCurrent == false) throw new DomainException(ErrorKeys.NoCurrentStudents);
            }

            return new LoginResultDto
            {
                UserId = user.Id,
                LoginName = user.LoginName,
                Token = jwtTokenGenerator.GenerateToken(user, profile),
                UserProfile = profile is null ? null : mapper.Map<UserProfileDto>(profile)
            };
        }
    }
}
