/**
 * Weeks run Monday to Sunday in local time. Days are `YYYY-MM-DD` strings, the shape of the API's `date` fields, and
 * are always read as local dates: `new Date("YYYY-MM-DD")` parses as UTC and can move a day.
 */

function toDay(date: Date): string {
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");

  return `${date.getFullYear()}-${month}-${day}`;
}

function toDate(day: string): Date {
  const [year, month, date] = day.split("-").map(Number);

  return new Date(year, month - 1, date);
}

function addDays(day: string, days: number): string {
  const date = toDate(day);
  date.setDate(date.getDate() + days);

  return toDay(date);
}

/** Some engines end abbreviations with a dot (`nov.`); the screens show them without it. */
function withoutDots(text: string): string {
  return text.replace(/\./g, "");
}

function capitalise(text: string): string {
  return text.charAt(0).toUpperCase() + text.slice(1);
}

/** The Monday of the week that contains `date`. */
export function weekStartOf(date: Date): string {
  const daysSinceMonday = (date.getDay() + 6) % 7;

  return addDays(toDay(date), -daysSinceMonday);
}

/** The Monday `weeks` weeks after (or, when negative, before) `weekStart`. */
export function addWeeks(weekStart: string, weeks: number): string {
  return addDays(weekStart, weeks * 7);
}

/** Whether `day` falls between the Monday `weekStart` and its Sunday, both included. */
export function isInWeek(day: string, weekStart: string): boolean {
  return day >= weekStart && day <= addDays(weekStart, 6);
}

/** The week's Monday and Sunday as weekday, day and month: `Lun, 9 nov - Dom, 15 nov`. */
export function formatWeekRange(weekStart: string, language: string): string {
  const format = new Intl.DateTimeFormat(language, {
    weekday: "short",
    day: "numeric",
    month: "short",
  });

  return [weekStart, addDays(weekStart, 6)]
    .map((day) => capitalise(withoutDots(format.format(toDate(day)))))
    .join(" - ");
}

/**
 * The short weekday of an instant, such as the API's `completedAt`, in local time and lower-cased: `mié`. Unlike a
 * `YYYY-MM-DD` day, an ISO date-time carries its offset, so `new Date` reads it right.
 */
export function formatShortWeekday(instant: string, language: string): string {
  const format = new Intl.DateTimeFormat(language, { weekday: "short" });

  return withoutDots(format.format(new Date(instant))).toLowerCase();
}

/** A day as day, short month and year, such as `10 nov 2026`; read as a local date, so it never moves a day. */
export function formatDay(day: string, language: string): string {
  return withoutDots(
    new Intl.DateTimeFormat(language, {
      day: "numeric",
      month: "short",
      year: "numeric",
    }).format(toDate(day)),
  );
}

/** The year of the week's Monday. */
export function weekYear(weekStart: string): string {
  return weekStart.slice(0, 4);
}
