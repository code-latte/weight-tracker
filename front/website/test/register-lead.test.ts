import { afterEach, describe, expect, it, vi } from "vitest";
import { registerLead } from "@/leads/register-lead";

function answer(status: number) {
  return vi.fn(async () => new Response(null, { status }));
}

describe("registerLead", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("posts the email to the customer leads endpoint", async () => {
    // Arrange
    const fetch = answer(204);
    vi.stubGlobal("fetch", fetch);

    // Act
    await registerLead("ana@example.com", "https://api.example.com");

    // Assert
    expect(fetch).toHaveBeenCalledWith("https://api.example.com/customer-leads", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ email: "ana@example.com" }),
    });
  });

  it("succeeds on 204", async () => {
    // Arrange
    vi.stubGlobal("fetch", answer(204));

    // Act
    const result = await registerLead("ana@example.com");

    // Assert
    expect(result).toEqual({ ok: true });
  });

  it("reports an invalid email on 400", async () => {
    // Arrange
    vi.stubGlobal("fetch", answer(400));

    // Act
    const result = await registerLead("ana@");

    // Assert
    expect(result).toEqual({ ok: false, reason: "invalid" });
  });

  it("reports too many requests on 429", async () => {
    // Arrange
    vi.stubGlobal("fetch", answer(429));

    // Act
    const result = await registerLead("ana@example.com");

    // Assert
    expect(result).toEqual({ ok: false, reason: "tooMany" });
  });

  it("reports a network failure for any other status", async () => {
    // Arrange
    vi.stubGlobal("fetch", answer(500));

    // Act
    const result = await registerLead("ana@example.com");

    // Assert
    expect(result).toEqual({ ok: false, reason: "network" });
  });

  it("reports a network failure when the request gets no answer", async () => {
    // Arrange
    vi.stubGlobal(
      "fetch",
      vi.fn(async () => {
        throw new TypeError("Failed to fetch");
      }),
    );

    // Act
    const result = await registerLead("ana@example.com");

    // Assert
    expect(result).toEqual({ ok: false, reason: "network" });
  });
});
