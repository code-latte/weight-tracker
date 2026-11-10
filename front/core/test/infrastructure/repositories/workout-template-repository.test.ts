import { describe, expect, it } from "vitest";
import type {
  HttpClientInterface,
  HttpRequest,
  WorkoutTemplateRepositoryInterface,
} from "../../../src/application";
import {
  ok,
  type AddExerciseRequest,
  type CreateSuperSeriesRequest,
  type CreateWorkoutFromTemplateRequest,
  type CreateWorkoutTemplateRequest,
  type ReorderWorkoutExercisesRequest,
  type UpdateWorkoutTemplateRequest,
  type Workout,
  type WorkoutExerciseDetail,
  type WorkoutTemplate,
} from "../../../src/domain";
import { HttpError, WorkoutTemplateRepository } from "../../../src/infrastructure";

const templateExercise: WorkoutExerciseDetail = {
  id: "0a1b2c3d-4e5f-4a6b-8c7d-9e0f1a2b3c4d",
  exerciseId: "1f2e3d4c-5b6a-4978-8695-a4b3c2d1e0f9",
  effort: "HIGH",
  perSide: false,
  restMinutes: null,
  sets: [{ count: 3, target: { type: "REPS", value: 10, maxValue: null } }],
  superSeriesId: null,
  setRecords: [],
};

const template: WorkoutTemplate = {
  id: "8d7c6b5a-4f3e-4d2c-9b1a-0f9e8d7c6b5a",
  name: "Pierna A",
  description: "Fuerza",
  trainerId: "6a2d3f1e-0e6c-4b64-9d8f-1e7a8d2b3c4d",
  exercises: [templateExercise],
};

const workout: Workout = {
  id: "7b0c1f7e-3a54-4d4e-9d0e-1a2b3c4d5e6f",
  name: "Pierna A",
  description: "Fuerza",
  workoutDate: "2026-11-04",
  userId: "5f1c2f0e-9d5b-4a53-8c7e-0d6f7c1a2b3c",
  trainerId: template.trainerId,
  status: "DRAFT",
  mesocycleId: null,
  mesocycleWeekNumber: null,
  completedAt: null,
  reviewedAt: null,
  reviewedBy: null,
  resultsUpdatedAt: null,
  exercises: [],
};

const createRequest: CreateWorkoutTemplateRequest = { name: "Pierna A", description: "Fuerza" };

const updateRequest: UpdateWorkoutTemplateRequest = { name: "Pierna B", description: null };

const addExerciseRequest: AddExerciseRequest = {
  exerciseId: templateExercise.exerciseId,
  effort: "HIGH",
  sets: [{ count: 3, targetType: "REPS", target: 10 }],
  perSide: false,
};

const reorderRequest: ReorderWorkoutExercisesRequest = {
  exerciseIds: [templateExercise.exerciseId, "2a3b4c5d-6e7f-4809-9a1b-2c3d4e5f6a7b"],
};

const createSuperSeriesRequest: CreateSuperSeriesRequest = {
  exerciseIds: [templateExercise.id, "3b4c5d6e-7f80-4a1b-9c2d-3e4f5a6b7c8d"],
};

const superSeriesId = 3;

