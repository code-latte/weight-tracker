import { describe, expect, it } from "vitest";
import type { HttpClientInterface, HttpRequest } from "../../../src/application";
import {
  ok,
  type AddExerciseRequest,
  type CreateWorkoutRequest,
  type ReorderWorkoutExercisesRequest,
  type UpdateWorkoutRequest,
  type Workout,
  type WorkoutDetail,
  type WorkoutExerciseDetail,
} from "../../../src/domain";
import { HttpError, WorkoutRepository } from "../../../src/infrastructure";

const workout: Workout = {
  id: "7b0c1f7e-3a54-4d4e-9d0e-1a2b3c4d5e6f",
  name: "Pierna",
  description: "Día 1",
  workoutDate: "2026-11-02",
  userId: "5f1c2f0e-9d5b-4a53-8c7e-0d6f7c1a2b3c",
  trainerId: null,
  status: "DRAFT",
  exercises: [],
};

const workoutExercise: WorkoutExerciseDetail = {
  id: "0a1b2c3d-4e5f-4a6b-8c7d-9e0f1a2b3c4d",
  exerciseId: "1f2e3d4c-5b6a-4978-8695-a4b3c2d1e0f9",
  effort: "HIGH",
  perSide: true,
  restMinutes: 1.5,
  sets: [
    { count: 3, target: { type: "REPS", value: 10, maxValue: null } },
    { count: 1, target: { type: "Minutes", minutes: 2 } },
  ],
  superSeriesId: null,
};

const workoutDetail: WorkoutDetail = {
  ...workout,
  exercises: [workoutExercise],
};

const createRequest: CreateWorkoutRequest = {
  name: "Pierna",
  description: "Día 1",
  workoutDate: "2026-11-02",
  userId: workout.userId,
};

const updateRequest: UpdateWorkoutRequest = {
  name: "Pierna B",
  description: null,
  workoutDate: "2026-11-03",
  userId: workout.userId,
};

const addExerciseRequest: AddExerciseRequest = {
  exerciseId: workoutExercise.exerciseId,
  effort: "HIGH",
  sets: [
    { count: 3, targetType: "REPS", target: 10 },
    { count: 1, targetType: "Minutes", targetMinutes: 2 },
  ],
  perSide: true,
  restMinutes: 1.5,
};

