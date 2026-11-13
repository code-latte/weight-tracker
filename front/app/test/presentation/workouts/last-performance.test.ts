import type { LastPerformanceSet } from "@weight-tracker/core/domain";
import { hasWeightIn, highestWeightIndexOf } from "@/presentation/workouts/last-performance";

const set = (weight: number | null): LastPerformanceSet => ({
  reps: 10,
  durationSeconds: null,
  weight,
});

describe("highestWeightIndexOf", () => {
  it("returns the position of the set with the highest weight", () => {
    // Arrange
    const sets = [set(20), set(30), set(25)];

    // Act
    const index = highestWeightIndexOf(sets);

    // Assert
    expect(index).toBe(1);
  });

  it("returns the last set when the highest weight is repeated", () => {
    // Arrange
    const sets = [set(30), set(20), set(30), set(30), set(10)];

    // Act
    const index = highestWeightIndexOf(sets);

    // Assert
    expect(index).toBe(3);
  });

  it("returns the only set that has a weight", () => {
    // Arrange
    const sets = [set(null), set(15), set(null)];

    // Act
    const index = highestWeightIndexOf(sets);

    // Assert
    expect(index).toBe(1);
  });

  it("returns null when no set has a weight", () => {
    // Arrange
    const sets = [set(null), set(null)];

    // Act
    const index = highestWeightIndexOf(sets);

    // Assert
    expect(index).toBeNull();
  });

  it("returns null when the highest weight is 0", () => {
    // Arrange
    const sets = [set(0), set(null), set(0)];

    // Act
    const index = highestWeightIndexOf(sets);

    // Assert
    expect(index).toBeNull();
  });

  it("returns null for no sets", () => {
    // Arrange
    const sets: LastPerformanceSet[] = [];

    // Act
    const index = highestWeightIndexOf(sets);

    // Assert
    expect(index).toBeNull();
  });

  it("reads a weight that arrives as a string", () => {
    // Arrange
    const sets = [{ ...set(null), weight: "62.5" }, set(40)] as LastPerformanceSet[];

    // Act
    const index = highestWeightIndexOf(sets);

    // Assert
    expect(index).toBe(0);
  });
});

describe("hasWeightIn", () => {
  it("is true when only one of three sets has a weight", () => {
    // Arrange
    const sets = [set(null), set(40), set(null)];

    // Act
    const hasWeight = hasWeightIn(sets);

    // Assert
    expect(hasWeight).toBe(true);
  });

  it("is false when no set has a weight", () => {
    // Arrange
    const sets = [set(null), set(null)];

    // Act
    const hasWeight = hasWeightIn(sets);

    // Assert
    expect(hasWeight).toBe(false);
  });

  it("counts a weight of 0 as a weight", () => {
    // Arrange
    const sets = [set(null), set(0)];

    // Act
    const hasWeight = hasWeightIn(sets);

    // Assert
    expect(hasWeight).toBe(true);
  });

  it("is false for no sets", () => {
    // Arrange
    const sets: LastPerformanceSet[] = [];

    // Act
    const hasWeight = hasWeightIn(sets);

    // Assert
    expect(hasWeight).toBe(false);
  });
});
