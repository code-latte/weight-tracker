import { describe, expect, it } from "vitest";
import { type ExerciseFormValues, validateExerciseForm } from "../../../src/application";

const valid: ExerciseFormValues = {
  name: "Remo con banda",
  muscle: "MIDDLE_BACK",
  type: "STRENGTH",
  videoUrl: "",
};

describe("validateExerciseForm", () => {
  it("answers no violation for valid values", () => {
    // Arrange / Act
    const result = validateExerciseForm(valid);

    // Assert
    expect(result).toEqual({});
  });

  it.each(["", "   "])("requires the name (%j)", (name) => {
    // Arrange / Act
    const result = validateExerciseForm({ ...valid, name });

    // Assert
    expect(result).toEqual({ name: "required" });
  });

  it.each(["ab", "  ab  "])("rejects a name under 3 characters once trimmed (%j)", (name) => {
    // Arrange / Act
    const result = validateExerciseForm({ ...valid, name });

    // Assert
    expect(result).toEqual({ name: "minLength" });
  });

  it("rejects a name over 150 characters", () => {
    // Arrange / Act
    const result = validateExerciseForm({ ...valid, name: "a".repeat(151) });

    // Assert
    expect(result).toEqual({ name: "maxLength" });
  });

  it.each([3, 150])("accepts a name of exactly %i characters", (length) => {
    // Arrange / Act
    const result = validateExerciseForm({ ...valid, name: "a".repeat(length) });

    // Assert
    expect(result).toEqual({});
  });

  it("measures the name after trimming", () => {
    // Arrange / Act
    const result = validateExerciseForm({ ...valid, name: ` ${"a".repeat(150)} ` });

    // Assert
    expect(result).toEqual({});
  });

  it("requires the muscle and the type", () => {
    // Arrange / Act
    const result = validateExerciseForm({ ...valid, muscle: "", type: "" });

    // Assert
    expect(result).toEqual({ muscle: "required", type: "required" });
  });

  it("accepts an empty or spaces-only link", () => {
    // Arrange / Act
    const result = validateExerciseForm({ ...valid, videoUrl: "   " });

    // Assert
    expect(result).toEqual({});
  });

  it.each([
    "ftp://example.com/video",
    "example",
    "https://example",
    "https://exa mple.com",
    "www.example.com",
    "no es un enlace",
  ])("rejects the link %j as not a web address", (videoUrl) => {
    // Arrange / Act
    const result = validateExerciseForm({ ...valid, videoUrl });

    // Assert
    expect(result).toEqual({ videoUrl: "url" });
  });

  it.each(["https://www.youtube.com/watch?v=abc", "http://example.com", "  https://example.com  "])(
    "accepts the link %j",
    (videoUrl) => {
      // Arrange / Act
      const result = validateExerciseForm({ ...valid, videoUrl });

      // Assert
      expect(result).toEqual({});
    },
  );

  it("rejects a link over 500 characters", () => {
    // Arrange
    const videoUrl = `https://example.com/${"a".repeat(481)}`;

    // Act
    const result = validateExerciseForm({ ...valid, videoUrl });

    // Assert
    expect(result).toEqual({ videoUrl: "maxLength" });
  });

  it("accepts a link of exactly 500 characters", () => {
    // Arrange
    const videoUrl = `https://example.com/${"a".repeat(480)}`;

    // Act
    const result = validateExerciseForm({ ...valid, videoUrl });

    // Assert
    expect(result).toEqual({});
  });
});
