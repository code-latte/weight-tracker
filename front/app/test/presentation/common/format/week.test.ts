import {
  addWeeks,
  formatDay,
  formatShortWeekday,
  formatWeekRange,
  isInWeek,
  weekStartOf,
  weekYear,
} from "@/presentation/common/format/week";

describe("weekStartOf", () => {
  it("keeps a Monday", () => {
    // Arrange
    const monday = new Date(2026, 10, 9);

    // Act
    const weekStart = weekStartOf(monday);

    // Assert
    expect(weekStart).toBe("2026-11-09");
  });

  it("goes back to the Monday from a Sunday", () => {
    // Arrange
    const sunday = new Date(2026, 10, 15);

    // Act
    const weekStart = weekStartOf(sunday);

    // Assert
    expect(weekStart).toBe("2026-11-09");
  });

  it("crosses the year boundary", () => {
    // Arrange
    const newYearsDay = new Date(2026, 0, 1);

    // Act
    const weekStart = weekStartOf(newYearsDay);

    // Assert
    expect(weekStart).toBe("2025-12-29");
  });
});

describe("addWeeks", () => {
  it.each([
    [1, "2026-11-16"],
    [-1, "2026-11-02"],
    [-260, "2021-11-15"],
  ])("moves %i weeks", (weeks, expected) => {
    // Arrange
    const weekStart = "2026-11-09";

    // Act
    const moved = addWeeks(weekStart, weeks);

    // Assert
    expect(moved).toBe(expected);
  });
});

describe("isInWeek", () => {
  it.each([
    ["2026-11-08", false],
    ["2026-11-09", true],
    ["2026-11-15", true],
    ["2026-11-16", false],
  ])("tells whether %s is in the week of 9 Nov 2026", (day, expected) => {
    // Arrange
    const weekStart = "2026-11-09";

    // Act
    const inWeek = isInWeek(day, weekStart);

    // Assert
    expect(inWeek).toBe(expected);
  });
});

describe("formatWeekRange", () => {
  it.each([
    ["es", "Lun, 9 nov - Dom, 15 nov"],
    ["en", "Mon, Nov 9 - Sun, Nov 15"],
  ])("formats the range in %s", (language, expected) => {
    // Arrange
    const weekStart = "2026-11-09";

    // Act
    const range = formatWeekRange(weekStart, language);

    // Assert
    expect(range).toBe(expected);
  });

  it("formats a range that crosses the year", () => {
    // Arrange
    const weekStart = "2025-12-29";

    // Act
    const range = formatWeekRange(weekStart, "es");

    // Assert
    expect(range).toBe("Lun, 29 dic - Dom, 4 ene");
  });
});

describe("formatShortWeekday", () => {
  it.each([
    ["es", "mié"],
    ["en", "wed"],
  ])("formats the weekday in %s, lower-cased and without a dot", (language, expected) => {
    // Arrange
    // Late on Wednesday in local time, whatever the machine's zone; as UTC it may already be Thursday.
    const wednesday = new Date(2026, 10, 11, 23, 30).toISOString();

    // Act
    const weekday = formatShortWeekday(wednesday, language);

    // Assert
    expect(weekday).toBe(expected);
  });

  it("reads the instant in local time", () => {
    // Arrange
    const earlyMonday = new Date(2026, 10, 9, 0, 15).toISOString();

    // Act
    const weekday = formatShortWeekday(earlyMonday, "es");

    // Assert
    expect(weekday).toBe("lun");
  });
});

describe("weekYear", () => {
  it("is the year of the Monday", () => {
    // Arrange
    const weekStart = "2025-12-29";

    // Act
    const year = weekYear(weekStart);

    // Assert
    expect(year).toBe("2025");
  });
});

describe("formatDay", () => {
  it.each([
    ["es", "10 nov 2026"],
    ["en", "Nov 10, 2026"],
  ])("formats the day in %s", (language, expected) => {
    // Arrange
    const day = "2026-11-10";

    // Act
    const formatted = formatDay(day, language);

    // Assert
    expect(formatted).toBe(expected);
  });

  it("keeps the first day of a year in the same day", () => {
    // Arrange
    const day = "2026-01-01";

    // Act
    const formatted = formatDay(day, "es");

    // Assert
    expect(formatted).toBe("1 ene 2026");
  });
});
