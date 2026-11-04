import { describe, expect, it } from "vitest";
import type { HttpClientInterface, HttpRequest } from "../../../src/application";
import { ok, type ExerciseFavorite } from "../../../src/domain";
import { ExerciseFavoriteRepository, HttpError } from "../../../src/infrastructure";

const exerciseId = "1f2e3d4c-5b6a-4978-8695-a4b3c2d1e0f9";

const favorite: ExerciseFavorite = {
  exerciseId,
  createdAtUtc: "2026-11-03T09:30:00Z",
};

const olderFavorite: ExerciseFavorite = {
  exerciseId: "2a3b4c5d-6e7f-4809-9a1b-2c3d4e5f6a7b",
  createdAtUtc: "2026-11-01T18:00:00Z",
};

class FakeHttpClient implements HttpClientInterface {
  readonly requests: HttpRequest[] = [];

  constructor(private readonly respond: () => Promise<unknown>) {}

  async request<T>(httpRequest: HttpRequest): Promise<T> {
    this.requests.push(httpRequest);
    return (await this.respond()) as T;
  }
}

const failingWith = (status: number) =>
  new FakeHttpClient(async () => {
    throw new HttpError(status, { status, detail: "Something failed" });
  });

const unreachable = () =>
  new FakeHttpClient(async () => {
    throw new TypeError("Failed to fetch");
  });

describe("ExerciseFavoriteRepository", () => {
  describe("getAll", () => {
    it("gets /exercise-favorites without a body", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => [favorite]);
      const repository = new ExerciseFavoriteRepository(httpClient);

      // Act
      await repository.getAll();

      // Assert
      expect(httpClient.requests).toEqual([{ method: "GET", path: "/exercise-favorites" }]);
    });

    it("returns the favorites unchanged and in the order the API sends them", async () => {
      // Arrange
      const repository = new ExerciseFavoriteRepository(
        new FakeHttpClient(async () => [favorite, olderFavorite]),
      );

      // Act
      const result = await repository.getAll();

      // Assert
      expect(result).toEqual(ok([favorite, olderFavorite]));
    });

    it("returns an empty list when there are no favorites", async () => {
      // Arrange
      const repository = new ExerciseFavoriteRepository(new FakeHttpClient(async () => []));

      // Act
      const result = await repository.getAll();

      // Assert
      expect(result).toEqual(ok([]));
    });

    it.each([
      [401, "UNAUTHORIZED"],
      [500, "UNEXPECTED"],
    ])("maps status %i to %s", async (status, code) => {
      // Arrange
      const repository = new ExerciseFavoriteRepository(failingWith(status));

      // Act
      const result = await repository.getAll();

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code }) });
    });

    it("returns the network code when there is no response", async () => {
      // Arrange
      const repository = new ExerciseFavoriteRepository(unreachable());

      // Act
      const result = await repository.getAll();

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "NETWORK" }) });
    });
  });

  describe("mark", () => {
    it("puts /exercise-favorites/{exerciseId} without a body", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => favorite);
      const repository = new ExerciseFavoriteRepository(httpClient);

      // Act
      await repository.mark(exerciseId);

      // Assert
      expect(httpClient.requests).toEqual([
        { method: "PUT", path: `/exercise-favorites/${exerciseId}` },
      ]);
    });

    it("returns the favorite on success", async () => {
      // Arrange
      const repository = new ExerciseFavoriteRepository(new FakeHttpClient(async () => favorite));

      // Act
      const result = await repository.mark(exerciseId);

      // Assert
      expect(result).toEqual(ok(favorite));
    });

    it("keeps the original date when the exercise already was a favorite", async () => {
      // Arrange
      const original = { ...favorite, createdAtUtc: "2026-10-01T08:00:00Z" };
      const repository = new ExerciseFavoriteRepository(new FakeHttpClient(async () => original));

      // Act
      const result = await repository.mark(exerciseId);

      // Assert
      expect(result).toEqual(ok(original));
    });

    it.each([
      [401, "UNAUTHORIZED"],
      [404, "NOT_FOUND"],
      [409, "CONFLICT"],
      [500, "UNEXPECTED"],
    ])("maps status %i to %s", async (status, code) => {
      // Arrange
      const repository = new ExerciseFavoriteRepository(failingWith(status));

      // Act
      const result = await repository.mark(exerciseId);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code }) });
    });

    it("returns the network code when there is no response", async () => {
      // Arrange
      const repository = new ExerciseFavoriteRepository(unreachable());

      // Act
      const result = await repository.mark(exerciseId);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "NETWORK" }) });
    });
  });

  describe("unmark", () => {
    it("deletes /exercise-favorites/{exerciseId} without a body", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => undefined);
      const repository = new ExerciseFavoriteRepository(httpClient);

      // Act
      await repository.unmark(exerciseId);

      // Assert
      expect(httpClient.requests).toEqual([
        { method: "DELETE", path: `/exercise-favorites/${exerciseId}` },
      ]);
    });

    it("returns an empty success when the API answers 204", async () => {
      // Arrange
      const repository = new ExerciseFavoriteRepository(new FakeHttpClient(async () => undefined));

      // Act
      const result = await repository.unmark(exerciseId);

      // Assert
      expect(result).toEqual(ok(undefined));
    });

    it.each([
      [401, "UNAUTHORIZED"],
      [404, "NOT_FOUND"],
      [409, "CONFLICT"],
      [500, "UNEXPECTED"],
    ])("maps status %i to %s", async (status, code) => {
      // Arrange
      const repository = new ExerciseFavoriteRepository(failingWith(status));

      // Act
      const result = await repository.unmark(exerciseId);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code }) });
    });

    it("returns the network code when there is no response", async () => {
      // Arrange
      const repository = new ExerciseFavoriteRepository(unreachable());

      // Act
      const result = await repository.unmark(exerciseId);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "NETWORK" }) });
    });
  });
});
