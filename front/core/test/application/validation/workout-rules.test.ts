import { describe, expect, it } from "vitest";
import {
  type CreateWorkoutFormValues,
  targetFieldsOf,
  validateCreateWorkoutForm,
  validateExerciseConfiguration,
  validateSetForm,
  validateWorkoutDetailsForm,
} from "../../../src/application";

const valid: CreateWorkoutFormValues = {
  name: "Pierna",
  description: "",
  workoutDate: "2026-11-02",
  userId: "5f1c2f0e-9d5b-4a53-8c7e-0d6f7c1a2b3c",
  trainerId: "",
};

const trainerId = "6a2d3f1e-0e6c-4b64-9d8f-1e7a8d2b3c4d";

describe("validateCreateWorkoutForm", () => {
  it("answers no violation for valid values", () => {
    // Arrange / Act
    const result = validateCreateWorkoutForm(valid, { withTrainer: false });

    // Assert
    expect(result).toEqual({});
  });

  it("reports every empty required field", () => {
    // Arrange
    const values = { name: "", description: "", workoutDate: "", userId: "", trainerId: "" };

    // Act
    const result = validateCreateWorkoutForm(values, { withTrainer: true });

    // Assert
    expect(result).toEqual({
      name: "required",
      workoutDate: "required",
      userId: "required",
      trainerId: "required",
    });
  });

  it.each([
    ["   ", "required"],
    ["a".repeat(256), undefined],
    [`  ${"a".repeat(256)}  `, undefined],
    ["a".repeat(257), "maxLength"],
  ] as const)("answers the name %j with %j", (name, expected) => {
    // Arrange / Act
    const result = validateCreateWorkoutForm({ ...valid, name }, { withTrainer: false });

    // Assert
    expect(result.name).toBe(expected);
  });

  it.each([
    ["a".repeat(1024), undefined],
    ["a".repeat(1025), "maxLength"],
  ] as const)("answers a description of length %#", (description, expected) => {
    // Arrange / Act
    const result = validateCreateWorkoutForm({ ...valid, description }, { withTrainer: false });

    // Assert
    expect(result.description).toBe(expected);
  });

  it.each([
    ["2026-02-28", undefined],
    ["2028-02-29", undefined],
    ["2026-02-30", "date"],
    ["2026-13-01", "date"],
    ["02/11/2026", "date"],
  ] as const)("answers the date %j with %j", (workoutDate, expected) => {
    // Arrange / Act
    const result = validateCreateWorkoutForm({ ...valid, workoutDate }, { withTrainer: false });

    // Assert
    expect(result.workoutDate).toBe(expected);
  });

  it.each([
    ["ana", "uuid"],
    ["5F1C2F0E-9D5B-4A53-8C7E-0D6F7C1A2B3C", undefined],
    ["  5f1c2f0e-9d5b-4a53-8c7e-0d6f7c1a2b3c  ", undefined],
  ] as const)("answers the athlete id %j with %j", (userId, expected) => {
    // Arrange / Act
    const result = validateCreateWorkoutForm({ ...valid, userId }, { withTrainer: false });

    // Assert
    expect(result.userId).toBe(expected);
  });

  it("checks the trainer id only when a trainer is required", () => {
    // Arrange
    const values = { ...valid, trainerId: "ana" };

    // Act
    const [without, withTrainer, validTrainer] = [
      validateCreateWorkoutForm(values, { withTrainer: false }),
      validateCreateWorkoutForm(values, { withTrainer: true }),
      validateCreateWorkoutForm({ ...valid, trainerId }, { withTrainer: true }),
    ];

    // Assert
    expect(without.trainerId).toBeUndefined();
    expect(withTrainer.trainerId).toBe("uuid");
    expect(validTrainer).toEqual({});
  });
});

