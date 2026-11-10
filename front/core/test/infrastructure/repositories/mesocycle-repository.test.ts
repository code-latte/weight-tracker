import { describe, expect, it } from "vitest";
import type { HttpClientInterface, HttpRequest } from "../../../src/application";
import {
  ok,
  type CreateMesocycleRequest,
  type DefineMesocycleWeekRequest,
  type Mesocycle,
  type RedefineMesocycleWeekRequest,
  type UpdateMesocycleRequest,
  type WorkoutListItem,
} from "../../../src/domain";
import { HttpError, MesocycleRepository } from "../../../src/infrastructure";

const trainer = {
  id: "6a2d3f1e-0e6c-4b64-9d8f-1e7a8d2b3c4d",
  email: "luis@example.com",
  name: "Luis",
  surname: "Pérez",
  role: "TRAINER",
};

const athlete = {
  id: "5f1c2f0e-9d5b-4a53-8c7e-0d6f7c1a2b3c",
  email: "ana@example.com",
  name: "Ana",
  surname: "García",
  role: "USER",
};

const mesocycle: Mesocycle = {
  id: "9c8b7a6d-5e4f-4a3b-8c2d-1e0f9a8b7c6d",
  trainerId: trainer.id,
  athleteId: athlete.id,
  name: "Bloque de fuerza",
  objective: "Subir la sentadilla",
  startDate: "2026-11-09",
  durationInWeeks: 4,
  weeks: [{ weekNumber: 1, trainingWeekType: "ADAPTATION", objective: "Técnica" }],
  trainer,
  athlete,
};

/** A workout of the mesocycle, as listed, in its first week. */
const weekWorkout: WorkoutListItem = {
  id: "7b0c1f7e-3a54-4d4e-9d0e-1a2b3c4d5e6f",
  name: "Pierna",
  description: null,
  workoutDate: "2026-11-11",
  userId: athlete.id,
  trainerId: trainer.id,
  status: "DRAFT",
  exercises: [],
  athlete,
  trainer,
  mesocycleId: mesocycle.id,
  mesocycleWeekNumber: 1,
  completedAt: null,
  reviewedAt: null,
  reviewedBy: null,
  resultsUpdatedAt: null,
};

const createRequest: CreateMesocycleRequest = {
  athleteId: athlete.id,
  name: "Bloque de fuerza",
  objective: "Subir la sentadilla",
  startDate: "2026-11-09",
  durationInWeeks: 4,
};

const updateRequest: UpdateMesocycleRequest = {
  name: "Bloque de fuerza B",
  objective: "Subir el peso muerto",
  startDate: "2026-11-16",
  durationInWeeks: 6,
};

const defineWeekRequest: DefineMesocycleWeekRequest = {
  weekNumber: 3,
  trainingWeekType: "INTENSIFICATION",
  objective: "Series pesadas",
};

