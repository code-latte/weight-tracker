import { describe, expect, it } from "vitest";
import type { TrainerAthlete } from "@weight-tracker/core/domain";
import { filterAthletes } from "@/presentation/athletes/filter-athletes";

const athlete = (overrides: Partial<TrainerAthlete>): TrainerAthlete => ({
  id: "1b2c3d4e-5f6a-4b7c-8d9e-0f1a2b3c4d5e",
  athleteId: "9a8b7c6d-5e4f-4a3b-8c2d-1e0f9a8b7c6d",
  email: "athlete@example.com",
  name: "",
  surname: "",
  trainerName: "Ana",
  trainerSurname: "García",
  startedAtUtc: "2026-10-03T08:00:00Z",
  ...overrides,
});

const luis = athlete({ id: "a1", email: "luis@example.com", name: "Luis", surname: "Pérez" });
const marta = athlete({ id: "a2", email: "marta@example.com", name: "Marta", surname: "Anaya" });
const nameless = athlete({ id: "a3", email: "nameless@example.com" });
const athletes = [luis, marta, nameless];

describe("filterAthletes", () => {
  it("keeps every athlete in order when there is no search", () => {
    // Arrange / Act
    const result = filterAthletes(athletes, "");

    // Assert
    expect(result).toEqual([luis, marta, nameless]);
  });

  it("matches the full name ignoring case and accents", () => {
    // Arrange / Act
    const result = filterAthletes(athletes, "LUIS PEREZ");

    // Assert
    expect(result).toEqual([luis]);
  });

  it("matches the email", () => {
    // Arrange / Act
    const result = filterAthletes(athletes, "marta@");

    // Assert
    expect(result).toEqual([marta]);
  });

  it("ignores surrounding spaces and a search made only of spaces", () => {
    // Arrange / Act
    const trimmed = filterAthletes(athletes, "  anaya  ");
    const blank = filterAthletes(athletes, "   ");

    // Assert
    expect(trimmed).toEqual([marta]);
    expect(blank).toEqual(athletes);
  });

  it("matches nobody when nothing contains the text", () => {
    // Arrange / Act
    const result = filterAthletes(athletes, "zzz");

    // Assert
    expect(result).toEqual([]);
  });
});
