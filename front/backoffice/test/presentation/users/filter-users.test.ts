import { describe, expect, it } from "vitest";
import type { UserSummary } from "@weight-tracker/core/domain";
import { filterUsers } from "@/presentation/users/filter-users";

const ana: UserSummary = {
  id: "5f1c2f0e-9d5b-4a53-8c7e-0d6f7c1a2b3c",
  email: "ana@example.com",
  name: "Ana",
  surname: "García",
  role: "TRAINER",
  isActive: true,
  deactivatedAtUtc: null,
  trainer: null,
};
const luis: UserSummary = {
  id: "6a2d3f1e-0e6c-4b64-9d8f-1e7a8d2b3c4d",
  email: "luis@example.com",
  name: "Luis",
  surname: "Pérez",
  role: "USER",
  isActive: true,
  deactivatedAtUtc: null,
  trainer: null,
};
const marta: UserSummary = {
  id: "7b3e4a2f-1f7d-4c75-8e9a-2f8b9e3c4d5e",
  email: "marta@example.com",
  name: "Marta",
  surname: "Anaya",
  role: "ADMIN",
  isActive: true,
  deactivatedAtUtc: null,
  trainer: null,
};
const users = [ana, luis, marta];
const carla: UserSummary = {
  id: "8c4f5b3a-2a8e-4d86-9fab-3a9caf4d5e6f",
  email: "carla@example.com",
  name: "Carla",
  surname: "Pérez",
  role: "TRAINER",
  isActive: false,
  deactivatedAtUtc: "2026-10-20T09:30:00Z",
  trainer: null,
};

describe("filterUsers", () => {
  it("keeps every user in order when there is no search and no role", () => {
    // Arrange / Act
    const result = filterUsers(users, "", null, false);

    // Assert
    expect(result).toEqual([ana, luis, marta]);
  });

  it("matches the email", () => {
    // Arrange / Act
    const result = filterUsers(users, "luis@", null, false);

    // Assert
    expect(result).toEqual([luis]);
  });

  it("matches the name", () => {
    // Arrange / Act
    const result = filterUsers(users, "Marta", null, false);

    // Assert
    expect(result).toEqual([marta]);
  });

  it("matches the surname ignoring case and accents", () => {
    // Arrange / Act
    const result = filterUsers(users, "PEREZ", null, false);

    // Assert
    expect(result).toEqual([luis]);
  });

  it("matches any of the three fields", () => {
    // Arrange / Act
    const result = filterUsers(users, "ana", null, false);

    // Assert
    expect(result).toEqual([ana, marta]);
  });

  it("keeps only the selected role", () => {
    // Arrange / Act
    const result = filterUsers(users, "", "USER", false);

    // Assert
    expect(result).toEqual([luis]);
  });

  it("combines the role and the search", () => {
    // Arrange / Act
    const result = filterUsers(users, "ana", "ADMIN", false);

    // Assert
    expect(result).toEqual([marta]);
  });

  it("ignores a search made only of spaces", () => {
    // Arrange / Act
    const result = filterUsers(users, "   ", null, false);

    // Assert
    expect(result).toEqual(users);
  });
});

describe("filterUsers with inactive users", () => {
  const withInactive = [ana, carla, luis];

  it("hides the inactive users while they are not asked for", () => {
    // Arrange / Act
    const result = filterUsers(withInactive, "", null, false);

    // Assert
    expect(result).toEqual([ana, luis]);
  });

  it("shows the inactive users in the API's order when asked for", () => {
    // Arrange / Act
    const result = filterUsers(withInactive, "", null, true);

    // Assert
    expect(result).toEqual([ana, carla, luis]);
  });

  it("combines the inactive users with the role and the search", () => {
    // Arrange / Act
    const result = filterUsers(withInactive, "perez", "TRAINER", true);

    // Assert
    expect(result).toEqual([carla]);
  });
});
