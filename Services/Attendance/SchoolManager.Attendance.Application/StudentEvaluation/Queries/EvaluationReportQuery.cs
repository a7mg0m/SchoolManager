using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Application
{
    /// <summary>
    /// Admin reviews report: every lesson evaluation between two dates (both included, at most a year), newest first.
    /// Evaluations keep no class: the admin site groups them by the students' classes.
    /// </summary>
    public record GetEvaluationReportQuery(DateOnly From, DateOnly To) : IQuery<List<StudentEvaluationDto>>;

    public class GetEvaluationReportQueryHandler(IStudentEvaluationRepository evaluations, IMapper mapper)
        : IQueryHandler<GetEvaluationReportQuery, List<StudentEvaluationDto>>
    {
        public const int MaxDays = 366;

        public async Task<List<StudentEvaluationDto>> Handle(GetEvaluationReportQuery query, CancellationToken cancellationToken)
        {
            var from = query.From;
            var to = query.To < from ? from : query.To;
            if (to.DayNumber - from.DayNumber > MaxDays) to = from.AddDays(MaxDays);

            var list = await evaluations.GetBetweenAsync(from, to, cancellationToken);
            return mapper.Map<List<StudentEvaluationDto>>(list);
        }
    }
}
