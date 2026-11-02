import { describe, expect, it } from "vitest";
import type { AthleteInvitationRepositoryInterface } from "../../../../src/application";
import { AcceptAthleteInvitationUseCase } from "../../../../src/application";
import {
  DomainError,
  fail,
  ok,
  type Result,
  type AthleteInvitation,
  type CreatedAthleteInvitation,
} from "../../../../src/domain";

const invitationId = "5e6f7a8b-9c0d-4e1f-8a2b-3c4d5e6f7a8b";

const invitation: AthleteInvitation = {
  id: invitationId,
  trainerId: "3c4d5e6f-7a8b-4c9d-8e0f-1a2b3c4d5e6f",
  trainerName: "Ana",
  trainerSurname: "García",
  inviteEmail: "luis@example.com",
  userId: null,
  userName: null,
  userSurname: null,
  status: "PENDING",
  createdAtUtc: "2026-10-28T10:00:00Z",
  expiresAtUtc: "2026-11-04T10:00:00Z",
  acceptedAtUtc: null,
};

class FakeRepository implements AthleteInvitationRepositoryInterface {
  readonly calls: unknown[][] = [];
  otherCalls = 0;

  constructor(private readonly result: Result<AthleteInvitation, DomainError>) {}

  async accept(...args: unknown[]): Promise<Result<AthleteInvitation, DomainError>> {
    this.calls.push(args);
    return this.result;
  }

  async getAll(): Promise<Result<AthleteInvitation[], DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async invite(): Promise<Result<CreatedAthleteInvitation, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async revoke(): Promise<Result<AthleteInvitation, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async reject(): Promise<Result<AthleteInvitation, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }
}

describe("AcceptAthleteInvitationUseCase", () => {
  it("returns what the repository answers with", async () => {
    // Arrange
    const useCase = new AcceptAthleteInvitationUseCase(new FakeRepository(ok(invitation)));

    // Act
    const result = await useCase.execute({ invitationId, token: "raw-token" });

    // Assert
    expect(result).toEqual(ok(invitation));
  });

  it("forwards the input unchanged, once, and calls nothing else", async () => {
    // Arrange
    const repository = new FakeRepository(ok(invitation));
    const useCase = new AcceptAthleteInvitationUseCase(repository);

    // Act
    await useCase.execute({ invitationId, token: "raw-token" });

    // Assert
    expect(repository.calls).toEqual([[invitationId, { token: "raw-token" }]]);
    expect(repository.otherCalls).toBe(0);
  });

  it.each(["FORBIDDEN", "NOT_FOUND", "CONFLICT"] as const)(
    "returns the repository failure %s unchanged",
    async (code) => {
      // Arrange
      const failure = fail(new DomainError(code, "Rejected"));
      const useCase = new AcceptAthleteInvitationUseCase(new FakeRepository(failure));

      // Act
      const result = await useCase.execute({ invitationId, token: "raw-token" });

      // Assert
      expect(result).toBe(failure);
    },
  );
});
