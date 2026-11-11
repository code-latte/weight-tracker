import { describe, expect, it } from "vitest";
import { baseUrlOf, launchStateOf } from "@/config";

describe("launchStateOf", () => {
  it("is prelaunch when the setting is missing", () => {
    // Arrange
    const value = undefined;

    // Act
    const state = launchStateOf(value);

    // Assert
    expect(state).toBe("prelaunch");
  });

  it("is prelaunch for a state the content does not know", () => {
    // Arrange
    const value = "available";

    // Act
    const state = launchStateOf(value);

    // Assert
    expect(state).toBe("prelaunch");
  });

  it("keeps a known state", () => {
    // Arrange
    const value = "prelaunch";

    // Act
    const state = launchStateOf(value);

    // Assert
    expect(state).toBe("prelaunch");
  });
});

describe("baseUrlOf", () => {
  it("drops the trailing slashes of a given address", () => {
    // Arrange
    const value = "https://api.trainera.app//";

    // Act
    const url = baseUrlOf(value, "http://localhost:5199");

    // Assert
    expect(url).toBe("https://api.trainera.app");
  });

  it("falls back for a missing or blank address", () => {
    // Arrange
    const values = [undefined, "  "];

    // Act
    const urls = values.map((value) => baseUrlOf(value, "http://localhost:5199"));

    // Assert
    expect(urls).toEqual(["http://localhost:5199", "http://localhost:5199"]);
  });
});
