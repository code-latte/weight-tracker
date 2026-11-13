import { effortKeysOf } from "@/presentation/workouts/effort";

describe("effortKeysOf", () => {
  it.each(["STRENGTH", "CARDIO", "STRETCHING"])(
    "reads the scale of a %s exercise before the generic one",
    (type) => {
      // Arrange
      const code = "HIGH";

      // Act
      const keys = effortKeysOf(code, type);

      // Assert
      expect(keys).toEqual([`effortsByType.${type}.HIGH`, "efforts.HIGH"]);
    },
  );

  it("reads only the generic scale when the exercise has no type", () => {
    // Arrange
    const code = "MID";

    // Act
    const keys = effortKeysOf(code);

    // Assert
    expect(keys).toEqual(["efforts.MID"]);
  });

  it("reads only the generic scale for a type without a scale of its own", () => {
    // Arrange
    const code = "MIDLOW";

    // Act
    const keys = effortKeysOf(code, "OTHER");

    // Assert
    expect(keys).toEqual(["efforts.MIDLOW"]);
  });

  it("does not mistake an object property for a type", () => {
    // Arrange
    const code = "LOW";

    // Act
    const keys = effortKeysOf(code, "constructor");

    // Assert
    expect(keys).toEqual(["efforts.LOW"]);
  });
});
