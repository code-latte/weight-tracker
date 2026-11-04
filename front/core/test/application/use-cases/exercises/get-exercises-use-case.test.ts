import { describe, expect, it } from "vitest";
import type { ExerciseRepositoryInterface } from "../../../../src/application";
import { GetExercisesUseCase } from "../../../../src/application";
import { DomainError, fail, ok, type Exercise, type Result } from "../../../../src/domain";

const exercise: Exercise = {
  id: "1f2e3d4c-5b6a-4978-8695-a4b3c2d1e0f9",
  freeExerciseDbId: "Barbell_Squat",
  videoUrl: null,
  muscle: "quadriceps",
  bodyPart: "LEGS",
  type: "STRENGTH",
  translations: [],
  ownerId: null,
  archivedAtUtc: null,
};

class FakeExerciseRepository implements ExerciseRepositoryInterface {
  getAllCalls = 0;

  constructor(private readonly result: Result<Exercise[], DomainError>) {}

  async getAll(): Promise<Result<Exercise[], DomainError>> {
    this.getAllCalls += 1;
    return this.result;
  }

  async create(): Promise<Result<Exercise, DomainError>> {
    return fail(new DomainError("UNEXPECTED", "Not used by this use case"));
  }
}

describe("GetExercisesUseCase", () => {
  it("returns the exercises the repository answers with", async () => {
    // Arrange
    const useCase = new GetExercisesUseCase(new FakeExerciseRepository(ok([exercise])));

    // Act
    const result = await useCase.execute();

    // Assert
    expect(result).toEqual(ok([exercise]));
  });

  it("calls the repository once", async () => {
    // Arrange
    const repository = new FakeExerciseRepository(ok([exercise]));
    const useCase = new GetExercisesUseCase(repository);

    // Act
    await useCase.execute();

    // Assert
    expect(repository.getAllCalls).toBe(1);
  });

  it("returns the repository failure unchanged", async () => {
    // Arrange
    const failure = fail(new DomainError("NETWORK", "Failed to fetch"));
    const useCase = new GetExercisesUseCase(new FakeExerciseRepository(failure));

    // Act
    const result = await useCase.execute();

    // Assert
    expect(result).toBe(failure);
  });
});
