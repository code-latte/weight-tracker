import { describe, expect, it } from "vitest";
import { baseUrlOf, launchStateOf, optionalSettingOf } from "@/config";

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

describe("optionalSettingOf", () => {
  it("is null when the setting is missing", () => {
    // Arrange
    const value = undefined;

    // Act
    const setting = optionalSettingOf(value);

    // Assert
    expect(setting).toBeNull();
  });

  it("is null for a blank setting", () => {
    // Arrange
    const value = "   ";

    // Act
    const setting = optionalSettingOf(value);

    // Assert
    expect(setting).toBeNull();
  });

  it("keeps a value, trimmed", () => {
    // Arrange
    const value = " 5bbf7ae5-6f1a-422a-be81-5ed8552043e3 ";

    // Act
    const setting = optionalSettingOf(value);

    // Assert
    expect(setting).toBe("5bbf7ae5-6f1a-422a-be81-5ed8552043e3");
  });
});
