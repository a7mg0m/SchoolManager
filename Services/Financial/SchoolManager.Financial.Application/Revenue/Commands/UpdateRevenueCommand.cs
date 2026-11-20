using BuildingBlocks.CQRS;
using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Application
{
    public record UpdateRevenueCommand(
        Guid Id,
        string Title,
        string? Description,
        Guid CategoryId,
        RevenueType Type,
        decimal Amount,
        string Currency,
        DateTime RevenueDate,
        string? Reference,
        /// <summary>Only used when the revenue has no year yet</summary>
        Guid? AcademicYearId = null
    ) : ICommand<bool>;
}
