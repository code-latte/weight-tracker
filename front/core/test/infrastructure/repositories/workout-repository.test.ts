import { describe, expect, it } from "vitest";
import type { HttpClientInterface, HttpRequest } from "../../../src/application";
import {
  ok,
  type AddExerciseRequest,
  type AddOrUpdateSetRecordRequest,
  type ChangeWorkoutStatusRequest,
  type CreateSuperSeriesRequest,
  type CreateWorkoutRequest,
  type ReorderSetRecordsRequest,
  type ReorderWorkoutExercisesRequest,
  type ReviewWorkoutRequest,
  type SetRecord,
  type UpdateWorkoutRequest,
  type Workout,
  type WorkoutById,
  type WorkoutDetail,
  type WorkoutExerciseDetail,
  type WorkoutListItem,
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
  mesocycleId: null,
  mesocycleWeekNumber: null,
  startedAt: null,
  completedAt: null,
  reviewedAt: null,
  reviewedBy: null,
  resultsUpdatedAt: null,
  exercises: [],
};

const placement = { mesocycleId: "9c8b7a6d-5e4f-4a3b-8c2d-1e0f9a8b7c6d", mesocycleWeekNumber: 2 };

const placedWorkout: Workout = { ...workout, ...placement };

const listedPerson = {
  id: "5f1c2f0e-9d5b-4a53-8c7e-0d6f7c1a2b3c",
  email: "ana@example.com",
  name: "Ana",
  surname: "García",
  role: "USER",
};

const listedWorkout: WorkoutListItem = {
  ...workout,
  athlete: listedPerson,
  trainer: null,
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
  setRecords: [],
};

const workoutDetail: WorkoutDetail = {
  ...workout,
  exercises: [workoutExercise],
};

