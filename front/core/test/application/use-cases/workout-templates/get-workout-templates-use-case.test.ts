import { describe, expect, it } from "vitest";
import type { WorkoutTemplateRepositoryInterface } from "../../../../src/application";
import { GetWorkoutTemplatesUseCase } from "../../../../src/application";
import {
  DomainError,
  fail,
  ok,
  type Result,
  type WorkoutExerciseDetail,
  type WorkoutTemplate,
} from "../../../../src/domain";

const templateId = "8d7c6b5a-4f3e-4d2c-9b1a-0f9e8d7c6b5a";
const trainerId = "6a2d3f1e-0e6c-4b64-9d8f-1e7a8d2b3c4d";
const exerciseId = "1f2e3d4c-5b6a-4978-8695-a4b3c2d1e0f9";

const entry: WorkoutExerciseDetail = {
  id: "0a1b2c3d-4e5f-4a6b-8c7d-9e0f1a2b3c4d",
  exerciseId,
  effort: "HIGH",
  perSide: false,
  restMinutes: null,
  sets: [{ count: 3, target: { type: "REPS", value: 10, maxValue: null } }],
  superSeriesId: null,
};

const template: WorkoutTemplate = {
  id: templateId,
  name: "Pierna A",
  description: null,
  trainerId,
  exercises: [entry],
};

/** Records every call by method name and answers each with the configured result. */
class FakeWorkoutTemplateRepository implements WorkoutTemplateRepositoryInterface {
  readonly calls: { method: string; args: unknown[] }[] = [];

  constructor(private readonly result: Result<unknown, DomainError>) {}

  private answer<T>(method: string, args: unknown[]): Promise<Result<T, DomainError>> {
    this.calls.push({ method, args });
    return Promise.resolve(this.result as Result<T, DomainError>);
  }

  getAll(...args: unknown[]) {
    return this.answer<never>("getAll", args);
  }

  getById(...args: unknown[]) {
    return this.answer<never>("getById", args);
  }

  create(...args: unknown[]) {
    return this.answer<never>("create", args);
  }

  update(...args: unknown[]) {
    return this.answer<never>("update", args);
  }

  delete(...args: unknown[]) {
    return this.answer<never>("delete", args);
  }

  addExercise(...args: unknown[]) {
    return this.answer<never>("addExercise", args);
  }

  updateExercise(...args: unknown[]) {
    return this.answer<never>("updateExercise", args);
  }

  removeExercise(...args: unknown[]) {
    return this.answer<never>("removeExercise", args);
  }

  reorderExercises(...args: unknown[]) {
    return this.answer<never>("reorderExercises", args);
  }

  createSuperSeries(...args: unknown[]) {
    return this.answer<never>("createSuperSeries", args);
  }

  breakSuperSeries(...args: unknown[]) {
    return this.answer<never>("breakSuperSeries", args);
  }

  createWorkout(...args: unknown[]) {
    return this.answer<never>("createWorkout", args);
  }

  createFromWorkout(...args: unknown[]) {
    return this.answer<never>("createFromWorkout", args);
  }
}

describe("GetWorkoutTemplatesUseCase", () => {
  it("returns what the repository answers with", async () => {
    // Arrange
    const answer = ok([template]);
    const useCase = new GetWorkoutTemplatesUseCase(new FakeWorkoutTemplateRepository(answer));

    // Act
    const result = await useCase.execute({ trainerId });

    // Assert
    expect(result).toEqual(answer);
  });

  it("forwards the optional trainer id, once", async () => {
    // Arrange
    const repository = new FakeWorkoutTemplateRepository(ok([template]));
    const useCase = new GetWorkoutTemplatesUseCase(repository);

    // Act
    await useCase.execute({ trainerId });

    // Assert
    expect(repository.calls).toEqual([{ method: "getAll", args: [{ trainerId }] }]);
  });

  it.each(["VALIDATION", "FORBIDDEN", "NOT_FOUND", "CONFLICT"] as const)(
    "returns the repository failure %s unchanged",
    async (code) => {
      // Arrange
      const failure = fail(new DomainError(code, "Rejected"));
      const useCase = new GetWorkoutTemplatesUseCase(new FakeWorkoutTemplateRepository(failure));

      // Act
      const result = await useCase.execute({ trainerId });

      // Assert
      expect(result).toBe(failure);
    },
  );
});
