using BuildingBlocks.CQRS;
using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Application
{
    /// <summary>A payment applied to a student fee (income report)</summary>
    public record StudentFeePaymentDto(
        Guid Id,
        Guid StudentFeeId,
        Guid StudentId,
        Guid? AcademicYearId,
        bool IsBusFee,
        decimal Amount,
        string Currency,
        DateTime PaidOn);

    /// <summary>Payments between two dates (both included, either open), oldest first</summary>
    public record GetStudentFeePaymentsQuery(DateOnly? From, DateOnly? To) : IQuery<List<StudentFeePaymentDto>>;

    public class GetStudentFeePaymentsQueryHandler(IStudentFeePaymentRepository payments)
        : IQueryHandler<GetStudentFeePaymentsQuery, List<StudentFeePaymentDto>>
    {
        public async Task<List<StudentFeePaymentDto>> Handle(GetStudentFeePaymentsQuery query, CancellationToken cancellationToken)
        {
            var from = query.From?.ToDateTime(TimeOnly.MinValue);
            var toExclusive = query.To?.AddDays(1).ToDateTime(TimeOnly.MinValue);
            var list = await payments.SearchAsync(p =>
                (from == null || p.PaidOn >= from) && (toExclusive == null || p.PaidOn < toExclusive));
            return list
                .OrderBy(p => p.PaidOn)
                .Select(p => new StudentFeePaymentDto(p.Id, p.StudentFeeId, p.StudentId, p.AcademicYearId, p.IsBusFee, p.Amount, p.Currency, p.PaidOn))
                .ToList();
        }
    }
}
