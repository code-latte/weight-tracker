const MINUTES_TEXT_PATTERN = /^(\d+)(?::([0-5]\d))?$/;

/** The longest time the API stores, `999.99` minutes, as the longest `M:SS` that fits in it. */
export const MINUTES_TEXT_MAX_SECONDS = 999 * 60 + 59;

function secondsOf(text: string): number | null {
  const match = MINUTES_TEXT_PATTERN.exec(text.trim());
  if (match === null) {
    return null;
  }
  return Number(match[1]) * 60 + Number(match[2] ?? 0);
}

/** A time typed as whole minutes (`2`) or minutes and two-digit seconds (`1:30`), above zero and up to `999:59`. */
export function isMinutesText(text: string): boolean {
  const seconds = secondsOf(text);

  return seconds !== null && seconds > 0 && seconds <= MINUTES_TEXT_MAX_SECONDS;
}

/** A valid `M` or `M:SS` as minutes, rounded to the two decimals the API stores: `1:30` is `1.5`, `1:20` is `1.33`. */
export function minutesFromText(text: string): number {
  return Math.round(((secondsOf(text) ?? 0) / 60) * 100) / 100;
}

/**
 * Minutes as `M`, or `M:SS` when there are seconds, rounded to the nearest second: `1.5` is `1:30`, `1.33` is `1:20`.
 * Takes the API's value as it comes, number or text.
 */
export function minutesToText(minutes: number | string): string {
  const seconds = Math.round(Number(minutes) * 60);
  const rest = seconds % 60;

  return rest === 0
    ? String(seconds / 60)
    : `${Math.floor(seconds / 60)}:${String(rest).padStart(2, "0")}`;
}