describe("validateWorkoutDetailsForm", () => {
  it("answers no violation for valid values", () => {
    // Arrange / Act
    const result = validateWorkoutDetailsForm({
      name: "Pierna",
      description: "",
      workoutDate: "2026-11-02",
    });

    // Assert
    expect(result).toEqual({});
  });

  it("reports each field with the same rules as the create form", () => {
    // Arrange
    const values = { name: "   ", description: "a".repeat(1025), workoutDate: "2026-02-30" };

    // Act
    const result = validateWorkoutDetailsForm(values);

    // Assert
    expect(result).toEqual({ name: "required", description: "maxLength", workoutDate: "date" });
  });

  it("reports an empty date as required and a 257-character name as too long", () => {
    // Arrange
    const values = { name: "a".repeat(257), description: "", workoutDate: "" };

    // Act
    const result = validateWorkoutDetailsForm(values);

    // Assert
    expect(result).toEqual({ name: "maxLength", workoutDate: "required" });
  });
});

describe("validateSetForm", () => {
  it.each([
    [{ count: "2", type: "Reps", value: "15", maxValue: "" }, {}],
    [{ count: "1", type: "RepsRange", value: "10", maxValue: "12" }, {}],
    [{ count: "3", type: "Duration", value: "40", maxValue: "" }, {}],
    [{ count: "1", type: "MaxReps", value: "", maxValue: "" }, {}],
    [
      { count: "", type: "Reps", value: "", maxValue: "" },
      { count: "positiveInteger", value: "positiveInteger" },
    ],
    [
      { count: "0", type: "Reps", value: "1.5", maxValue: "" },
      { count: "positiveInteger", value: "positiveInteger" },
    ],
    [{ count: "1", type: "RepsRange", value: "8", maxValue: "" }, { maxValue: "positiveInteger" }],
    [{ count: "3", type: "Minutes", value: "1:30", maxValue: "" }, {}],
    [{ count: "1", type: "Minutes", value: "20", maxValue: "" }, {}],
    [{ count: "1", type: "Minutes", value: "1.5", maxValue: "" }, { value: "minutesFormat" }],
    [{ count: "1", type: "Minutes", value: "1:60", maxValue: "" }, { value: "minutesFormat" }],
    [{ count: "1", type: "Minutes", value: "", maxValue: "" }, { value: "minutesFormat" }],
  ])("answers %j with %j", (values, expected) => {
    // Arrange / Act
    const result = validateSetForm(values);

    // Assert
    expect(result).toEqual(expected);
  });
});

describe("targetFieldsOf", () => {
  it.each([
    ["Reps", { value: true, maxValue: false }],
    ["Duration", { value: true, maxValue: false }],
    ["RepsRange", { value: true, maxValue: true }],
    ["MaxReps", { value: false, maxValue: false }],
    ["Minutes", { value: true, maxValue: false }],
  ])("answers the fields %s needs", (type, expected) => {
    // Arrange / Act
    const result = targetFieldsOf(type);

    // Assert
    expect(result).toEqual(expected);
  });
});

describe("validateExerciseConfiguration", () => {
  it.each([
    [{ effort: "HIGH", setCount: 1 }, {}],
    [
      { effort: "", setCount: 0 },
      { effort: "required", sets: "minItems" },
    ],
    [{ effort: "LOW", setCount: 0 }, { sets: "minItems" }],
    [{ effort: "LOW", setCount: 1, rest: "" }, {}],
    [{ effort: "LOW", setCount: 1, rest: "1:30" }, {}],
    [{ effort: "LOW", setCount: 1, rest: "2" }, {}],
    [{ effort: "LOW", setCount: 1, rest: "1:75" }, { rest: "minutesFormat" }],
    [{ effort: "LOW", setCount: 1, rest: "0" }, { rest: "minutesFormat" }],
  ])("answers %j with %j", (values, expected) => {
    // Arrange / Act
    const result = validateExerciseConfiguration(values);

    // Assert
    expect(result).toEqual(expected);
  });
});
