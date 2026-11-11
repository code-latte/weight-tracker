import { describe, expect, it } from "vitest";
import type { HttpClientInterface, HttpRequest } from "../../../src/application";
import { type CustomerLead, ok } from "../../../src/domain";
import { CustomerLeadRepository, HttpError } from "../../../src/infrastructure";

const newest: CustomerLead = { email: "ana@example.com", registeredAtUtc: "2026-11-10T08:15:00Z" };

const oldest: CustomerLead = { email: "luis@example.com", registeredAtUtc: "2026-11-09T17:40:00Z" };

class FakeHttpClient implements HttpClientInterface {
  readonly requests: HttpRequest[] = [];

  constructor(private readonly respond: () => Promise<unknown>) {}

  async request<T>(httpRequest: HttpRequest): Promise<T> {
    this.requests.push(httpRequest);
    return (await this.respond()) as T;
  }
}

describe("CustomerLeadRepository", () => {
  describe("getAll", () => {
    it("gets /customer-leads without a body", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => [newest]);
      const repository = new CustomerLeadRepository(httpClient);

      // Act
      await repository.getAll();

      // Assert
      expect(httpClient.requests).toEqual([{ method: "GET", path: "/customer-leads" }]);
    });

    it("returns the leads unchanged and in the API's order on success", async () => {
      // Arrange
      const repository = new CustomerLeadRepository(
        new FakeHttpClient(async () => [newest, oldest]),
      );

      // Act
      const result = await repository.getAll();

      // Assert
      expect(result).toEqual(ok([newest, oldest]));
    });

    it("returns an empty list when the API has no leads", async () => {
      // Arrange
      const repository = new CustomerLeadRepository(new FakeHttpClient(async () => []));

      // Act
      const result = await repository.getAll();

      // Assert
      expect(result).toEqual(ok([]));
    });

    it.each([
      [401, "UNAUTHORIZED"],
      [403, "FORBIDDEN"],
      [500, "UNEXPECTED"],
    ])("maps status %i to %s", async (status, code) => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => {
        throw new HttpError(status, { status, detail: "Something failed" });
      });
      const repository = new CustomerLeadRepository(httpClient);

      // Act
      const result = await repository.getAll();

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code }) });
    });

    it("returns the network code when there is no response", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => {
        throw new TypeError("Failed to fetch");
      });
      const repository = new CustomerLeadRepository(httpClient);

      // Act
      const result = await repository.getAll();

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "NETWORK" }) });
    });
  });
});
