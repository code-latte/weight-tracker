import { describe, expect, it } from "vitest";
import type { ExerciseFavoriteRepositoryInterface } from "../../../../src/application";
import { MarkExerciseFavoriteUseCase } from "../../../../src/application";
import { DomainError, type ExerciseFavorite, fail, ok, type Result } from "../../../../src/domain";

const exerciseId = "1f2e3d4c-5b6a-4978-8695-a4b3c2d1e0f9";

const favorite: ExerciseFavorite = {
  exerciseId,
  createdAtUtc: "2026-11-03T09:30:00Z",
};

class FakeExerciseFavoriteRepository implements ExerciseFavoriteRepositoryInterface {
  readonly marked: string[] = [];
  otherCalls = 0;

  constructor(private readonly result: Result<ExerciseFavorite, DomainError>) {}

  async mark(id: string): Promise<Result<ExerciseFavorite, DomainError>> {
    this.marked.push(id);
    return this.result;
  }

  async getAll(): Promise<Result<ExerciseFavorite[], DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async unmark(): Promise<Result<void, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }
}

describe("MarkExerciseFavoriteUseCase", () => {
  it("returns the favorite the repository answers with", async () => {
    // Arrange
    const useCase = new MarkExerciseFavoriteUseCase(
      new FakeExerciseFavoriteRepository(ok(favorite)),
    );

    // Act
    const result = await useCase.execute(exerciseId);

    // Assert
    expect(result).toEqual(ok(favorite));
  });

  it("forwards the exercise id once and touches nothing else", async () => {
    // Arrange
    const repository = new FakeExerciseFavoriteRepository(ok(favorite));
    const useCase = new MarkExerciseFavoriteUseCase(repository);

    // Act
    await useCase.execute(exerciseId);

    // Assert
    expect([repository.marked, repository.otherCalls]).toEqual([[exerciseId], 0]);
  });

  it("returns the repository failure unchanged", async () => {
    // Arrange
    const failure = fail(new DomainError("CONFLICT", "The exercise is archived"));
    const useCase = new MarkExerciseFavoriteUseCase(new FakeExerciseFavoriteRepository(failure));

    // Act
    const result = await useCase.execute(exerciseId);

    // Assert
    expect(result).toBe(failure);
  });
});
