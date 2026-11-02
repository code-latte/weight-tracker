import { describe, expect, it } from "vitest";
import { validateInviteAthleteForm } from "../../../src/application";

describe("validateInviteAthleteForm", () => {
  it.each(["", "   "])("reports a missing email (%j)", (email) => {
    // Arrange / Act
    const violations = validateInviteAthleteForm({ email });

    // Assert
    expect(violations).toEqual({ email: "required" });
  });

  it("reports a malformed email", () => {
    // Arrange / Act
    const violations = validateInviteAthleteForm({ email: "ana.example.com" });

    // Assert
    expect(violations).toEqual({ email: "email" });
  });

  it("accepts a valid email with spaces around it", () => {
    // Arrange / Act
    const violations = validateInviteAthleteForm({ email: "  ana@example.com  " });

    // Assert
    expect(violations).toEqual({});
  });
});
