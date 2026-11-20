using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Vakantiepark.UI.Pages.Shared
{
    // A date field in dd-mm-jjjj with a Dutch calendar (US-38). A browser date input follows the
    // browser's language, so this is a text field plus a calendar that is always nl-NL.
    public partial class InputDutchDate
    {
        public const string Format = "dd-MM-yyyy";
        private static readonly string[] AcceptedFormats = [Format, "d-M-yyyy"];
        private static readonly DateTimeFormatInfo Dutch = new CultureInfo("nl-NL").DateTimeFormat;

        // The years in the year list; the default range also fits a birth date.
        [Parameter] public int MinYear { get; set; } = 1900;
        [Parameter] public int MaxYear { get; set; } = DateTime.Today.Year + 5;

        private bool isOpen;
        private DateOnly shownMonth;

        private static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

        // Monday first, as on a Dutch calendar: ma di wo do vr za zo.
        private static IEnumerable<string> WeekDays
            => Enumerable.Range(1, 7).Select(index => Dutch.AbbreviatedDayNames[index % 7]);

        private IEnumerable<int> Years => Enumerable.Range(MinYear, MaxYear - MinYear + 1).Reverse();

        // Empty cells before the 1st, because the week starts on Monday.
        private int LeadingBlanks => ((int)shownMonth.DayOfWeek + 6) % 7;

        private void ToggleCalendar()
        {
            isOpen = !isOpen;
            if (isOpen)
                ShowMonthOf(CurrentValue ?? Today);
        }

        private void ShowMonthOf(DateOnly date)
            => shownMonth = new DateOnly(Math.Clamp(date.Year, MinYear, MaxYear), date.Month, 1);

        private void ShowMonth(int months)
            => ShowMonthOf(shownMonth.AddMonths(months));

        private void SetMonth(int month)
            => shownMonth = new DateOnly(shownMonth.Year, month, 1);

        private void SetYear(int year)
            => shownMonth = new DateOnly(year, shownMonth.Month, 1);

        private void Choose(DateOnly date)
        {
            CurrentValue = date;
            isOpen = false;
        }

        private void OnTextChanged(ChangeEventArgs args)
            => CurrentValueAsString = args.Value?.ToString();

        private void Close()
            => isOpen = false;

        private void OnKeyDown(KeyboardEventArgs args)
        {
            if (args.Key == "Escape")
                Close();
        }

        protected override string? FormatValueAsString(DateOnly? value)
            => value?.ToString(Format, CultureInfo.InvariantCulture);

        protected override bool TryParseValueFromString(string? value, out DateOnly? result, [NotNullWhen(false)] out string? validationErrorMessage)
        {
            validationErrorMessage = null;
            result = null;

            if (string.IsNullOrWhiteSpace(value))
                return true;

            if (DateOnly.TryParseExact(value.Trim(), AcceptedFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date))
            {
                result = date;
                return true;
            }

            validationErrorMessage = "Vul de datum in als dd-mm-jjjj, bijvoorbeeld 05-03-1985.";
            return false;
        }
    }
}
