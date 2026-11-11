import { render } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import SectionViews from "@/analytics/section-views";
import { setSink, type TrackedEvent } from "@/analytics/track";

type ObserverCallback = (entries: Partial<IntersectionObserverEntry>[]) => void;

/** A stand-in for the browser's observer that lets the test say which elements came on screen. */
let notify: ObserverCallback = () => {};

class FakeIntersectionObserver {
  constructor(callback: ObserverCallback) {
    notify = callback;
  }
  observe() {}
  unobserve() {}
  disconnect() {}
}

function sectionElement(id: string): HTMLElement {
  const element = document.createElement("section");
  element.dataset.section = id;
  document.body.append(element);
  return element;
}

describe("SectionViews", () => {
  let events: TrackedEvent[];

  beforeEach(() => {
    vi.stubGlobal("IntersectionObserver", FakeIntersectionObserver);
    events = [];
    setSink((event) => events.push(event));
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    setSink(() => {});
    document.body.innerHTML = "";
  });

  it("reports a section the first time it comes on screen, and only once", () => {
    // Arrange
    const faq = sectionElement("faq");
    const ai = sectionElement("ia");
    render(<SectionViews />);

    // Act
    notify([
      { target: faq, isIntersecting: true },
      { target: ai, isIntersecting: false },
      { target: faq, isIntersecting: true },
    ]);

    // Assert
    expect(events).toEqual([expect.objectContaining({ name: "section_view", section: "faq" })]);
  });
});
