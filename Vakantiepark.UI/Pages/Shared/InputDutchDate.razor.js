// Places the calendar of InputDutchDate under its field, or above it when there is no room below.
// The calendar has position: fixed, so it is measured against the window and follows scrolling.
const margin = 8;
const gap = 4;
const minimumWidth = 304;

export function place(field, calendar) {
    const rect = field.getBoundingClientRect();
    // As wide as the field, but never narrower than the calendar needs.
    calendar.style.width = `${Math.min(Math.max(rect.width, minimumWidth), window.innerWidth - 2 * margin)}px`;

    const height = calendar.offsetHeight;
    const fitsBelow = window.innerHeight - rect.bottom >= height + gap + margin;
    const fitsAbove = rect.top >= height + gap + margin;
    const top = fitsBelow || !fitsAbove ? rect.bottom + gap : rect.top - height - gap;
    const left = Math.min(rect.left, window.innerWidth - calendar.offsetWidth - margin);

    calendar.style.top = `${Math.max(margin, top)}px`;
    calendar.style.left = `${Math.max(margin, left)}px`;
    calendar.style.visibility = 'visible';
}

// Keeps the calendar next to its field while the page or a scrolling parent scrolls.
// The listeners remove themselves once the calendar is closed.
export function follow(field, calendar) {
    const update = () => {
        if (!calendar.isConnected) {
            window.removeEventListener('scroll', update, true);
            window.removeEventListener('resize', update);
            return;
        }

        place(field, calendar);
    };

    // Below the field reads best: scroll the page a little when the calendar does not fit there.
    // In a sticky column the field does not move, and place() then opens it above the field.
    const shortBy = field.getBoundingClientRect().bottom + gap + calendar.offsetHeight + margin - window.innerHeight;
    if (shortBy > 0)
        window.scrollBy({ top: shortBy, behavior: 'instant' });

    window.addEventListener('scroll', update, true);
    window.addEventListener('resize', update);
    place(field, calendar);
}