const workoutById: WorkoutById = {
  ...workoutDetail,
  exercises: [
    {
      ...workoutExercise,
      translations: [{ languageCode: "es", name: "Sentadilla" }],
      videoUrl: null,
      muscle: "QUADRICEPS",
      bodyPart: "LEGS",
      type: "STRENGTH",
    },
  ],
  trainerId: "6a2d3f1e-0e6c-4b64-9d8f-1e7a8d2b3c4d",
  athlete: listedPerson,
  trainer: { ...listedPerson, id: "6a2d3f1e-0e6c-4b64-9d8f-1e7a8d2b3c4d", role: "TRAINER" },
  mesocycle: null,
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

const createSuperSeriesRequest: CreateSuperSeriesRequest = {
  exerciseIds: [workoutExercise.id, "3b4c5d6e-7f80-4a1b-9c2d-3e4f5a6b7c8d"],
};

const superSeriesId = 3;

const setRecord: SetRecord = {
  id: "4c5d6e7f-8091-4b2c-9d3e-4f5a6b7c8d9e",
  type: "Reps",
  reps: 8,
  durationSeconds: null,
  weight: 60.5,
  registeredAtUtc: "2026-11-02T10:15:00Z",
};

const workoutWithResults: WorkoutDetail = {
  ...workoutDetail,
  status: "STARTED",
  resultsUpdatedAt: setRecord.registeredAtUtc,
  exercises: [{ ...workoutExercise, setRecords: [setRecord] }],
};

const setRecordRequest: AddOrUpdateSetRecordRequest = { reps: 8, weight: 60.5 };

const timedSetRecordRequest: AddOrUpdateSetRecordRequest = { durationSeconds: 45 };

const reorderSetRecordsRequest: ReorderSetRecordsRequest = {
  setRecordIds: ["5d6e7f80-91a2-4c3d-8e4f-5a6b7c8d9e0f", setRecord.id],
};

const reviewRequest: ReviewWorkoutRequest = { resultsUpdatedAt: setRecord.registeredAtUtc };

const changeStatusRequest: ChangeWorkoutStatusRequest = { status: "STARTED" };

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
      const httpClient = new FakeHttpClient(async () => [listedWorkout]);
      const repository = new WorkoutRepository(httpClient);

      // Act
      await repository.getAll();

      // Assert
      expect(httpClient.requests).toEqual([{ method: "GET", path: "/workouts" }]);
    });

    it("asks only for the workouts outside a mesocycle when standalone is set", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => [listedWorkout]);
      const repository = new WorkoutRepository(httpClient);

      // Act
      await repository.getAll({ standalone: true });

      // Assert
      expect(httpClient.requests).toEqual([{ method: "GET", path: "/workouts?standalone=true" }]);
    });

    it("gets /workouts when standalone is false", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => [listedWorkout]);
      const repository = new WorkoutRepository(httpClient);

      // Act
      await repository.getAll({ standalone: false });

      // Assert
      expect(httpClient.requests).toEqual([{ method: "GET", path: "/workouts" }]);
    });

    it("returns the workouts on success", async () => {
      // Arrange
      const repository = new WorkoutRepository(new FakeHttpClient(async () => [listedWorkout]));

      // Act
      const result = await repository.getAll();

      // Assert
      expect(result).toEqual(ok([listedWorkout]));
    });

    it("keeps the athlete and the trainer of each workout, and a missing trainer as null", async () => {
      // Arrange
      const prescribed: WorkoutListItem = {
        ...listedWorkout,
        trainerId: "6a2d3f1e-0e6c-4b64-9d8f-1e7a8d2b3c4d",
        trainer: { ...listedPerson, id: "6a2d3f1e-0e6c-4b64-9d8f-1e7a8d2b3c4d", role: "TRAINER" },
      };
      const repository = new WorkoutRepository(
        new FakeHttpClient(async () => [listedWorkout, prescribed]),
      );

      // Act
      const result = await repository.getAll();

      // Assert
      expect(result).toEqual(ok([listedWorkout, prescribed]));
      expect(result.ok && result.value.map((item) => item.trainer)).toEqual([
        null,
        prescribed.trainer,
      ]);
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
      const httpClient = new FakeHttpClient(async () => workoutById);
      const repository = new WorkoutRepository(httpClient);

      // Act
      await repository.getById(workout.id);

      // Assert
      expect(httpClient.requests).toEqual([{ method: "GET", path: `/workouts/${workout.id}` }]);
    });

    it("returns the workout with its exercises, athlete and trainer on success", async () => {
      // Arrange
      const repository = new WorkoutRepository(new FakeHttpClient(async () => workoutById));

      // Act
      const result = await repository.getById(workout.id);

      // Assert
      expect(result).toEqual(ok(workoutById));
    });

    it("returns the session dates unchanged", async () => {
      // Arrange
      const completed = {
        ...workoutById,
        status: "COMPLETED",
        startedAt: "2026-11-13T10:00:00Z",
        completedAt: "2026-11-13T11:05:00Z",
      };
      const repository = new WorkoutRepository(new FakeHttpClient(async () => completed));

      // Act
      const result = await repository.getById(workout.id);

      // Assert
      expect(result).toEqual(ok(completed));
      expect(result.ok && result.value.startedAt).toBe("2026-11-13T10:00:00Z");
      expect(result.ok && result.value.completedAt).toBe("2026-11-13T11:05:00Z");
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

    it("sends the mesocycle placement unchanged", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => placedWorkout);
      const repository = new WorkoutRepository(httpClient);
      const placedRequest: CreateWorkoutRequest = { ...createRequest, ...placement };

      // Act
      await repository.create(placedRequest);

      // Assert
      expect(httpClient.requests).toEqual([
        { method: "POST", path: "/workouts", body: placedRequest },
      ]);
    });

    it("sends a placed workout without a date when none is given", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => placedWorkout);
      const repository = new WorkoutRepository(httpClient);
      const placedRequest: CreateWorkoutRequest = {
        name: "Pierna",
        userId: workout.userId,
        ...placement,
      };

      // Act
      await repository.create(placedRequest);

      // Assert
      expect(httpClient.requests).toEqual([
        { method: "POST", path: "/workouts", body: placedRequest },
      ]);
      expect(httpClient.requests[0]?.body).not.toHaveProperty("workoutDate");
    });

    it("sends no mesocycle placement when it is omitted", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => workout);
      const repository = new WorkoutRepository(httpClient);

      // Act
      await repository.create(createRequest);

      // Assert
      expect(httpClient.requests[0]?.body).not.toHaveProperty("mesocycleId");
      expect(httpClient.requests[0]?.body).not.toHaveProperty("mesocycleWeekNumber");
    });

    it("returns the mesocycle placement the API answers with", async () => {
      // Arrange
      const repository = new WorkoutRepository(new FakeHttpClient(async () => placedWorkout));

      // Act
      const result = await repository.create(createRequest);

      // Assert
      expect(result).toEqual(ok(placedWorkout));
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
      [409, "CONFLICT"],
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

    it("sends the mesocycle placement unchanged", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => placedWorkout);
      const repository = new WorkoutRepository(httpClient);
      const placedRequest: UpdateWorkoutRequest = { ...updateRequest, ...placement };

      // Act
      await repository.update(workout.id, placedRequest);

      // Assert
      expect(httpClient.requests).toEqual([
        { method: "PUT", path: `/workouts/${workout.id}`, body: placedRequest },
      ]);
    });

    it("sends no mesocycle placement when it is omitted", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => workout);
      const repository = new WorkoutRepository(httpClient);

      // Act
      await repository.update(workout.id, updateRequest);

      // Assert
      expect(httpClient.requests[0]?.body).not.toHaveProperty("mesocycleId");
      expect(httpClient.requests[0]?.body).not.toHaveProperty("mesocycleWeekNumber");
    });

    it("returns the mesocycle placement the API answers with", async () => {
      // Arrange
      const repository = new WorkoutRepository(new FakeHttpClient(async () => placedWorkout));

      // Act
      const result = await repository.update(workout.id, { ...updateRequest, ...placement });

      // Assert
      expect(result).toEqual(ok(placedWorkout));
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

  describe("delete", () => {
    it("deletes /workouts/{id} without a body", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => undefined);
      const repository = new WorkoutRepository(httpClient);

      // Act
      await repository.delete(workout.id);

      // Assert
      expect(httpClient.requests).toEqual([{ method: "DELETE", path: `/workouts/${workout.id}` }]);
    });

    it("returns an empty success when the API answers 204", async () => {
      // Arrange
      const repository = new WorkoutRepository(new FakeHttpClient(async () => undefined));

      // Act
      const result = await repository.delete(workout.id);

      // Assert
      expect(result).toEqual(ok(undefined));
    });

    it.each([
      [401, "UNAUTHORIZED"],
      [403, "FORBIDDEN"],
      [404, "NOT_FOUND"],
      [409, "CONFLICT"],
      [500, "UNEXPECTED"],
    ])("maps status %i to %s", async (status, code) => {
      // Arrange
      const repository = new WorkoutRepository(failingWith(status));

      // Act
      const result = await repository.delete(workout.id);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code }) });
    });

    it("returns the network code when there is no response", async () => {
      // Arrange
      const repository = new WorkoutRepository(unreachable());

      // Act
      const result = await repository.delete(workout.id);

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

  describe("createSuperSeries", () => {
    it("posts the request to /workouts/{workoutId}/superseries unchanged", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => ({ id: superSeriesId }));
      const repository = new WorkoutRepository(httpClient);

      // Act
      await repository.createSuperSeries(workout.id, createSuperSeriesRequest);

      // Assert
      expect(httpClient.requests).toEqual([
        {
          method: "POST",
          path: `/workouts/${workout.id}/superseries`,
          body: createSuperSeriesRequest,
        },
      ]);
    });

    it("returns the created superseries on success", async () => {
      // Arrange
      const repository = new WorkoutRepository(
        new FakeHttpClient(async () => ({ id: superSeriesId })),
      );

      // Act
      const result = await repository.createSuperSeries(workout.id, createSuperSeriesRequest);

      // Assert
      expect(result).toEqual(ok({ id: superSeriesId }));
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
      const result = await repository.createSuperSeries(workout.id, createSuperSeriesRequest);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code }) });
    });

    it("returns the network code when there is no response", async () => {
      // Arrange
      const repository = new WorkoutRepository(unreachable());

      // Act
      const result = await repository.createSuperSeries(workout.id, createSuperSeriesRequest);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "NETWORK" }) });
    });
  });

  describe("breakSuperSeries", () => {
    it("deletes /workouts/{workoutId}/superseries/{superSeriesId} without a body", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => undefined);
      const repository = new WorkoutRepository(httpClient);

      // Act
      await repository.breakSuperSeries(workout.id, superSeriesId);

      // Assert
      expect(httpClient.requests).toEqual([
        { method: "DELETE", path: `/workouts/${workout.id}/superseries/${superSeriesId}` },
      ]);
    });

    it("returns an empty success when the API answers 204", async () => {
      // Arrange
      const repository = new WorkoutRepository(new FakeHttpClient(async () => undefined));

      // Act
      const result = await repository.breakSuperSeries(workout.id, superSeriesId);

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
      const result = await repository.breakSuperSeries(workout.id, superSeriesId);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code }) });
    });

    it("returns the network code when there is no response", async () => {
      // Arrange
      const repository = new WorkoutRepository(unreachable());

      // Act
      const result = await repository.breakSuperSeries(workout.id, superSeriesId);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "NETWORK" }) });
    });
  });

  describe("addSetRecord", () => {
    it("posts a timed set without a weight unchanged", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => workoutWithResults);
      const repository = new WorkoutRepository(httpClient);

      // Act
      await repository.addSetRecord(workout.id, workoutExercise.exerciseId, timedSetRecordRequest);

      // Assert
      expect(httpClient.requests).toEqual([
        {
          method: "POST",
          path: `/workouts/${workout.id}/exercises/${workoutExercise.exerciseId}/set-records`,
          body: { durationSeconds: 45 },
        },
      ]);
    });

    it("returns a timed record as the API answered it, nulls included", async () => {
      // Arrange
      const timed: SetRecord = {
        ...setRecord,
        type: "Duration",
        reps: null,
        durationSeconds: 45,
        weight: null,
      };
      const detail = {
        ...workoutWithResults,
        exercises: [{ ...workoutExercise, setRecords: [timed] }],
      };
      const repository = new WorkoutRepository(new FakeHttpClient(async () => detail));

      // Act
      const result = await repository.addSetRecord(
        workout.id,
        workoutExercise.exerciseId,
        timedSetRecordRequest,
      );

      // Assert
      expect(result).toEqual(ok(detail));
    });

    it("posts the request to /workouts/{workoutId}/exercises/{exerciseId}/set-records unchanged", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => workoutWithResults);
      const repository = new WorkoutRepository(httpClient);

      // Act
      await repository.addSetRecord(workout.id, workoutExercise.exerciseId, setRecordRequest);

      // Assert
      expect(httpClient.requests).toEqual([
        {
          method: "POST",
          path: `/workouts/${workout.id}/exercises/${workoutExercise.exerciseId}/set-records`,
          body: setRecordRequest,
        },
      ]);
    });

    it("returns the workout with its set records on success", async () => {
      // Arrange
      const repository = new WorkoutRepository(new FakeHttpClient(async () => workoutWithResults));

      // Act
      const result = await repository.addSetRecord(
        workout.id,
        workoutExercise.exerciseId,
        setRecordRequest,
      );

      // Assert
      expect(result).toEqual(ok(workoutWithResults));
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
      const result = await repository.addSetRecord(
        workout.id,
        workoutExercise.exerciseId,
        setRecordRequest,
      );

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code }) });
    });

    it("returns the network code when there is no response", async () => {
      // Arrange
      const repository = new WorkoutRepository(unreachable());

      // Act
      const result = await repository.addSetRecord(
        workout.id,
        workoutExercise.exerciseId,
        setRecordRequest,
      );

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "NETWORK" }) });
    });
  });

  describe("updateSetRecord", () => {
    it("puts a corrected duration without a weight unchanged", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => workoutWithResults);
      const repository = new WorkoutRepository(httpClient);

      // Act
      await repository.updateSetRecord(workout.id, workoutExercise.exerciseId, setRecord.id, {
        durationSeconds: 60,
      });

      // Assert
      expect(httpClient.requests).toEqual([
        {
          method: "PUT",
          path: `/workouts/${workout.id}/exercises/${workoutExercise.exerciseId}/set-records/${setRecord.id}`,
          body: { durationSeconds: 60 },
        },
      ]);
    });

    it("puts the request to .../set-records/{setRecordId} unchanged", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => workoutWithResults);
      const repository = new WorkoutRepository(httpClient);

      // Act
      await repository.updateSetRecord(
        workout.id,
        workoutExercise.exerciseId,
        setRecord.id,
        setRecordRequest,
      );

      // Assert
      expect(httpClient.requests).toEqual([
        {
          method: "PUT",
          path: `/workouts/${workout.id}/exercises/${workoutExercise.exerciseId}/set-records/${setRecord.id}`,
          body: setRecordRequest,
        },
      ]);
    });

    it("returns the workout with the corrected set record on success", async () => {
      // Arrange
      const repository = new WorkoutRepository(new FakeHttpClient(async () => workoutWithResults));

      // Act
      const result = await repository.updateSetRecord(
        workout.id,
        workoutExercise.exerciseId,
        setRecord.id,
        setRecordRequest,
      );

      // Assert
      expect(result).toEqual(ok(workoutWithResults));
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
      const result = await repository.updateSetRecord(
        workout.id,
        workoutExercise.exerciseId,
        setRecord.id,
        setRecordRequest,
      );

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code }) });
    });

    it("returns the network code when there is no response", async () => {
      // Arrange
      const repository = new WorkoutRepository(unreachable());

      // Act
      const result = await repository.updateSetRecord(
        workout.id,
        workoutExercise.exerciseId,
        setRecord.id,
        setRecordRequest,
      );

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "NETWORK" }) });
    });
  });

  describe("removeSetRecord", () => {
    it("deletes .../set-records/{setRecordId} without a body", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => workoutWithResults);
      const repository = new WorkoutRepository(httpClient);

      // Act
      await repository.removeSetRecord(workout.id, workoutExercise.exerciseId, setRecord.id);

      // Assert
      expect(httpClient.requests).toEqual([
        {
          method: "DELETE",
          path: `/workouts/${workout.id}/exercises/${workoutExercise.exerciseId}/set-records/${setRecord.id}`,
        },
      ]);
    });

    it("returns the workout the API answers with 200", async () => {
      // Arrange
      const repository = new WorkoutRepository(new FakeHttpClient(async () => workoutWithResults));

      // Act
      const result = await repository.removeSetRecord(
        workout.id,
        workoutExercise.exerciseId,
        setRecord.id,
      );

      // Assert
      expect(result).toEqual(ok(workoutWithResults));
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
      const result = await repository.removeSetRecord(
        workout.id,
        workoutExercise.exerciseId,
        setRecord.id,
      );

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code }) });
    });

    it("returns the network code when there is no response", async () => {
      // Arrange
      const repository = new WorkoutRepository(unreachable());

      // Act
      const result = await repository.removeSetRecord(
        workout.id,
        workoutExercise.exerciseId,
        setRecord.id,
      );

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "NETWORK" }) });
    });
  });

  describe("reorderSetRecords", () => {
    it("puts the request to .../set-records/order unchanged", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => workoutWithResults);
      const repository = new WorkoutRepository(httpClient);

      // Act
      await repository.reorderSetRecords(
        workout.id,
        workoutExercise.exerciseId,
        reorderSetRecordsRequest,
      );

      // Assert
      expect(httpClient.requests).toEqual([
        {
          method: "PUT",
          path: `/workouts/${workout.id}/exercises/${workoutExercise.exerciseId}/set-records/order`,
          body: reorderSetRecordsRequest,
        },
      ]);
    });

    it("returns the reordered workout on success", async () => {
      // Arrange
      const repository = new WorkoutRepository(new FakeHttpClient(async () => workoutWithResults));

      // Act
      const result = await repository.reorderSetRecords(
        workout.id,
        workoutExercise.exerciseId,
        reorderSetRecordsRequest,
      );

      // Assert
      expect(result).toEqual(ok(workoutWithResults));
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
      const result = await repository.reorderSetRecords(
        workout.id,
        workoutExercise.exerciseId,
        reorderSetRecordsRequest,
      );

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code }) });
    });

    it("returns the network code when there is no response", async () => {
      // Arrange
      const repository = new WorkoutRepository(unreachable());

      // Act
      const result = await repository.reorderSetRecords(
        workout.id,
        workoutExercise.exerciseId,
        reorderSetRecordsRequest,
      );

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "NETWORK" }) });
    });
  });

  describe("review", () => {
    it("puts the request to /workouts/{workoutId}/review unchanged", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => workoutWithResults);
      const repository = new WorkoutRepository(httpClient);

      // Act
      await repository.review(workout.id, reviewRequest);

      // Assert
      expect(httpClient.requests).toEqual([
        {
          method: "PUT",
          path: `/workouts/${workout.id}/review`,
          body: reviewRequest,
        },
      ]);
    });

    it("returns the reviewed workout on success", async () => {
      // Arrange
      const repository = new WorkoutRepository(new FakeHttpClient(async () => workoutWithResults));

      // Act
      const result = await repository.review(workout.id, reviewRequest);

      // Assert
      expect(result).toEqual(ok(workoutWithResults));
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
      const result = await repository.review(workout.id, reviewRequest);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code }) });
    });

    it("keeps a null resultsUpdatedAt in the body", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => workoutWithResults);
      const repository = new WorkoutRepository(httpClient);

      // Act
      await repository.review(workout.id, { resultsUpdatedAt: null });

      // Assert
      expect(httpClient.requests[0]?.body).toEqual({ resultsUpdatedAt: null });
    });

    it("returns the network code when there is no response", async () => {
      // Arrange
      const repository = new WorkoutRepository(unreachable());

      // Act
      const result = await repository.review(workout.id, reviewRequest);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "NETWORK" }) });
    });
  });

  describe("changeStatus", () => {
    it("puts the request to /workouts/{workoutId}/status unchanged", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => workoutWithResults);
      const repository = new WorkoutRepository(httpClient);

      // Act
      await repository.changeStatus(workout.id, changeStatusRequest);

      // Assert
      expect(httpClient.requests).toEqual([
        {
          method: "PUT",
          path: `/workouts/${workout.id}/status`,
          body: changeStatusRequest,
        },
      ]);
    });

    it("returns the workout in its new status on success", async () => {
      // Arrange
      const repository = new WorkoutRepository(new FakeHttpClient(async () => workoutWithResults));

      // Act
      const result = await repository.changeStatus(workout.id, changeStatusRequest);

      // Assert
      expect(result).toEqual(ok(workoutWithResults));
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
      const result = await repository.changeStatus(workout.id, changeStatusRequest);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code }) });
    });

    it("returns the network code when there is no response", async () => {
      // Arrange
      const repository = new WorkoutRepository(unreachable());

      // Act
      const result = await repository.changeStatus(workout.id, changeStatusRequest);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "NETWORK" }) });
    });
  });
});
