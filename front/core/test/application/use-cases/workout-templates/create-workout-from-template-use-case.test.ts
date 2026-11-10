import { describe, expect, it } from "vitest";
import type { WorkoutTemplateRepositoryInterface } from "../../../../src/application";
import { CreateWorkoutFromTemplateUseCase } from "../../../../src/application";
import { DomainError, fail, ok, type Result, type Workout } from "../../../../src/domain";

const templateId = "8d7c6b5a-4f3e-4d2c-9b1a-0f9e8d7c6b5a";
const workoutId = "7b0c1f7e-3a54-4d4e-9d0e-1a2b3c4d5e6f";
const trainerId = "6a2d3f1e-0e6c-4b64-9d8f-1e7a8d2b3c4d";
const workout: Workout = {
  id: workoutId,
  name: "Pierna A",
  description: null,
  workoutDate: "2026-11-04",
  userId: "5f1c2f0e-9d5b-4a53-8c7e-0d6f7c1a2b3c",
  trainerId,
  status: "DRAFT",
  mesocycleId: null,
  mesocycleWeekNumber: null,
  completedAt: null,
  reviewedAt: null,
  reviewedBy: null,
  resultsUpdatedAt: null,
  exercises: [],
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

describe("CreateWorkoutFromTemplateUseCase", () => {
  it("returns what the repository answers with", async () => {
    // Arrange
    const answer = ok(workout);
    const useCase = new CreateWorkoutFromTemplateUseCase(new FakeWorkoutTemplateRepository(answer));

    // Act
    const result = await useCase.execute({
      templateId,
      userId: workout.userId,
      mesocycleId: "m-1",
      mesocycleWeekNumber: 2,
    });

    // Assert
    expect(result).toEqual(answer);
  });

  it("splits the template id from the placement, unchanged, once", async () => {
    // Arrange
    const repository = new FakeWorkoutTemplateRepository(ok(workout));
    const useCase = new CreateWorkoutFromTemplateUseCase(repository);

    // Act
    await useCase.execute({
      templateId,
      userId: workout.userId,
      mesocycleId: "m-1",
      mesocycleWeekNumber: 2,
    });

    // Assert
    expect(repository.calls).toEqual([
      {
        method: "createWorkout",
        args: [templateId, { userId: workout.userId, mesocycleId: "m-1", mesocycleWeekNumber: 2 }],
      },
    ]);
  });

  it.each(["VALIDATION", "FORBIDDEN", "NOT_FOUND", "CONFLICT"] as const)(
    "returns the repository failure %s unchanged",
    async (code) => {
      // Arrange
      const failure = fail(new DomainError(code, "Rejected"));
      const useCase = new CreateWorkoutFromTemplateUseCase(
        new FakeWorkoutTemplateRepository(failure),
      );

      // Act
      const result = await useCase.execute({
        templateId,
        userId: workout.userId,
        mesocycleId: "m-1",
        mesocycleWeekNumber: 2,
      });

      // Assert
      expect(result).toBe(failure);
    },
  );
});
