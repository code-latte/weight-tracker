import { describe, expect, it } from "vitest";
import type { AthleteInvitation } from "@weight-tracker/core/domain";
import { filterInvitations } from "@/presentation/invitations/filter-invitations";

const invitation = (overrides: Partial<AthleteInvitation>): AthleteInvitation => ({
  id: "5e6f7a8b-9c0d-4e1f-8a2b-3c4d5e6f7a8b",
  trainerId: "3c4d5e6f-7a8b-4c9d-8e0f-1a2b3c4d5e6f",
  trainerName: "Ana",
  trainerSurname: "García",
  inviteEmail: "athlete@example.com",
  userId: null,
  userName: null,
  userSurname: null,
  status: "PENDING",
  createdAtUtc: "2026-10-28T10:00:00Z",
  expiresAtUtc: "2026-11-04T10:00:00Z",
  acceptedAtUtc: null,
  ...overrides,
});

const toLuis = invitation({ id: "i1", inviteEmail: "luis@example.com" });
const toMarta = invitation({ id: "i2", inviteEmail: "marta@example.com", status: "REVOKED" });
const fromJose = invitation({
  id: "i3",
  inviteEmail: "carla@example.com",
  trainerName: "José",
  trainerSurname: "Ruiz",
  status: "ACCEPTED",
});
const invitations = [toLuis, toMarta, fromJose];

describe("filterInvitations", () => {
  it("keeps every invitation in order without search or status", () => {
    // Arrange / Act
    const result = filterInvitations(invitations, "", null, { byTrainer: false });

    // Assert
    expect(result).toEqual(invitations);
  });

  it("matches the invited email ignoring case and surrounding spaces", () => {
    // Arrange / Act
    const result = filterInvitations(invitations, "  LUIS@ ", null, { byTrainer: false });

    // Assert
    expect(result).toEqual([toLuis]);
  });

  it("does not match the trainer unless asked to", () => {
    // Arrange / Act
    const result = filterInvitations(invitations, "jose ruiz", null, { byTrainer: false });

    // Assert
    expect(result).toEqual([]);
  });

  it("matches the trainer's full name ignoring accents when asked to", () => {
    // Arrange / Act
    const result = filterInvitations(invitations, "jose ruiz", null, { byTrainer: true });

    // Assert
    expect(result).toEqual([fromJose]);
  });

  it("still matches the invited email when searching by trainer too", () => {
    // Arrange / Act
    const result = filterInvitations(invitations, "marta@", null, { byTrainer: true });

    // Assert
    expect(result).toEqual([toMarta]);
  });

  it("keeps only the selected status", () => {
    // Arrange / Act
    const result = filterInvitations(invitations, "", "REVOKED", { byTrainer: false });

    // Assert
    expect(result).toEqual([toMarta]);
  });

  it("combines the status and the search", () => {
    // Arrange / Act
    const result = filterInvitations(invitations, "example.com", "PENDING", { byTrainer: true });

    // Assert
    expect(result).toEqual([toLuis]);
  });
});
