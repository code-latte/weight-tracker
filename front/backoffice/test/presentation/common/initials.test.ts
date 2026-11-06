import { describe, expect, it } from "vitest";
import { initialsOf } from "@/presentation/common/initials";

describe("initialsOf", () => {
  it("takes the first letter of the name and of the surname", () => {
    // Arrange / Act
    const result = initialsOf({ name: "ana", surname: "garcía", email: "ana@example.com" });

    // Assert
    expect(result).toBe("AG");
  });

  it("keeps the part that is present", () => {
    // Arrange / Act
    const result = initialsOf({ name: " Ana ", surname: "", email: "ana@example.com" });

    // Assert
    expect(result).toBe("A");
  });

  it("falls back to the email when the person has no name", () => {
    // Arrange / Act
    const result = initialsOf({ name: " ", surname: "", email: "bea@example.com" });

    // Assert
    expect(result).toBe("B");
  });
});
