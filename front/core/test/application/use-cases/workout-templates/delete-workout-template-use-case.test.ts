import { describe, expect, it } from "vitest";
import type { WorkoutTemplateRepositoryInterface } from "../../../../src/application";
import { DeleteWorkoutTemplateUseCase } from "../../../../src/application";
import { DomainError, fail, ok, type Result } from "../../../../src/domain";

const templateId = "8d7c6b5a-4f3e-4d2c-9b1a-0f9e8d7c6b5a";

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

describe("DeleteWorkoutTemplateUseCase", () => {
  it("returns what the repository answers with", async () => {
    // Arrange
    const answer = ok(undefined);
    const useCase = new DeleteWorkoutTemplateUseCase(new FakeWorkoutTemplateRepository(answer));

    // Act
    const result = await useCase.execute({ id: templateId });

    // Assert
    expect(result).toEqual(answer);
  });

  it("forwards the template id, once", async () => {
    // Arrange
    const repository = new FakeWorkoutTemplateRepository(ok(undefined));
    const useCase = new DeleteWorkoutTemplateUseCase(repository);

    // Act
    await useCase.execute({ id: templateId });

    // Assert
    expect(repository.calls).toEqual([{ method: "delete", args: [templateId] }]);
  });

  it.each(["VALIDATION", "FORBIDDEN", "NOT_FOUND", "CONFLICT"] as const)(
    "returns the repository failure %s unchanged",
    async (code) => {
      // Arrange
      const failure = fail(new DomainError(code, "Rejected"));
      const useCase = new DeleteWorkoutTemplateUseCase(new FakeWorkoutTemplateRepository(failure));

      // Act
      const result = await useCase.execute({ id: templateId });

      // Assert
      expect(result).toBe(failure);
    },
  );
});