const createWorkoutRequest: CreateWorkoutFromTemplateRequest = {
  userId: workout.userId,
  workoutDate: "2026-11-04",
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

/** One repository method: how to call it, the request it must send, and what the API answers on success. */
interface Operation {
  name: string;
  call: (repository: WorkoutTemplateRepositoryInterface) => Promise<unknown>;
  request: HttpRequest;
  answer: unknown;
}

const operations: Operation[] = [
  {
    name: "getAll",
    call: (repository) => repository.getAll(),
    request: { method: "GET", path: "/workout-templates" },
    answer: [template],
  },
  {
    name: "getById",
    call: (repository) => repository.getById(template.id),
    request: { method: "GET", path: `/workout-templates/${template.id}` },
    answer: template,
  },
  {
    name: "create",
    call: (repository) => repository.create(createRequest),
    request: { method: "POST", path: "/workout-templates", body: createRequest },
    answer: template,
  },
  {
    name: "update",
    call: (repository) => repository.update(template.id, updateRequest),
    request: { method: "PUT", path: `/workout-templates/${template.id}`, body: updateRequest },
    answer: { ...template, name: "Pierna B", description: null },
  },
  {
    name: "delete",
    call: (repository) => repository.delete(template.id),
    request: { method: "DELETE", path: `/workout-templates/${template.id}` },
    answer: undefined,
  },
  {
    name: "addExercise",
    call: (repository) => repository.addExercise(template.id, addExerciseRequest),
    request: {
      method: "POST",
      path: `/workout-templates/${template.id}/exercises`,
      body: addExerciseRequest,
    },
    answer: templateExercise,
  },
  {
    name: "updateExercise",
    call: (repository) =>
      repository.updateExercise(template.id, templateExercise.exerciseId, addExerciseRequest),
    request: {
      method: "PUT",
      path: `/workout-templates/${template.id}/exercises/${templateExercise.exerciseId}`,
      body: addExerciseRequest,
    },
    answer: templateExercise,
  },
  {
    name: "removeExercise",
    call: (repository) => repository.removeExercise(template.id, templateExercise.exerciseId),
    request: {
      method: "DELETE",
      path: `/workout-templates/${template.id}/exercises/${templateExercise.exerciseId}`,
    },
    answer: undefined,
  },
  {
    name: "reorderExercises",
    call: (repository) => repository.reorderExercises(template.id, reorderRequest),
    request: {
      method: "PUT",
      path: `/workout-templates/${template.id}/exercises/order`,
      body: reorderRequest,
    },
    answer: template,
  },
  {
    name: "createSuperSeries",
    call: (repository) => repository.createSuperSeries(template.id, createSuperSeriesRequest),
    request: {
      method: "POST",
      path: `/workout-templates/${template.id}/superseries`,
      body: createSuperSeriesRequest,
    },
    answer: { id: superSeriesId },
  },
  {
    name: "breakSuperSeries",
    call: (repository) => repository.breakSuperSeries(template.id, superSeriesId),
    request: {
      method: "DELETE",
      path: `/workout-templates/${template.id}/superseries/${superSeriesId}`,
    },
    answer: undefined,
  },
  {
    name: "createWorkout",
    call: (repository) => repository.createWorkout(template.id, createWorkoutRequest),
    request: {
      method: "POST",
      path: `/workout-templates/${template.id}/workouts`,
      body: createWorkoutRequest,
    },
    answer: workout,
  },
  {
    name: "createFromWorkout",
    call: (repository) => repository.createFromWorkout(workout.id),
    request: { method: "POST", path: `/workouts/${workout.id}/templates` },
    answer: template,
  },
];

describe("WorkoutTemplateRepository", () => {
  describe.each(operations)("$name", ({ call, request, answer }) => {
    it("sends its request unchanged", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => answer);
      const repository = new WorkoutTemplateRepository(httpClient);

      // Act
      await call(repository);

      // Assert
      expect(httpClient.requests).toEqual([request]);
    });

    it("returns what the API answers on success", async () => {
      // Arrange
      const repository = new WorkoutTemplateRepository(new FakeHttpClient(async () => answer));

      // Act
      const result = await call(repository);

      // Assert
      expect(result).toEqual(ok(answer));
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
      const repository = new WorkoutTemplateRepository(failingWith(status));

      // Act
      const result = await call(repository);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code }) });
    });

    it("returns the network code when there is no response", async () => {
      // Arrange
      const repository = new WorkoutTemplateRepository(unreachable());

      // Act
      const result = await call(repository);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "NETWORK" }) });
    });
  });

  describe("getAll with a trainer", () => {
    it("sends the trainer id as the query", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => [template]);
      const repository = new WorkoutTemplateRepository(httpClient);

      // Act
      await repository.getAll({ trainerId: template.trainerId });

      // Assert
      expect(httpClient.requests).toEqual([
        { method: "GET", path: `/workout-templates?trainerId=${template.trainerId}` },
      ]);
    });
  });
});
