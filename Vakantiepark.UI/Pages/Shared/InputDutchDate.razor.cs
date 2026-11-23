using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Vakantiepark.UI.Application.Helpers;

namespace Vakantiepark.UI.Pages.Shared
{
    // A date field in dd-mm-jjjj with a Dutch calendar (US-38). A browser date input follows the
    // browser's language, so this is a text field plus a calendar that is always nl-NL.
    // Works with DateOnly and DateOnly?; only DateOnly? may be left empty.
    public partial class InputDutchDate<TValue>
    {
        private static readonly string[] AcceptedFormats = [DateFormats.Date, "d-M-yyyy"];
        private static readonly DateTimeFormatInfo Dutch = new CultureInfo("nl-NL").DateTimeFormat;
        private static readonly bool IsNullable = Nullable.GetUnderlyingType(typeof(TValue)) is not null;

        // The years in the year list; the default range also fits a birth date.
        [Parameter] public int MinYear { get; set; } = 1900;
        [Parameter] public int MaxYear { get; set; } = DateTime.Today.Year + 5;

        // Days outside these dates cannot be picked in the calendar.
        [Parameter] public DateOnly? MinDate { get; set; }
        [Parameter] public DateOnly? MaxDate { get; set; }

        // For a pair of fields: the departure field gets the arrival as RangeStart, the arrival
        // field the departure as RangeEnd. The calendar then shows the whole stay, also on hover.
        [Parameter] public DateOnly? RangeStart { get; set; }
        [Parameter] public DateOnly? RangeEnd { get; set; }

        private bool isOpen;
        private DateOnly shownMonth;
        private DateOnly? hoveredDate;

        private static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

        private DateOnly? CurrentDate => CurrentValue is DateOnly date ? date : null;

        // disabled="..." on the field also locks the calendar button.
        private bool IsFieldDisabled
            => AdditionalAttributes?.TryGetValue("disabled", out object? value) == true && value is true or "true" or "disabled";

        // Monday first, as on a Dutch calendar: ma di wo do vr za zo.
        private static IEnumerable<string> WeekDays
            => Enumerable.Range(1, 7).Select(index => Dutch.AbbreviatedDayNames[index % 7]);

        private IEnumerable<int> Years => Enumerable.Range(MinYear, MaxYear - MinYear + 1).Reverse();

        // Empty cells before the 1st, because the week starts on Monday.
        private int LeadingBlanks => ((int)shownMonth.DayOfWeek + 6) % 7;

        private void ToggleCalendar()
        {
            isOpen = !isOpen;
            hoveredDate = null;
            if (isOpen)
                ShowMonthOf(CurrentDate ?? RangeStart ?? RangeEnd ?? MinDate ?? Today);
        }

        private void ShowMonthOf(DateOnly date)
            => shownMonth = new DateOnly(Math.Clamp(date.Year, MinYear, MaxYear), date.Month, 1);

        private void ShowMonth(int months)
            => ShowMonthOf(shownMonth.AddMonths(months));

        private void SetMonth(int month)
            => shownMonth = new DateOnly(shownMonth.Year, month, 1);

        private void SetYear(int year)
            => shownMonth = new DateOnly(year, shownMonth.Month, 1);

        private bool IsDisabled(DateOnly date)
            => date < MinDate || date > MaxDate;

        // The stay as it would be with the hovered day, or else with the chosen day.
        private (DateOnly Start, DateOnly End)? ShownRange()
        {
            DateOnly? own = hoveredDate ?? CurrentDate;
            if (RangeStart is DateOnly start && own is DateOnly end && end > start)
                return (start, end);
            if (RangeEnd is DateOnly last && own is DateOnly first && first < last)
                return (first, last);
            return null;
        }

        private string DayClass(DateOnly date)
        {
            List<string> classes = [];
            if (date == CurrentDate)
                classes.Add("selected");
            if (date == Today)
                classes.Add("today");

            if (ShownRange() is (DateOnly start, DateOnly end))
            {
                if (date == start)
                    classes.Add("range-start");
                else if (date == end)
                    classes.Add("range-end");
                else if (date > start && date < end)
                    classes.Add("in-range");
            }

            return string.Join(" ", classes);
        }

        // Through CurrentValueAsString, like typing: that also clears an earlier "not a date" error.
        private void Choose(DateOnly date)
        {
            CurrentValueAsString = date.ToString(DateFormats.Date, CultureInfo.InvariantCulture);
            isOpen = false;
            hoveredDate = null;
        }

        private void OnTextChanged(ChangeEventArgs args)
            => CurrentValueAsString = args.Value?.ToString();

        private void Close()
        {
            isOpen = false;
            hoveredDate = null;
        }

        private void OnKeyDown(KeyboardEventArgs args)
        {
            if (args.Key == "Escape")
                Close();
        }

        protected override string? FormatValueAsString(TValue? value)
            => value is DateOnly date ? date.ToString(DateFormats.Date, CultureInfo.InvariantCulture) : null;

        protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out TValue result, [NotNullWhen(false)] out string? validationErrorMessage)
        {
            validationErrorMessage = null;
            result = default!;

            if (string.IsNullOrWhiteSpace(value))
            {
                if (IsNullable)
                    return true;

                validationErrorMessage = "Vul een datum in.";
                return false;
            }

            if (DateOnly.TryParseExact(value.Trim(), AcceptedFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date))
            {
                result = (TValue)(object)date;
                return true;
            }

            validationErrorMessage = "Vul de datum in als dd-mm-jjjj, bijvoorbeeld 05-03-1985.";
            return false;
        }
    }
}
