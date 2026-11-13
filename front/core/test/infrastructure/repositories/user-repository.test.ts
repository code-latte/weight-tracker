import { describe, expect, it } from "vitest";
import type { HttpClientInterface, HttpRequest } from "../../../src/application";
import {
  type CreatedUserResponse,
  type CreateUserRequest,
  type ExerciseHistorySession,
  type ExerciseLastPerformance,
  ok,
  type UpdateProfileRequest,
  type UserActivationSummary,
  type UserProfile,
  type UserSummary,
} from "../../../src/domain";
import { HttpError, UserRepository } from "../../../src/infrastructure";

const profile: UserProfile = {
  id: "5f1c2f0e-9d5b-4a53-8c7e-0d6f7c1a2b3c",
  email: "trainer@example.com",
  name: "Ana",
  surname: "García",
  role: "TRAINER",
};

const lastPerformance: ExerciseLastPerformance = {
  workoutDate: "2026-11-10",
  effort: "HIGH",
  sets: [
    { reps: 10, durationSeconds: null, weight: 20 },
    { reps: 10, durationSeconds: null, weight: null },
  ],
};

const history: ExerciseHistorySession[] = [
  {
    workoutId: "7b0c1f7e-3a54-4d4e-9d0e-1a2b3c4d5e6f",
    workoutDate: "2026-11-12",
    maxWeight: 130,
    volume: 3190,
    estimatedOneRepMax: 160.3,
  },
  {
    workoutId: "2b3c4d5e-6f70-4819-9a0b-1c2d3e4f5a6b",
    workoutDate: "2026-10-27",
    maxWeight: null,
    volume: null,
    estimatedOneRepMax: null,
  },
];

const summary: UserActivationSummary = { ...profile, isActive: true, deactivatedAtUtc: null };

const listed: UserSummary = { ...summary, trainer: null };

const updateRequest: UpdateProfileRequest = { name: "Ana", surname: "García" };

const createRequest: CreateUserRequest = {
  email: "new@example.com",
  password: "Str0ng!Passw0rd",
  role: 2,
  name: "Luis",
  surname: "Pérez",
  dateBirth: "1990-05-17",
};

const createdUser: CreatedUserResponse = {
  id: "7a2b3c4d-1e2f-4a5b-9c8d-0e1f2a3b4c5d",
  email: "new@example.com",
  name: "Luis",
  surname: "Pérez",
  dateBirth: "1990-05-17",
  role: "TRAINER",
};

class FakeHttpClient implements HttpClientInterface {
  readonly requests: HttpRequest[] = [];

  constructor(private readonly respond: () => Promise<unknown>) {}

  async request<T>(httpRequest: HttpRequest): Promise<T> {
    this.requests.push(httpRequest);
    return (await this.respond()) as T;
  }
}

