import { act, renderHook } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import useNow from "@/presentation/workouts/hooks/use-now";

const start = new Date("2026-11-13T10:00:00Z");

describe("useNow", () => {
  beforeEach(() => {
    vi.useFakeTimers();
    vi.setSystemTime(start);
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it("answers the current time on mount", () => {
    // Arrange / Act
    const { result } = renderHook(() => useNow(true));

    // Assert
    expect(result.current).toBe(start.getTime());
  });

  it("answers the new time every interval while live", () => {
    // Arrange
    const { result } = renderHook(() => useNow(true));

    // Act
    act(() => {
      vi.advanceTimersByTime(60_000);
    });

    // Assert
    expect(result.current).toBe(start.getTime() + 60_000);
  });

  it("keeps the time it read on mount when not live", () => {
    // Arrange
    const { result } = renderHook(() => useNow(false));

    // Act
    act(() => {
      vi.advanceTimersByTime(5 * 60_000);
    });

    // Assert
    expect(result.current).toBe(start.getTime());
  });

  it("clears its interval on unmount", () => {
    // Arrange
    const { unmount } = renderHook(() => useNow(true));

    // Act
    unmount();

    // Assert
    expect(vi.getTimerCount()).toBe(0);
  });
});
