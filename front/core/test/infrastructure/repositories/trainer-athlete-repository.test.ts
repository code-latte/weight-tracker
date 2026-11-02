import { describe, expect, it } from "vitest";
import type { HttpClientInterface, HttpRequest } from "../../../src/application";
import {
  ok,
  type TrainerAthlete,
  type TrainerAthleteRelationship,
  type TrainerAthleteRequest,
} from "../../../src/domain";
import { HttpError, TrainerAthleteRepository } from "../../../src/infrastructure";

const trainerId = "3c4d5e6f-7a8b-4c9d-8e0f-1a2b3c4d5e6f";

const athlete: TrainerAthlete = {
  id: "1b2c3d4e-5f6a-4b7c-8d9e-0f1a2b3c4d5e",
  athleteId: "9a8b7c6d-5e4f-4a3b-8c2d-1e0f9a8b7c6d",
  email: "luis@example.com",
  name: "Luis",
  surname: "Pérez",
  trainerName: "Ana",
  trainerSurname: "García",
  startedAtUtc: "2026-10-03T08:00:00Z",
};

const relationship: TrainerAthleteRelationship = {
  id: athlete.id,
  trainerId,
  trainerName: "Ana",
  trainerSurname: "García",
  athleteId: athlete.athleteId,
  athleteName: "Luis",
  athleteSurname: "Pérez",
  status: "ACTIVE",
  startedAtUtc: "2026-10-03T08:00:00Z",
  finishedAtUtc: null,
};

const request: TrainerAthleteRequest = { athleteId: athlete.athleteId };

class FakeHttpClient implements HttpClientInterface {
  readonly requests: HttpRequest[] = [];

  constructor(private readonly respond: () => Promise<unknown>) {}

  async request<T>(httpRequest: HttpRequest): Promise<T> {
    this.requests.push(httpRequest);
    return (await this.respond()) as T;
  }
}

