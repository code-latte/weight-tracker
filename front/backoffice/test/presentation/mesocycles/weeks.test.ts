import { describe, expect, it } from "vitest";
import type { MesocycleWeek } from "@weight-tracker/core/domain";
import { nextFreeWeekOf, sortedWeeks } from "@/presentation/mesocycles/weeks";

function week(weekNumber: number): MesocycleWeek {
  return { weekNumber, trainingWeekType: "ADAPTATION", objective: `Semana ${weekNumber}` };
}

describe("nextFreeWeekOf", () => {
  it("answers the first gap before the weeks after it", () => {
    // Arrange
    const weeks = [week(1), week(3)];

    // Act
    const result = nextFreeWeekOf(weeks, 4);

    // Assert
    expect(result).toBe(2);
  });

  it("answers week 1 when no week is defined", () => {
    // Arrange / Act
    const result = nextFreeWeekOf([], 4);

    // Assert
    expect(result).toBe(1);
  });

  it("answers null when every week up to the duration is defined", () => {
    // Arrange
    const weeks = [week(2), week(1)];

    // Act
    const result = nextFreeWeekOf(weeks, 2);

    // Assert
    expect(result).toBeNull();
  });

  it("reads week numbers sent as strings", () => {
    // Arrange
    const weeks = [{ ...week(1), weekNumber: "1" }];

    // Act
    const result = nextFreeWeekOf(weeks, 2);

    // Assert
    expect(result).toBe(2);
  });
});

describe("sortedWeeks", () => {
  it("sorts by week number without changing the given list", () => {
    // Arrange
    const weeks = [week(3), week(1), week(2)];

    // Act
    const result = sortedWeeks(weeks);

    // Assert
    expect(result.map((item) => item.weekNumber)).toEqual([1, 2, 3]);
    expect(weeks.map((item) => item.weekNumber)).toEqual([3, 1, 2]);
  });
});
