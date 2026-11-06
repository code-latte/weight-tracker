import { describe, expect, it } from "vitest";
import {
  type CreateWorkoutTemplateFormValues,
  validateCreateWorkoutTemplateForm,
  validateWorkoutTemplateDetailsForm,
  WORKOUT_TEMPLATE_DESCRIPTION_MAX_LENGTH,
  WORKOUT_TEMPLATE_NAME_MAX_LENGTH,
} from "../../../src/application";

const valid: CreateWorkoutTemplateFormValues = {
  name: "Pierna A",
  description: "",
  trainerId: "",
};

describe("validateWorkoutTemplateDetailsForm", () => {
  it("answers no violation for valid values", () => {
    // Arrange / Act
    const result = validateWorkoutTemplateDetailsForm(valid);

    // Assert
    expect(result).toEqual({});
  });

  it("requires a name that is not blank once trimmed", () => {
    // Arrange
    const values = { ...valid, name: "   " };

    // Act
    const result = validateWorkoutTemplateDetailsForm(values);

    // Assert
    expect(result).toEqual({ name: "required" });
  });

  it("accepts a trimmed name at the limit and rejects one above it", () => {
    // Arrange
    const atLimit = { ...valid, name: ` ${"a".repeat(WORKOUT_TEMPLATE_NAME_MAX_LENGTH)} ` };
    const aboveLimit = { ...valid, name: "a".repeat(WORKOUT_TEMPLATE_NAME_MAX_LENGTH + 1) };

    // Act
    const results = [atLimit, aboveLimit].map(validateWorkoutTemplateDetailsForm);

    // Assert
    expect(results).toEqual([{}, { name: "maxLength" }]);
  });

  it("accepts a trimmed description at the limit and rejects one above it", () => {
    // Arrange
    const atLimit = {
      ...valid,
      description: ` ${"a".repeat(WORKOUT_TEMPLATE_DESCRIPTION_MAX_LENGTH)} `,
    };
    const aboveLimit = {
      ...valid,
      description: "a".repeat(WORKOUT_TEMPLATE_DESCRIPTION_MAX_LENGTH + 1),
    };

    // Act
    const results = [atLimit, aboveLimit].map(validateWorkoutTemplateDetailsForm);

    // Assert
    expect(results).toEqual([{}, { description: "maxLength" }]);
  });
});

describe("validateCreateWorkoutTemplateForm", () => {
  it("does not ask for a trainer when none is required", () => {
    // Arrange / Act
    const result = validateCreateWorkoutTemplateForm(valid, { withTrainer: false });

    // Assert
    expect(result).toEqual({});
  });

  it("requires a trainer when one is required", () => {
    // Arrange / Act
    const result = validateCreateWorkoutTemplateForm(valid, { withTrainer: true });

    // Assert
    expect(result).toEqual({ trainerId: "required" });
  });
});