describe("TrainerAthleteRepository", () => {
  describe("getMine", () => {
    it("sends GET /trainer-athletes without a body", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => [athlete]);
      const repository = new TrainerAthleteRepository(httpClient);

      // Act
      await repository.getMine();

      // Assert
      expect(httpClient.requests).toEqual([{ method: "GET", path: "/trainer-athletes" }]);
    });

    it("returns what the API answers on success", async () => {
      // Arrange
      const repository = new TrainerAthleteRepository(new FakeHttpClient(async () => [athlete]));

      // Act
      const result = await repository.getMine();

      // Assert
      expect(result).toEqual(ok([athlete]));
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
      const repository = new TrainerAthleteRepository(httpClient);

      // Act
      const result = await repository.getMine();

      // Assert
      expect(result).toEqual({
        ok: false,
        error: expect.objectContaining({ code, message: "Something failed" }),
      });
    });

    it("returns the network code when there is no response", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => {
        throw new TypeError("Failed to fetch");
      });
      const repository = new TrainerAthleteRepository(httpClient);

      // Act
      const result = await repository.getMine();

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "NETWORK" }) });
    });
  });

  describe("getByTrainer", () => {
    it("sends GET /trainer-athletes/{trainerId} without a body", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => [athlete]);
      const repository = new TrainerAthleteRepository(httpClient);

      // Act
      await repository.getByTrainer(trainerId);

      // Assert
      expect(httpClient.requests).toEqual([
        { method: "GET", path: `/trainer-athletes/${trainerId}` },
      ]);
    });

    it("returns what the API answers on success", async () => {
      // Arrange
      const repository = new TrainerAthleteRepository(new FakeHttpClient(async () => [athlete]));

      // Act
      const result = await repository.getByTrainer(trainerId);

      // Assert
      expect(result).toEqual(ok([athlete]));
    });

    it.each([
      [401, "UNAUTHORIZED"],
      [403, "FORBIDDEN"],
      [404, "NOT_FOUND"],
      [500, "UNEXPECTED"],
    ])("maps status %i to %s", async (status, code) => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => {
        throw new HttpError(status, { status, detail: "Something failed" });
      });
      const repository = new TrainerAthleteRepository(httpClient);

      // Act
      const result = await repository.getByTrainer(trainerId);

      // Assert
      expect(result).toEqual({
        ok: false,
        error: expect.objectContaining({ code, message: "Something failed" }),
      });
    });

    it("returns the network code when there is no response", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => {
        throw new TypeError("Failed to fetch");
      });
      const repository = new TrainerAthleteRepository(httpClient);

      // Act
      const result = await repository.getByTrainer(trainerId);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "NETWORK" }) });
    });
  });

  describe("link", () => {
    it("sends POST /trainer-athletes/{trainerId} with the athlete id", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => relationship);
      const repository = new TrainerAthleteRepository(httpClient);

      // Act
      await repository.link(trainerId, request);

      // Assert
      expect(httpClient.requests).toEqual([
        {
          method: "POST",
          path: `/trainer-athletes/${trainerId}`,
          body: { athleteId: athlete.athleteId },
        },
      ]);
    });

    it("returns what the API answers on success", async () => {
      // Arrange
      const repository = new TrainerAthleteRepository(new FakeHttpClient(async () => relationship));

      // Act
      const result = await repository.link(trainerId, request);

      // Assert
      expect(result).toEqual(ok(relationship));
    });

    it.each([
      [400, "VALIDATION"],
      [401, "UNAUTHORIZED"],
      [403, "FORBIDDEN"],
      [404, "NOT_FOUND"],
      [409, "CONFLICT"],
      [500, "UNEXPECTED"],
    ])("maps status %i to %s", async (status, code) => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => {
        throw new HttpError(status, { status, detail: "Something failed" });
      });
      const repository = new TrainerAthleteRepository(httpClient);

      // Act
      const result = await repository.link(trainerId, request);

      // Assert
      expect(result).toEqual({
        ok: false,
        error: expect.objectContaining({ code, message: "Something failed" }),
      });
    });

    it("returns the network code when there is no response", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => {
        throw new TypeError("Failed to fetch");
      });
      const repository = new TrainerAthleteRepository(httpClient);

      // Act
      const result = await repository.link(trainerId, request);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "NETWORK" }) });
    });
  });

  describe("end", () => {
    it("sends PUT /trainer-athletes/{trainerId}/end with the athlete id", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => relationship);
      const repository = new TrainerAthleteRepository(httpClient);

      // Act
      await repository.end(trainerId, request);

      // Assert
      expect(httpClient.requests).toEqual([
        {
          method: "PUT",
          path: `/trainer-athletes/${trainerId}/end`,
          body: { athleteId: athlete.athleteId },
        },
      ]);
    });

    it("returns what the API answers on success", async () => {
      // Arrange
      const repository = new TrainerAthleteRepository(new FakeHttpClient(async () => relationship));

      // Act
      const result = await repository.end(trainerId, request);

      // Assert
      expect(result).toEqual(ok(relationship));
    });

    it.each([
      [400, "VALIDATION"],
      [401, "UNAUTHORIZED"],
      [403, "FORBIDDEN"],
      [404, "NOT_FOUND"],
      [500, "UNEXPECTED"],
    ])("maps status %i to %s", async (status, code) => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => {
        throw new HttpError(status, { status, detail: "Something failed" });
      });
      const repository = new TrainerAthleteRepository(httpClient);

      // Act
      const result = await repository.end(trainerId, request);

      // Assert
      expect(result).toEqual({
        ok: false,
        error: expect.objectContaining({ code, message: "Something failed" }),
      });
    });

    it("returns the network code when there is no response", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => {
        throw new TypeError("Failed to fetch");
      });
      const repository = new TrainerAthleteRepository(httpClient);

      // Act
      const result = await repository.end(trainerId, request);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "NETWORK" }) });
    });
  });
});