const reorderRequest: ReorderWorkoutExercisesRequest = {
  exerciseIds: [workoutExercise.exerciseId, "2a3b4c5d-6e7f-4809-9a1b-2c3d4e5f6a7b"],
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

describe("WorkoutRepository", () => {
  describe("getAll", () => {
    it("gets /workouts without a body", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => [workout]);
      const repository = new WorkoutRepository(httpClient);

      // Act
      await repository.getAll();

      // Assert
      expect(httpClient.requests).toEqual([{ method: "GET", path: "/workouts" }]);
    });

    it("returns the workouts on success", async () => {
      // Arrange
      const repository = new WorkoutRepository(new FakeHttpClient(async () => [workout]));

      // Act
      const result = await repository.getAll();

      // Assert
      expect(result).toEqual(ok([workout]));
    });

    it.each([
      [401, "UNAUTHORIZED"],
      [403, "FORBIDDEN"],
      [500, "UNEXPECTED"],
    ])("maps status %i to %s", async (status, code) => {
      // Arrange
      const repository = new WorkoutRepository(failingWith(status));

      // Act
      const result = await repository.getAll();

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code }) });
    });

    it("returns the network code when there is no response", async () => {
      // Arrange
      const repository = new WorkoutRepository(unreachable());

      // Act
      const result = await repository.getAll();

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "NETWORK" }) });
    });
  });

  describe("getById", () => {
    it("gets /workouts/{id} without a body", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => workoutDetail);
      const repository = new WorkoutRepository(httpClient);

      // Act
      await repository.getById(workout.id);

      // Assert
      expect(httpClient.requests).toEqual([{ method: "GET", path: `/workouts/${workout.id}` }]);
    });

    it("returns the workout with its exercises on success", async () => {
      // Arrange
      const repository = new WorkoutRepository(new FakeHttpClient(async () => workoutDetail));

      // Act
      const result = await repository.getById(workout.id);

      // Assert
      expect(result).toEqual(ok(workoutDetail));
    });

    it.each([
      [401, "UNAUTHORIZED"],
      [403, "FORBIDDEN"],
      [404, "NOT_FOUND"],
      [500, "UNEXPECTED"],
    ])("maps status %i to %s", async (status, code) => {
      // Arrange
      const repository = new WorkoutRepository(failingWith(status));

      // Act
      const result = await repository.getById(workout.id);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code }) });
    });

    it("returns the network code when there is no response", async () => {
      // Arrange
      const repository = new WorkoutRepository(unreachable());

      // Act
      const result = await repository.getById(workout.id);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "NETWORK" }) });
    });
  });

  describe("create", () => {
    it("posts the request to /workouts unchanged", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => workout);
      const repository = new WorkoutRepository(httpClient);

      // Act
      await repository.create(createRequest);

      // Assert
      expect(httpClient.requests).toEqual([
        { method: "POST", path: "/workouts", body: createRequest },
      ]);
    });

    it("returns the workout on success", async () => {
      // Arrange
      const repository = new WorkoutRepository(new FakeHttpClient(async () => workout));

      // Act
      const result = await repository.create(createRequest);

      // Assert
      expect(result).toEqual(ok(workout));
    });

    it.each([
      [400, "VALIDATION"],
      [401, "UNAUTHORIZED"],
      [403, "FORBIDDEN"],
      [404, "NOT_FOUND"],
      [500, "UNEXPECTED"],
    ])("maps status %i to %s", async (status, code) => {
      // Arrange
      const repository = new WorkoutRepository(failingWith(status));

      // Act
      const result = await repository.create(createRequest);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code }) });
    });

    it("returns the network code when there is no response", async () => {
      // Arrange
      const repository = new WorkoutRepository(unreachable());

      // Act
      const result = await repository.create(createRequest);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "NETWORK" }) });
    });

    it("keeps the problem detail as the message", async () => {
      // Arrange
      const repository = new WorkoutRepository(failingWith(400));

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
    it("puts the request to /workouts/{id} unchanged", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => workout);
      const repository = new WorkoutRepository(httpClient);

      // Act
      await repository.update(workout.id, updateRequest);

      // Assert
      expect(httpClient.requests).toEqual([
        { method: "PUT", path: `/workouts/${workout.id}`, body: updateRequest },
      ]);
    });

    it("returns the updated workout on success", async () => {
      // Arrange
      const repository = new WorkoutRepository(new FakeHttpClient(async () => workout));

      // Act
      const result = await repository.update(workout.id, updateRequest);

      // Assert
      expect(result).toEqual(ok(workout));
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
      const repository = new WorkoutRepository(failingWith(status));

      // Act
      const result = await repository.update(workout.id, updateRequest);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code }) });
    });

    it("returns the network code when there is no response", async () => {
      // Arrange
      const repository = new WorkoutRepository(unreachable());

      // Act
      const result = await repository.update(workout.id, updateRequest);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "NETWORK" }) });
    });
  });

  describe("addExercise", () => {
    it("posts the request to /workouts/{workoutId}/exercises unchanged", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => workoutExercise);
      const repository = new WorkoutRepository(httpClient);

      // Act
      await repository.addExercise(workout.id, addExerciseRequest);

      // Assert
      expect(httpClient.requests).toEqual([
        { method: "POST", path: `/workouts/${workout.id}/exercises`, body: addExerciseRequest },
      ]);
    });

    it("returns the workout exercise on success", async () => {
      // Arrange
      const repository = new WorkoutRepository(new FakeHttpClient(async () => workoutExercise));

      // Act
      const result = await repository.addExercise(workout.id, addExerciseRequest);

      // Assert
      expect(result).toEqual(ok(workoutExercise));
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
      const repository = new WorkoutRepository(failingWith(status));

      // Act
      const result = await repository.addExercise(workout.id, addExerciseRequest);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code }) });
    });

    it("returns the network code when there is no response", async () => {
      // Arrange
      const repository = new WorkoutRepository(unreachable());

      // Act
      const result = await repository.addExercise(workout.id, addExerciseRequest);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "NETWORK" }) });
    });
  });

  describe("updateExercise", () => {
    it("puts the request to /workouts/{workoutId}/exercises/{exerciseId} unchanged", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => workoutExercise);
      const repository = new WorkoutRepository(httpClient);

      // Act
      await repository.updateExercise(workout.id, workoutExercise.exerciseId, addExerciseRequest);

      // Assert
      expect(httpClient.requests).toEqual([
        {
          method: "PUT",
          path: `/workouts/${workout.id}/exercises/${workoutExercise.exerciseId}`,
          body: addExerciseRequest,
        },
      ]);
    });

    it("returns the workout exercise on success", async () => {
      // Arrange
      const repository = new WorkoutRepository(new FakeHttpClient(async () => workoutExercise));

      // Act
      const result = await repository.updateExercise(
        workout.id,
        workoutExercise.exerciseId,
        addExerciseRequest,
      );

      // Assert
      expect(result).toEqual(ok(workoutExercise));
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
      const repository = new WorkoutRepository(failingWith(status));

      // Act
      const result = await repository.updateExercise(
        workout.id,
        workoutExercise.exerciseId,
        addExerciseRequest,
      );

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code }) });
    });

    it("returns the network code when there is no response", async () => {
      // Arrange
      const repository = new WorkoutRepository(unreachable());

      // Act
      const result = await repository.updateExercise(
        workout.id,
        workoutExercise.exerciseId,
        addExerciseRequest,
      );

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "NETWORK" }) });
    });
  });

  describe("removeExercise", () => {
    it("deletes /workouts/{workoutId}/exercises/{exerciseId} without a body", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => undefined);
      const repository = new WorkoutRepository(httpClient);

      // Act
      await repository.removeExercise(workout.id, workoutExercise.exerciseId);

      // Assert
      expect(httpClient.requests).toEqual([
        {
          method: "DELETE",
          path: `/workouts/${workout.id}/exercises/${workoutExercise.exerciseId}`,
        },
      ]);
    });

    it("returns an empty success when the API answers 204", async () => {
      // Arrange
      const repository = new WorkoutRepository(new FakeHttpClient(async () => undefined));

      // Act
      const result = await repository.removeExercise(workout.id, workoutExercise.exerciseId);

      // Assert
      expect(result).toEqual(ok(undefined));
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
      const repository = new WorkoutRepository(failingWith(status));

      // Act
      const result = await repository.removeExercise(workout.id, workoutExercise.exerciseId);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code }) });
    });

    it("returns the network code when there is no response", async () => {
      // Arrange
      const repository = new WorkoutRepository(unreachable());

      // Act
      const result = await repository.removeExercise(workout.id, workoutExercise.exerciseId);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "NETWORK" }) });
    });
  });

  describe("reorderExercises", () => {
    it("puts the request to /workouts/{workoutId}/exercises/order unchanged", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => workoutDetail);
      const repository = new WorkoutRepository(httpClient);

      // Act
      await repository.reorderExercises(workout.id, reorderRequest);

      // Assert
      expect(httpClient.requests).toEqual([
        {
          method: "PUT",
          path: `/workouts/${workout.id}/exercises/order`,
          body: reorderRequest,
        },
      ]);
    });

    it("returns the reordered workout on success", async () => {
      // Arrange
      const repository = new WorkoutRepository(new FakeHttpClient(async () => workoutDetail));

      // Act
      const result = await repository.reorderExercises(workout.id, reorderRequest);

      // Assert
      expect(result).toEqual(ok(workoutDetail));
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
      const repository = new WorkoutRepository(failingWith(status));

      // Act
      const result = await repository.reorderExercises(workout.id, reorderRequest);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code }) });
    });

    it("returns the network code when there is no response", async () => {
      // Arrange
      const repository = new WorkoutRepository(unreachable());

      // Act
      const result = await repository.reorderExercises(workout.id, reorderRequest);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "NETWORK" }) });
    });
  });
});
