import { describe, expect, it } from "vitest";
import { fullNameOf } from "@/presentation/common/full-name";

describe("fullNameOf", () => {
  it("joins name and surname", () => {
    // Arrange / Act
    const result = fullNameOf({ name: "Ana", surname: "García" });

    // Assert
    expect(result).toBe("Ana García");
  });

  it("keeps the part that is present", () => {
    // Arrange / Act
    const result = fullNameOf({ name: "", surname: "García" });

    // Assert
    expect(result).toBe("García");
  });

  it("answers null when both are empty", () => {
    // Arrange / Act
    const result = fullNameOf({ name: "", surname: "" });

    // Assert
    expect(result).toBeNull();
  });
});
