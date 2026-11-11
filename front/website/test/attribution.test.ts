import { afterEach, describe, expect, it, vi } from "vitest";
import { attributionOf, resetAttribution } from "@/analytics/attribution";
import { setSink, track, type TrackedEvent } from "@/analytics/track";

describe("attributionOf", () => {
  it("reads the UTM parameters and the referrer", () => {
    // Arrange
    const search = "?utm_source=instagram&utm_medium=social&utm_campaign=founders&other=1";

    // Act
    const attribution = attributionOf(search, "https://l.instagram.com/");

    // Assert
    expect(attribution).toEqual({
      utm_source: "instagram",
      utm_medium: "social",
      utm_campaign: "founders",
      referrer: "https://l.instagram.com/",
    });
  });

  it("leaves out empty parameters and a missing referrer", () => {
    // Arrange
    const search = "?utm_source=&utm_term=%20";

    // Act
    const attribution = attributionOf(search, "");

    // Assert
    expect(attribution).toEqual({});
  });
});

describe("track", () => {
  afterEach(() => {
    resetAttribution();
    window.history.replaceState(null, "", "/");
  });

  it("sends the event with the visit's attribution", () => {
    // Arrange
    window.history.replaceState(null, "", "/?utm_source=linkedin");
    const events: TrackedEvent[] = [];
    const previous = setSink((event) => events.push(event));

    // Act
    track({ name: "cta_click", location: "hero" });

    // Assert
    setSink(previous);
    expect(events).toEqual([{ name: "cta_click", location: "hero", utm_source: "linkedin" }]);
  });

  it("sets no cookie", () => {
    // Arrange
    const cookie = vi.spyOn(document, "cookie", "set");

    // Act
    track({ name: "form_start" });

    // Assert
    expect(cookie).not.toHaveBeenCalled();
  });
});
