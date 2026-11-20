using BuildingBlocks.CQRS;
using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Application
{
    public record CreateRevenueCommand(
        string Title,
        string? Description,
        Guid CategoryId,
        RevenueType Type,
        decimal Amount,
        string Currency,
        DateTime RevenueDate,
        string? Reference,
        /// <summary>The academic year chosen in the admin header</summary>
        Guid AcademicYearId = default
    ) : ICommand<Guid>;
}