describe("UserRepository", () => {
  describe("getMe", () => {
    it("gets /users/me without a body", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => profile);
      const repository = new UserRepository(httpClient);

      // Act
      await repository.getMe();

      // Assert
      expect(httpClient.requests).toEqual([{ method: "GET", path: "/users/me" }]);
    });

    it("returns the profile on success", async () => {
      // Arrange
      const repository = new UserRepository(new FakeHttpClient(async () => profile));

      // Act
      const result = await repository.getMe();

      // Assert
      expect(result).toEqual(ok(profile));
    });

    it.each([
      [401, "UNAUTHORIZED"],
      [500, "UNEXPECTED"],
    ])("maps status %i to %s", async (status, code) => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => {
        throw new HttpError(status, { status, detail: "Something failed" });
      });
      const repository = new UserRepository(httpClient);

      // Act
      const result = await repository.getMe();

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code }) });
    });

    it("returns the network code when there is no response", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => {
        throw new TypeError("Failed to fetch");
      });
      const repository = new UserRepository(httpClient);

      // Act
      const result = await repository.getMe();

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "NETWORK" }) });
    });
  });

  describe("getTrainer", () => {
    const userId = "2c3d4e5f-6a7b-4c8d-9e0f-1a2b3c4d5e6f";

    it("gets /users/{userId}/trainer without a body", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => profile);
      const repository = new UserRepository(httpClient);

      // Act
      await repository.getTrainer(userId);

      // Assert
      expect(httpClient.requests).toEqual([{ method: "GET", path: `/users/${userId}/trainer` }]);
    });

    it("returns the trainer's profile on success", async () => {
      // Arrange
      const repository = new UserRepository(new FakeHttpClient(async () => profile));

      // Act
      const result = await repository.getTrainer(userId);

      // Assert
      expect(result).toEqual(ok(profile));
    });

    it("returns null when the API answers with no content", async () => {
      // Arrange
      const repository = new UserRepository(new FakeHttpClient(async () => undefined));

      // Act
      const result = await repository.getTrainer(userId);

      // Assert
      expect(result).toEqual(ok(null));
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
      const repository = new UserRepository(httpClient);

      // Act
      const result = await repository.getTrainer(userId);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code }) });
    });

    it("returns the network code when there is no response", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => {
        throw new TypeError("Failed to fetch");
      });
      const repository = new UserRepository(httpClient);

      // Act
      const result = await repository.getTrainer(userId);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "NETWORK" }) });
    });
  });

  describe("getMyExerciseLastPerformance", () => {
    const exerciseId = "9b8a7c6d-5e4f-4a3b-8c2d-1e0f9a8b7c6d";

    it("gets /users/me/exercises/{exerciseId}/last-performance without a body", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => lastPerformance);
      const repository = new UserRepository(httpClient);

      // Act
      await repository.getMyExerciseLastPerformance(exerciseId);

      // Assert
      expect(httpClient.requests).toEqual([
        { method: "GET", path: `/users/me/exercises/${exerciseId}/last-performance` },
      ]);
    });

    it("returns the last performance on success, keeping a null weight", async () => {
      // Arrange
      const repository = new UserRepository(new FakeHttpClient(async () => lastPerformance));

      // Act
      const result = await repository.getMyExerciseLastPerformance(exerciseId);

      // Assert
      expect(result).toEqual(ok(lastPerformance));
    });

    it("returns null when the API answers with no content", async () => {
      // Arrange
      const repository = new UserRepository(new FakeHttpClient(async () => undefined));

      // Act
      const result = await repository.getMyExerciseLastPerformance(exerciseId);

      // Assert
      expect(result).toEqual(ok(null));
    });

    it.each([
      [401, "UNAUTHORIZED"],
      [404, "NOT_FOUND"],
      [500, "UNEXPECTED"],
    ])("maps status %i to %s", async (status, code) => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => {
        throw new HttpError(status, { status, detail: "Something failed" });
      });
      const repository = new UserRepository(httpClient);

      // Act
      const result = await repository.getMyExerciseLastPerformance(exerciseId);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code }) });
    });

    it("returns the network code when there is no response", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => {
        throw new TypeError("Failed to fetch");
      });
      const repository = new UserRepository(httpClient);

      // Act
      const result = await repository.getMyExerciseLastPerformance(exerciseId);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "NETWORK" }) });
    });
  });

  describe("getExerciseHistory", () => {
    const userId = "5f1c2f0e-9d5b-4a53-8c7e-0d6f7c1a2b3c";
    const exerciseId = "9b8a7c6d-5e4f-4a3b-8c2d-1e0f9a8b7c6d";

    it("gets /users/{userId}/exercises/{exerciseId}/history with the limit and no body", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => history);
      const repository = new UserRepository(httpClient);

      // Act
      await repository.getExerciseHistory(userId, exerciseId, 5);

      // Assert
      expect(httpClient.requests).toEqual([
        { method: "GET", path: `/users/${userId}/exercises/${exerciseId}/history?limit=5` },
      ]);
    });

    it("returns the sessions in the API's order, keeping every null", async () => {
      // Arrange
      const repository = new UserRepository(new FakeHttpClient(async () => history));

      // Act
      const result = await repository.getExerciseHistory(userId, exerciseId, 5);

      // Assert
      expect(result).toEqual(ok(history));
    });

    it("returns an empty list when the user never did the exercise", async () => {
      // Arrange
      const repository = new UserRepository(new FakeHttpClient(async () => []));

      // Act
      const result = await repository.getExerciseHistory(userId, exerciseId, 5);

      // Assert
      expect(result).toEqual(ok([]));
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
      const repository = new UserRepository(httpClient);

      // Act
      const result = await repository.getExerciseHistory(userId, exerciseId, 5);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code }) });
    });

    it("returns the network code when there is no response", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => {
        throw new TypeError("Failed to fetch");
      });
      const repository = new UserRepository(httpClient);

      // Act
      const result = await repository.getExerciseHistory(userId, exerciseId, 5);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "NETWORK" }) });
    });
  });

  describe("updateMe", () => {
    it("puts the request to /users/me unchanged", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => profile);
      const repository = new UserRepository(httpClient);

      // Act
      await repository.updateMe(updateRequest);

      // Assert
      expect(httpClient.requests).toEqual([
        { method: "PUT", path: "/users/me", body: updateRequest },
      ]);
    });

    it("returns the updated profile on success", async () => {
      // Arrange
      const repository = new UserRepository(new FakeHttpClient(async () => profile));

      // Act
      const result = await repository.updateMe(updateRequest);

      // Assert
      expect(result).toEqual(ok(profile));
    });

    it.each([
      [400, "VALIDATION"],
      [401, "UNAUTHORIZED"],
      [500, "UNEXPECTED"],
    ])("maps status %i to %s", async (status, code) => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => {
        throw new HttpError(status, { status, detail: "Something failed" });
      });
      const repository = new UserRepository(httpClient);

      // Act
      const result = await repository.updateMe(updateRequest);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code }) });
    });

    it("returns the network code when there is no response", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => {
        throw new TypeError("Failed to fetch");
      });
      const repository = new UserRepository(httpClient);

      // Act
      const result = await repository.updateMe(updateRequest);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "NETWORK" }) });
    });

    it("keeps the problem detail as the message", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => {
        throw new HttpError(400, { status: 400, detail: "Name is too long" });
      });
      const repository = new UserRepository(httpClient);

      // Act
      const result = await repository.updateMe(updateRequest);

      // Assert
      expect(result).toEqual({
        ok: false,
        error: expect.objectContaining({ message: "Name is too long" }),
      });
    });
  });

  describe("getAll", () => {
    it("gets /users without a body", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => [listed]);
      const repository = new UserRepository(httpClient);

      // Act
      await repository.getAll();

      // Assert
      expect(httpClient.requests).toEqual([{ method: "GET", path: "/users" }]);
    });

    it("returns the users in the API's order on success", async () => {
      // Arrange
      const other: UserSummary = {
        ...listed,
        id: "1b2c3d4e-5f6a-4b7c-8d9e-0f1a2b3c4d5e",
        role: "USER",
      };
      const repository = new UserRepository(new FakeHttpClient(async () => [other, listed]));

      // Act
      const result = await repository.getAll();

      // Assert
      expect(result).toEqual(ok([other, listed]));
    });

    it("keeps each user's activation state unchanged", async () => {
      // Arrange
      const deactivated: UserSummary = {
        ...listed,
        id: "1b2c3d4e-5f6a-4b7c-8d9e-0f1a2b3c4d5e",
        isActive: false,
        deactivatedAtUtc: "2026-10-20T09:30:00Z",
      };
      const repository = new UserRepository(new FakeHttpClient(async () => [listed, deactivated]));

      // Act
      const result = await repository.getAll();

      // Assert
      expect(result).toEqual(ok([listed, deactivated]));
    });

    it("keeps each user's current trainer unchanged", async () => {
      // Arrange
      const athlete: UserSummary = {
        ...listed,
        id: "1b2c3d4e-5f6a-4b7c-8d9e-0f1a2b3c4d5e",
        role: "USER",
        trainer: profile,
      };
      const repository = new UserRepository(new FakeHttpClient(async () => [athlete, listed]));

      // Act
      const result = await repository.getAll();

      // Assert
      expect(result).toEqual(ok([athlete, listed]));
    });

    it.each([
      [401, "UNAUTHORIZED"],
      [403, "FORBIDDEN"],
      [500, "UNEXPECTED"],
      [501, "UNEXPECTED"],
    ])("maps status %i to %s", async (status, code) => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => {
        throw new HttpError(status, { status, detail: "Something failed" });
      });
      const repository = new UserRepository(httpClient);

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
      const repository = new UserRepository(httpClient);

      // Act
      const result = await repository.getAll();

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "NETWORK" }) });
    });
  });

  describe("create", () => {
    it("posts the request to /users unchanged", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => createdUser);
      const repository = new UserRepository(httpClient);

      // Act
      await repository.create(createRequest);

      // Assert
      expect(httpClient.requests).toEqual([
        { method: "POST", path: "/users", body: createRequest },
      ]);
    });

    it("returns the created user on success", async () => {
      // Arrange
      const repository = new UserRepository(new FakeHttpClient(async () => createdUser));

      // Act
      const result = await repository.create(createRequest);

      // Assert
      expect(result).toEqual(ok(createdUser));
    });

    it.each([
      [400, "VALIDATION"],
      [401, "UNAUTHORIZED"],
      [403, "FORBIDDEN"],
      [409, "CONFLICT"],
      [500, "UNEXPECTED"],
    ])("maps status %i to %s", async (status, code) => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => {
        throw new HttpError(status, { status, detail: "Something failed" });
      });
      const repository = new UserRepository(httpClient);

      // Act
      const result = await repository.create(createRequest);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code }) });
    });

    it("returns the network code when there is no response", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => {
        throw new TypeError("Failed to fetch");
      });
      const repository = new UserRepository(httpClient);

      // Act
      const result = await repository.create(createRequest);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "NETWORK" }) });
    });

    it("keeps the problem detail as the message", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => {
        throw new HttpError(409, { status: 409, detail: "Email already exists" });
      });
      const repository = new UserRepository(httpClient);

      // Act
      const result = await repository.create(createRequest);

      // Assert
      expect(result).toEqual({
        ok: false,
        error: expect.objectContaining({ message: "Email already exists" }),
      });
    });
  });

  describe.each(["activate", "deactivate"] as const)("%s", (operation) => {
    const id = "1b2c3d4e-5f6a-4b7c-8d9e-0f1a2b3c4d5e";

    it(`puts /users/{id}/${operation} without a body`, async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => summary);
      const repository = new UserRepository(httpClient);

      // Act
      await repository[operation](id);

      // Assert
      expect(httpClient.requests).toEqual([{ method: "PUT", path: `/users/${id}/${operation}` }]);
    });

    it("returns the user the API answers with on success", async () => {
      // Arrange
      const repository = new UserRepository(new FakeHttpClient(async () => summary));

      // Act
      const result = await repository[operation](id);

      // Assert
      expect(result).toEqual(ok(summary));
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
      const repository = new UserRepository(httpClient);

      // Act
      const result = await repository[operation](id);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code }) });
    });

    it("returns the network code when there is no response", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => {
        throw new TypeError("Failed to fetch");
      });
      const repository = new UserRepository(httpClient);

      // Act
      const result = await repository[operation](id);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "NETWORK" }) });
    });

    it("keeps the problem detail as the message", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => {
        throw new HttpError(409, { status: 409, detail: "User is already in that state" });
      });
      const repository = new UserRepository(httpClient);

      // Act
      const result = await repository[operation](id);

      // Assert
      expect(result).toEqual({
        ok: false,
        error: expect.objectContaining({ message: "User is already in that state" }),
      });
    });
  });
});
