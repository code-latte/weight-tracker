import { describe, expect, it } from "vitest";
import {
  type CreateMesocycleFormValues,
  validateCreateMesocycleForm,
  validateMesocycleDetailsForm,
  validateMesocycleWeekForm,
} from "../../../src/application";

const valid: CreateMesocycleFormValues = {
  name: "Hipertrofia",
  objective: "Ganar masa muscular",
  startDate: "2026-11-03",
  durationInWeeks: "4",
  athleteId: "5f1c2f0e-9d5b-4a53-8c7e-0d6f7c1a2b3c",
  trainerId: "",
};

const trainerId = "6a2d3f1e-0e6c-4b64-9d8f-1e7a8d2b3c4d";

describe("validateCreateMesocycleForm", () => {
  it("answers no violation for valid values", () => {
    // Arrange / Act
    const result = validateCreateMesocycleForm(valid, { withTrainer: false });

    // Assert
    expect(result).toEqual({});
  });

  it("reports every empty required field", () => {
    // Arrange
    const values = {
      name: "",
      objective: "",
      startDate: "",
      durationInWeeks: "",
      athleteId: "",
      trainerId: "",
    };

    // Act
    const result = validateCreateMesocycleForm(values, { withTrainer: true });

    // Assert
    expect(result).toEqual({
      name: "required",
      objective: "required",
      startDate: "required",
      durationInWeeks: "required",
      athleteId: "required",
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
    const result = validateCreateMesocycleForm({ ...valid, name }, { withTrainer: false });

    // Assert
    expect(result.name).toBe(expected);
  });

  it.each([
    ["   ", "required"],
    ["a".repeat(4000), undefined],
    [`  ${"a".repeat(4000)}  `, undefined],
    ["a".repeat(4001), "maxLength"],
  ] as const)("answers an objective of case %# with %j", (objective, expected) => {
    // Arrange / Act
    const result = validateCreateMesocycleForm({ ...valid, objective }, { withTrainer: false });

    // Assert
    expect(result.objective).toBe(expected);
  });

  it.each([
    ["2026-02-30", "date"],
    ["03/11/2026", "date"],
    ["2026-11-03", undefined],
  ] as const)("answers the start date %j with %j", (startDate, expected) => {
    // Arrange / Act
    const result = validateCreateMesocycleForm({ ...valid, startDate }, { withTrainer: false });

    // Assert
    expect(result.startDate).toBe(expected);
  });

  it.each([
    ["0", "range"],
    ["1", undefined],
    ["52", undefined],
    ["53", "range"],
    ["2.5", "range"],
    ["abc", "range"],
  ] as const)("answers the duration %j with %j", (durationInWeeks, expected) => {
    // Arrange / Act
    const result = validateCreateMesocycleForm(
      { ...valid, durationInWeeks },
      { withTrainer: false },
    );

    // Assert
    expect(result.durationInWeeks).toBe(expected);
  });

  it("answers an athlete that is not an id with uuid", () => {
    // Arrange / Act
    const result = validateCreateMesocycleForm(
      { ...valid, athleteId: "ana" },
      { withTrainer: false },
    );

    // Assert
    expect(result.athleteId).toBe("uuid");
  });

  it("ignores the trainer when no trainer is required", () => {
    // Arrange / Act
    const result = validateCreateMesocycleForm(
      { ...valid, trainerId: "luis" },
      { withTrainer: false },
    );

    // Assert
    expect(result.trainerId).toBeUndefined();
  });

  it("accepts a trainer id when a trainer is required", () => {
    // Arrange / Act
    const result = validateCreateMesocycleForm({ ...valid, trainerId }, { withTrainer: true });

    // Assert
    expect(result).toEqual({});
  });
});

describe("validateMesocycleDetailsForm", () => {
  it("does not check who the mesocycle is for", () => {
    // Arrange
    const { name, objective, startDate, durationInWeeks } = valid;

    // Act
    const result = validateMesocycleDetailsForm({ name, objective, startDate, durationInWeeks });

    // Assert
    expect(result).toEqual({});
  });
});

describe("validateMesocycleWeekForm", () => {
  it("answers no violation for a type and an objective", () => {
    // Arrange / Act
    const result = validateMesocycleWeekForm({
      trainingWeekType: "ADAPTATION",
      objective: "Cargas iniciales",
    });

    // Assert
    expect(result).toEqual({});
  });

  it("reports a missing type and a blank objective", () => {
    // Arrange / Act
    const result = validateMesocycleWeekForm({ trainingWeekType: "", objective: "   " });

    // Assert
    expect(result).toEqual({ trainingWeekType: "required", objective: "required" });
  });

  it("reports an objective longer than the limit", () => {
    // Arrange / Act
    const result = validateMesocycleWeekForm({
      trainingWeekType: "DELOAD",
      objective: "a".repeat(4001),
    });

    // Assert
    expect(result.objective).toBe("maxLength");
  });
});
