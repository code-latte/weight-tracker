import { describe, expect, it } from "vitest";
import { isMinutesText, minutesFromText, minutesToText } from "../../../src/application";

describe("isMinutesText", () => {
  it.each(["2", "1:30", "0:45", " 1:05 ", "999:59"])("accepts %j", (text) => {
    // Arrange / Act
    const result = isMinutesText(text);

    // Assert
    expect(result).toBe(true);
  });

  it.each(["0", "0:00", "1:60", "1:5", "1.5", "1,5", "-1", "abc", "1000", ""])(
    "rejects %j",
    (text) => {
      // Arrange / Act
      const result = isMinutesText(text);

      // Assert
      expect(result).toBe(false);
    },
  );
});

describe("minutesFromText", () => {
  it.each([
    ["2", 2],
    ["1:30", 1.5],
    ["0:45", 0.75],
    ["1:20", 1.33],
    [" 1:05 ", 1.08],
  ] as const)("converts %j to %j", (text, expected) => {
    // Arrange / Act
    const result = minutesFromText(text);

    // Assert
    expect(result).toBe(expected);
  });
});

describe("minutesToText", () => {
  it.each([
    [1, "1"],
    [1.5, "1:30"],
    [0.75, "0:45"],
    [1.33, "1:20"],
    ["1.5", "1:30"],
    [999.99, "999:59"],
  ] as const)("shows %j as %j", (minutes, expected) => {
    // Arrange / Act
    const result = minutesToText(minutes);

    // Assert
    expect(result).toBe(expected);
  });

  it("gives back every second from 0:01 to 2:00 as typed", () => {
    // Arrange
    const texts = Array.from({ length: 120 }, (_, index) => {
      const seconds = index + 1;
      const rest = seconds % 60;
      return rest === 0
        ? String(seconds / 60)
        : `${Math.floor(seconds / 60)}:${String(rest).padStart(2, "0")}`;
    });

    // Act
    const result = texts.map((text) => minutesToText(minutesFromText(text)));

    // Assert
    expect(result).toEqual(texts);
  });
});
