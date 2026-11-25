using FluentValidation;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
    /// <summary>Checks of the week settings (shared by create and update)</summary>
    public class TimetableWorkDayDtoValidator : AbstractValidator<TimetableWorkDayDto>
    {
        public TimetableWorkDayDtoValidator()
        {
            RuleFor(x => x.Day).IsInEnum().WithMessage("WorkDayInvalid");

            When(x => x.IsWorkDay, () =>
            {
                RuleFor(x => x.SlotDuration)
                    .InclusiveBetween(TimetableWorkDay.MinSlotDuration, TimetableWorkDay.MaxSlotDuration)
                    .WithMessage("SlotDurationInvalid");
                RuleFor(x => x.SlotCount)
                    .InclusiveBetween(1, TimetableWorkDay.MaxSlotCount)
                    .WithMessage("SlotCountInvalid");
            });

            RuleFor(x => x)
                .Must(d => d.BreakStart.HasValue == d.BreakEnd.HasValue)
                .WithMessage("BreakIncomplete");
            RuleFor(x => x)
                .Must(d => d.BreakStart is null || d.BreakEnd is null || d.BreakEnd > d.BreakStart)
                .WithMessage("BreakEndBeforeStart");
        }

        /// <summary>One row per day; a start time when at least one day is a work day</summary>
        public static bool DaysUnique(List<TimetableWorkDayDto>? days)
            => days is null || days.Select(d => d.Day).Distinct().Count() == days.Count;

        public static bool StartTimeGiven(TimeOnly? dayStartTime, List<TimetableWorkDayDto>? days)
            => days is null || !days.Any(d => d.IsWorkDay) || dayStartTime.HasValue;

        public static IEnumerable<TimetableWorkDay> ToDomain(IEnumerable<TimetableWorkDayDto> days)
            => days.Select(d => new TimetableWorkDay(d.Day, d.IsWorkDay, d.BreakStart, d.BreakEnd, d.SlotDuration, d.SlotCount));
    }
}
