import { describe, expect, it } from "vitest";
import type { AthleteInvitationRepositoryInterface } from "../../../../src/application";
import { InviteAthleteUseCase } from "../../../../src/application";
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

const created: CreatedAthleteInvitation = {
  id: invitation.id,
  trainerId: invitation.trainerId,
  trainerName: invitation.trainerName,
  trainerSurname: invitation.trainerSurname,
  inviteEmail: invitation.inviteEmail,
  userId: null,
  userName: null,
  userSurname: null,
  status: "PENDING",
  createdAtUtc: invitation.createdAtUtc,
  expiresAtUtc: invitation.expiresAtUtc,
  token: "raw-token",
};

class FakeRepository implements AthleteInvitationRepositoryInterface {
  readonly calls: unknown[][] = [];
  otherCalls = 0;

  constructor(private readonly result: Result<CreatedAthleteInvitation, DomainError>) {}

  async invite(...args: unknown[]): Promise<Result<CreatedAthleteInvitation, DomainError>> {
    this.calls.push(args);
    return this.result;
  }

  async getAll(): Promise<Result<AthleteInvitation[], DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async revoke(): Promise<Result<AthleteInvitation, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async accept(): Promise<Result<AthleteInvitation, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async reject(): Promise<Result<AthleteInvitation, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }
}

describe("InviteAthleteUseCase", () => {
  it("returns what the repository answers with", async () => {
    // Arrange
    const useCase = new InviteAthleteUseCase(new FakeRepository(ok(created)));

    // Act
    const result = await useCase.execute({ email: "luis@example.com" });

    // Assert
    expect(result).toEqual(ok(created));
  });

  it("forwards the input unchanged, once, and calls nothing else", async () => {
    // Arrange
    const repository = new FakeRepository(ok(created));
    const useCase = new InviteAthleteUseCase(repository);

    // Act
    await useCase.execute({ email: "luis@example.com" });

    // Assert
    expect(repository.calls).toEqual([[{ email: "luis@example.com" }]]);
    expect(repository.otherCalls).toBe(0);
  });

  it.each(["VALIDATION", "FORBIDDEN", "CONFLICT"] as const)(
    "returns the repository failure %s unchanged",
    async (code) => {
      // Arrange
      const failure = fail(new DomainError(code, "Rejected"));
      const useCase = new InviteAthleteUseCase(new FakeRepository(failure));

      // Act
      const result = await useCase.execute({ email: "luis@example.com" });

      // Assert
      expect(result).toBe(failure);
    },
  );
});
