import { config } from "@/config";

export type RegisterLeadFailure = "invalid" | "tooMany" | "network";

export type RegisterLeadResult = { ok: true } | { ok: false; reason: RegisterLeadFailure };

/**
 * Registers an email as a lead through the API's public `POST /customer-leads`. A new and an already known email
 * answer the same. It never throws: every outcome is a result.
 */
export async function registerLead(
  email: string,
  apiUrl = config.apiUrl,
): Promise<RegisterLeadResult> {
  try {
    const response = await fetch(`${apiUrl}/customer-leads`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ email }),
    });

    if (response.status === 204) {
      return { ok: true };
    }
    if (response.status === 400) {
      return { ok: false, reason: "invalid" };
    }
    if (response.status === 429) {
      return { ok: false, reason: "tooMany" };
    }
    return { ok: false, reason: "network" };
  } catch {
    return { ok: false, reason: "network" };
  }
}
