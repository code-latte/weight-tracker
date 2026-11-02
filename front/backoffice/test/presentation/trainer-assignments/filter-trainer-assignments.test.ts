import { describe, expect, it } from "vitest";
import type { UserSummary } from "@weight-tracker/core/domain";
import { filterTrainerAssignments } from "@/presentation/trainer-assignments/filter-trainer-assignments";

const trainer = {
  id: "5f1c2f0e-9d5b-4a53-8c7e-0d6f7c1a2b3c",
  email: "ana@example.com",
  name: "Ana",
  surname: "García",
  role: "TRAINER",
};

const athlete = (overrides: Partial<UserSummary>): UserSummary => ({
  id: "1b2c3d4e-5f6a-4b7c-8d9e-0f1a2b3c4d5e",
  email: "athlete@example.com",
  name: "",
  surname: "",
  role: "USER",
  isActive: true,
  deactivatedAtUtc: null,
  trainer: null,
  ...overrides,
});

const luis = athlete({
  id: "a1",
  email: "luis@example.com",
  name: "Luis",
  surname: "Pérez",
  trainer,
});
const marta = athlete({ id: "a2", email: "marta@example.com", name: "Marta", surname: "Anaya" });
const nameless = athlete({ id: "a3", email: "nameless@example.com", trainer });
const athletes = [luis, marta, nameless];

describe("filterTrainerAssignments", () => {
  it("keeps every athlete in order when there is no search nor pill", () => {
    // Arrange / Act
    const result = filterTrainerAssignments(athletes, "", null);

    // Assert
    expect(result).toEqual([luis, marta, nameless]);
  });

  it("matches the athlete's full name ignoring case and accents", () => {
    // Arrange / Act
    const result = filterTrainerAssignments(athletes, "LUIS PEREZ", null);

    // Assert
    expect(result).toEqual([luis]);
  });

  it("matches the athlete's email", () => {
    // Arrange / Act
    const result = filterTrainerAssignments(athletes, "marta@", null);

    // Assert
    expect(result).toEqual([marta]);
  });

  it("matches the trainer's full name ignoring accents", () => {
    // Arrange / Act
    const result = filterTrainerAssignments(athletes, "ana garcia", null);

    // Assert
    expect(result).toEqual([luis, nameless]);
  });

  it("does not match the trainer's email", () => {
    // Arrange / Act
    const result = filterTrainerAssignments(athletes, "ana@", null);

    // Assert
    expect(result).toEqual([]);
  });

  it.each([
    ["with", [luis, nameless]],
    ["without", [marta]],
  ] as const)("keeps only the athletes %s a trainer", (assignment, expected) => {
    // Arrange / Act
    const result = filterTrainerAssignments(athletes, "", assignment);

    // Assert
    expect(result).toEqual(expected);
  });

  it("combines the pill and the search", () => {
    // Arrange / Act
    const result = filterTrainerAssignments(athletes, "example", "without");

    // Assert
    expect(result).toEqual([marta]);
  });

  it("ignores surrounding spaces and a search made only of spaces", () => {
    // Arrange / Act
    const trimmed = filterTrainerAssignments(athletes, "  anaya  ", null);
    const blank = filterTrainerAssignments(athletes, "   ", null);

    // Assert
    expect(trimmed).toEqual([marta]);
    expect(blank).toEqual(athletes);
  });
});