const redefineWeekRequest: RedefineMesocycleWeekRequest = {
  trainingWeekType: "DELOAD",
  objective: "Descarga",
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

describe("MesocycleRepository", () => {
  describe("getAll", () => {
    it("gets /mesocycles without a body", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => [mesocycle]);
      const repository = new MesocycleRepository(httpClient);

      // Act
      await repository.getAll();

      // Assert
      expect(httpClient.requests).toEqual([{ method: "GET", path: "/mesocycles" }]);
    });

    it("returns the mesocycles on success", async () => {
      // Arrange
      const repository = new MesocycleRepository(new FakeHttpClient(async () => [mesocycle]));

      // Act
      const result = await repository.getAll();

      // Assert
      expect(result).toEqual(ok([mesocycle]));
    });

    it.each([
      [401, "UNAUTHORIZED"],
      [403, "FORBIDDEN"],
      [500, "UNEXPECTED"],
    ])("maps status %i to %s", async (status, code) => {
      // Arrange
      const repository = new MesocycleRepository(failingWith(status));

      // Act
      const result = await repository.getAll();

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code }) });
    });

    it("returns the network code when there is no response", async () => {
      // Arrange
      const repository = new MesocycleRepository(unreachable());

      // Act
      const result = await repository.getAll();

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "NETWORK" }) });
    });
  });

  describe("getById", () => {
    it("gets /mesocycles/{id} without a body", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => mesocycle);
      const repository = new MesocycleRepository(httpClient);

      // Act
      await repository.getById(mesocycle.id);

      // Assert
      expect(httpClient.requests).toEqual([{ method: "GET", path: `/mesocycles/${mesocycle.id}` }]);
    });

    it("returns the mesocycle with its weeks on success", async () => {
      // Arrange
      const repository = new MesocycleRepository(new FakeHttpClient(async () => mesocycle));

      // Act
      const result = await repository.getById(mesocycle.id);

      // Assert
      expect(result).toEqual(ok(mesocycle));
    });

    it.each([
      [401, "UNAUTHORIZED"],
      [403, "FORBIDDEN"],
      [404, "NOT_FOUND"],
      [500, "UNEXPECTED"],
    ])("maps status %i to %s", async (status, code) => {
      // Arrange
      const repository = new MesocycleRepository(failingWith(status));

      // Act
      const result = await repository.getById(mesocycle.id);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code }) });
    });

    it("returns the network code when there is no response", async () => {
      // Arrange
      const repository = new MesocycleRepository(unreachable());

      // Act
      const result = await repository.getById(mesocycle.id);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "NETWORK" }) });
    });
  });

  describe("getWorkouts", () => {
    it("gets /mesocycles/{id}/workouts without a body", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => [weekWorkout]);
      const repository = new MesocycleRepository(httpClient);

      // Act
      await repository.getWorkouts(mesocycle.id);

      // Assert
      expect(httpClient.requests).toEqual([
        { method: "GET", path: `/mesocycles/${mesocycle.id}/workouts` },
      ]);
    });

    it("returns the workouts with their week numbers, in the API's order", async () => {
      // Arrange
      const second: WorkoutListItem = { ...weekWorkout, id: "w-2", mesocycleWeekNumber: 2 };
      const repository = new MesocycleRepository(
        new FakeHttpClient(async () => [second, weekWorkout]),
      );

      // Act
      const result = await repository.getWorkouts(mesocycle.id);

      // Assert
      expect(result).toEqual(ok([second, weekWorkout]));
    });

    it("returns an empty list when the mesocycle has no workouts", async () => {
      // Arrange
      const repository = new MesocycleRepository(new FakeHttpClient(async () => []));

      // Act
      const result = await repository.getWorkouts(mesocycle.id);

      // Assert
      expect(result).toEqual(ok([]));
    });

    it.each([
      [401, "UNAUTHORIZED"],
      [403, "FORBIDDEN"],
      [404, "NOT_FOUND"],
      [500, "UNEXPECTED"],
    ])("maps status %i to %s", async (status, code) => {
      // Arrange
      const repository = new MesocycleRepository(failingWith(status));

      // Act
      const result = await repository.getWorkouts(mesocycle.id);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code }) });
    });

    it("returns the network code when there is no response", async () => {
      // Arrange
      const repository = new MesocycleRepository(unreachable());

      // Act
      const result = await repository.getWorkouts(mesocycle.id);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "NETWORK" }) });
    });
  });

  describe("getByAthlete", () => {
    it("gets /users/{userId}/mesocycles without a body", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => [mesocycle]);
      const repository = new MesocycleRepository(httpClient);

      // Act
      await repository.getByAthlete(athlete.id);

      // Assert
      expect(httpClient.requests).toEqual([
        { method: "GET", path: `/users/${athlete.id}/mesocycles` },
      ]);
    });

    it("returns the athlete's mesocycles on success", async () => {
      // Arrange
      const repository = new MesocycleRepository(new FakeHttpClient(async () => [mesocycle]));

      // Act
      const result = await repository.getByAthlete(athlete.id);

      // Assert
      expect(result).toEqual(ok([mesocycle]));
    });

    it.each([
      [401, "UNAUTHORIZED"],
      [403, "FORBIDDEN"],
      [404, "NOT_FOUND"],
      [500, "UNEXPECTED"],
    ])("maps status %i to %s", async (status, code) => {
      // Arrange
      const repository = new MesocycleRepository(failingWith(status));

      // Act
      const result = await repository.getByAthlete(athlete.id);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code }) });
    });

    it("returns the network code when there is no response", async () => {
      // Arrange
      const repository = new MesocycleRepository(unreachable());

      // Act
      const result = await repository.getByAthlete(athlete.id);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "NETWORK" }) });
    });
  });

  describe("create", () => {
    it("posts the request to /mesocycles unchanged", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => mesocycle);
      const repository = new MesocycleRepository(httpClient);

      // Act
      await repository.create(createRequest);

      // Assert
      expect(httpClient.requests).toEqual([
        { method: "POST", path: "/mesocycles", body: createRequest },
      ]);
    });

    it("returns the mesocycle on success", async () => {
      // Arrange
      const repository = new MesocycleRepository(new FakeHttpClient(async () => mesocycle));

      // Act
      const result = await repository.create(createRequest);

      // Assert
      expect(result).toEqual(ok(mesocycle));
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
      const repository = new MesocycleRepository(failingWith(status));

      // Act
      const result = await repository.create(createRequest);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code }) });
    });

    it("returns the network code when there is no response", async () => {
      // Arrange
      const repository = new MesocycleRepository(unreachable());

      // Act
      const result = await repository.create(createRequest);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "NETWORK" }) });
    });

    it("keeps the problem detail as the message", async () => {
      // Arrange
      const repository = new MesocycleRepository(failingWith(400));

      // Act
      const result = await repository.create(createRequest);

      // Assert
      expect(result).toEqual({
        ok: false,
        error: expect.objectContaining({ message: "Something failed" }),
      });
    });
  });

  describe("update", () => {
    it("puts the request to /mesocycles/{id} unchanged", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => mesocycle);
      const repository = new MesocycleRepository(httpClient);

      // Act
      await repository.update(mesocycle.id, updateRequest);

      // Assert
      expect(httpClient.requests).toEqual([
        { method: "PUT", path: `/mesocycles/${mesocycle.id}`, body: updateRequest },
      ]);
    });

    it("returns the updated mesocycle on success", async () => {
      // Arrange
      const repository = new MesocycleRepository(new FakeHttpClient(async () => mesocycle));

      // Act
      const result = await repository.update(mesocycle.id, updateRequest);

      // Assert
      expect(result).toEqual(ok(mesocycle));
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
      const repository = new MesocycleRepository(failingWith(status));

      // Act
      const result = await repository.update(mesocycle.id, updateRequest);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code }) });
    });

    it("returns the network code when there is no response", async () => {
      // Arrange
      const repository = new MesocycleRepository(unreachable());

      // Act
      const result = await repository.update(mesocycle.id, updateRequest);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "NETWORK" }) });
    });
  });

  describe("defineWeek", () => {
    it("posts the week to /mesocycles/{id}/weeks unchanged", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => mesocycle);
      const repository = new MesocycleRepository(httpClient);

      // Act
      await repository.defineWeek(mesocycle.id, defineWeekRequest);

      // Assert
      expect(httpClient.requests).toEqual([
        { method: "POST", path: `/mesocycles/${mesocycle.id}/weeks`, body: defineWeekRequest },
      ]);
    });

    it("returns the mesocycle on success", async () => {
      // Arrange
      const repository = new MesocycleRepository(new FakeHttpClient(async () => mesocycle));

      // Act
      const result = await repository.defineWeek(mesocycle.id, defineWeekRequest);

      // Assert
      expect(result).toEqual(ok(mesocycle));
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
      const repository = new MesocycleRepository(failingWith(status));

      // Act
      const result = await repository.defineWeek(mesocycle.id, defineWeekRequest);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code }) });
    });

    it("returns the network code when there is no response", async () => {
      // Arrange
      const repository = new MesocycleRepository(unreachable());

      // Act
      const result = await repository.defineWeek(mesocycle.id, defineWeekRequest);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "NETWORK" }) });
    });
  });

  describe("redefineWeek", () => {
    it("puts the week to /mesocycles/{id}/weeks/{weekNumber} unchanged", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => mesocycle);
      const repository = new MesocycleRepository(httpClient);

      // Act
      await repository.redefineWeek(mesocycle.id, 2, redefineWeekRequest);

      // Assert
      expect(httpClient.requests).toEqual([
        { method: "PUT", path: `/mesocycles/${mesocycle.id}/weeks/2`, body: redefineWeekRequest },
      ]);
    });

    it("returns the mesocycle on success", async () => {
      // Arrange
      const repository = new MesocycleRepository(new FakeHttpClient(async () => mesocycle));

      // Act
      const result = await repository.redefineWeek(mesocycle.id, 2, redefineWeekRequest);

      // Assert
      expect(result).toEqual(ok(mesocycle));
    });

    it.each([
      [400, "VALIDATION"],
      [401, "UNAUTHORIZED"],
      [403, "FORBIDDEN"],
      [404, "NOT_FOUND"],
      [500, "UNEXPECTED"],
    ])("maps status %i to %s", async (status, code) => {
      // Arrange
      const repository = new MesocycleRepository(failingWith(status));

      // Act
      const result = await repository.redefineWeek(mesocycle.id, 2, redefineWeekRequest);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code }) });
    });

    it("returns the network code when there is no response", async () => {
      // Arrange
      const repository = new MesocycleRepository(unreachable());

      // Act
      const result = await repository.redefineWeek(mesocycle.id, 2, redefineWeekRequest);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "NETWORK" }) });
    });
  });
});
