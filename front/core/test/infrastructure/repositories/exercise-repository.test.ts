import { describe, expect, it } from "vitest";
import type { HttpClientInterface, HttpRequest } from "../../../src/application";
import { ok, type CreateExerciseRequest, type Exercise } from "../../../src/domain";
import { ExerciseRepository, HttpError } from "../../../src/infrastructure";

const exercise: Exercise = {
  id: "1f2e3d4c-5b6a-4978-8695-a4b3c2d1e0f9",
  freeExerciseDbId: "Barbell_Squat",
  videoUrl: null,
  muscle: "quadriceps",
  bodyPart: "LEGS",
  type: "STRENGTH",
  translations: [],
  ownerId: null,
  archivedAtUtc: null,
};

class FakeHttpClient implements HttpClientInterface {
  readonly requests: HttpRequest[] = [];

  constructor(private readonly respond: () => Promise<unknown>) {}

  async request<T>(httpRequest: HttpRequest): Promise<T> {
    this.requests.push(httpRequest);
    return (await this.respond()) as T;
  }
}

describe("ExerciseRepository", () => {
  describe("getAll", () => {
    it("gets /exercises without a body", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => [exercise]);
      const repository = new ExerciseRepository(httpClient);

      // Act
      await repository.getAll();

      // Assert
      expect(httpClient.requests).toEqual([{ method: "GET", path: "/exercises" }]);
    });

    it("returns the exercises on success", async () => {
      // Arrange
      const repository = new ExerciseRepository(new FakeHttpClient(async () => [exercise]));

      // Act
      const result = await repository.getAll();

      // Assert
      expect(result).toEqual(ok([exercise]));
    });

    it("returns each exercise's type unchanged, known or not", async () => {
      // Arrange
      const cardio = { ...exercise, id: "2a3b4c5d-6e7f-4809-9a1b-2c3d4e5f6a7b", type: "CARDIO" };
      const unknown = { ...exercise, id: "3b4c5d6e-7f80-4a1b-8c2d-3e4f5a6b7c8d", type: "YOGA" };
      const repository = new ExerciseRepository(new FakeHttpClient(async () => [cardio, unknown]));

      // Act
      const result = await repository.getAll();

      // Assert
      expect(result).toEqual(ok([cardio, unknown]));
    });

    it("returns each exercise's owner and archive date unchanged", async () => {
      // Arrange
      const custom = {
        ...exercise,
        id: "4c5d6e7f-8091-4b2c-9d3e-4f5a6b7c8d9e",
        ownerId: "5f1c2f0e-9d5b-4a53-8c7e-0d6f7c1a2b3c",
        archivedAtUtc: "2026-11-01T10:00:00Z",
      };
      const repository = new ExerciseRepository(new FakeHttpClient(async () => [exercise, custom]));

      // Act
      const result = await repository.getAll();

      // Assert
      expect(result).toEqual(ok([exercise, custom]));
    });

    it("returns an empty list when the catalog is empty", async () => {
      // Arrange
      const repository = new ExerciseRepository(new FakeHttpClient(async () => []));

      // Act
      const result = await repository.getAll();

      // Assert
      expect(result).toEqual(ok([]));
    });

    it("maps status 500 to UNEXPECTED", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => {
        throw new HttpError(500, { status: 500, detail: "Something failed" });
      });
      const repository = new ExerciseRepository(httpClient);

      // Act
      const result = await repository.getAll();

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "UNEXPECTED" }) });
    });

    it("returns the network code when there is no response", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => {
        throw new TypeError("Failed to fetch");
      });
      const repository = new ExerciseRepository(httpClient);

      // Act
      const result = await repository.getAll();

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "NETWORK" }) });
    });
  });

  describe("create", () => {
    const request: CreateExerciseRequest = {
      languageCode: "es",
      name: "Remo con banda",
      muscle: "MIDDLE_BACK",
      type: "STRENGTH",
    };
    const created: Exercise = {
      ...exercise,
      id: "6a7b8c9d-0e1f-4a2b-8c3d-4e5f6a7b8c9d",
      ownerId: "5f1c2f0e-9d5b-4a53-8c7e-0d6f7c1a2b3c",
    };

    it("posts the request to /exercises as the body", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => created);
      const repository = new ExerciseRepository(httpClient);

      // Act
      await repository.create(request);

      // Assert
      expect(httpClient.requests).toEqual([{ method: "POST", path: "/exercises", body: request }]);
    });

    it("sends the video link when the request has one", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => created);
      const repository = new ExerciseRepository(httpClient);
      const withLink = { ...request, videoUrl: "https://www.youtube.com/watch?v=abc" };

      // Act
      await repository.create(withLink);

      // Assert
      expect(httpClient.requests[0]).toEqual({
        method: "POST",
        path: "/exercises",
        body: withLink,
      });
    });

    it("does not add a video link the request does not have", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => created);
      const repository = new ExerciseRepository(httpClient);

      // Act
      await repository.create(request);

      // Assert
      expect(httpClient.requests[0]?.body).not.toHaveProperty("videoUrl");
    });

    it("returns the created exercise unchanged", async () => {
      // Arrange
      const repository = new ExerciseRepository(new FakeHttpClient(async () => created));

      // Act
      const result = await repository.create(request);

      // Assert
      expect(result).toEqual(ok(created));
    });

    it.each([
      [400, "VALIDATION"],
      [401, "UNAUTHORIZED"],
      [403, "FORBIDDEN"],
      [500, "UNEXPECTED"],
    ])("maps status %i to %s", async (status, code) => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => {
        throw new HttpError(status, { status, detail: "Refused" });
      });
      const repository = new ExerciseRepository(httpClient);

      // Act
      const result = await repository.create(request);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code }) });
    });

    it("returns the network code when there is no response", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => {
        throw new TypeError("Failed to fetch");
      });
      const repository = new ExerciseRepository(httpClient);

      // Act
      const result = await repository.create(request);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "NETWORK" }) });
    });
  });
});
