import { describe, expect, it } from "vitest";
import type { ExerciseRepositoryInterface } from "../../../../src/application";
import { CreateExerciseUseCase } from "../../../../src/application";
import {
  type CreateExerciseRequest,
  DomainError,
  fail,
  ok,
  type Exercise,
  type Result,
} from "../../../../src/domain";

const request: CreateExerciseRequest = {
  languageCode: "es",
  name: "Remo con banda",
  muscle: "MIDDLE_BACK",
  type: "STRENGTH",
};

const exercise: Exercise = {
  id: "6a7b8c9d-0e1f-4a2b-8c3d-4e5f6a7b8c9d",
  freeExerciseDbId: null,
  videoUrl: null,
  muscle: "MIDDLE_BACK",
  bodyPart: "BACK",
  type: "STRENGTH",
  translations: [{ languageCode: "es", name: "Remo con banda" }],
  ownerId: "5f1c2f0e-9d5b-4a53-8c7e-0d6f7c1a2b3c",
  archivedAtUtc: null,
};

class FakeExerciseRepository implements ExerciseRepositoryInterface {
  readonly created: CreateExerciseRequest[] = [];

  constructor(private readonly result: Result<Exercise, DomainError>) {}

  async getAll(): Promise<Result<Exercise[], DomainError>> {
    return ok([]);
  }

  async create(input: CreateExerciseRequest): Promise<Result<Exercise, DomainError>> {
    this.created.push(input);
    return this.result;
  }
}

describe("CreateExerciseUseCase", () => {
  it("returns the exercise the repository answers with", async () => {
    // Arrange
    const useCase = new CreateExerciseUseCase(new FakeExerciseRepository(ok(exercise)));

    // Act
    const result = await useCase.execute(request);

    // Assert
    expect(result).toEqual(ok(exercise));
  });

  it("forwards the request to the repository unchanged", async () => {
    // Arrange
    const repository = new FakeExerciseRepository(ok(exercise));
    const useCase = new CreateExerciseUseCase(repository);

    // Act
    await useCase.execute(request);

    // Assert
    expect(repository.created).toEqual([request]);
  });

  it("returns the repository's failure", async () => {
    // Arrange
    const error = new DomainError("FORBIDDEN", "Not allowed");
    const useCase = new CreateExerciseUseCase(new FakeExerciseRepository(fail(error)));

    // Act
    const result = await useCase.execute(request);

    // Assert
    expect(result).toEqual(fail(error));
  });
});
