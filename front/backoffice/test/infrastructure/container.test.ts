import { afterEach, describe, expect, it, vi } from "vitest";
import type { Dependencies } from "@weight-tracker/core/application";
import container from "@/infrastructure/container";

const keys: (keyof Dependencies)[] = [
  "httpClient",
  "keyValueStore",
  "notificationService",
  "getSessionUseCase",
  "refreshSessionUseCase",
  "getMyProfileUseCase",
  "updateMyProfileUseCase",
  "getUsersUseCase",
  "createUserUseCase",
  "activateUserUseCase",
  "deactivateUserUseCase",
  "getWorkoutsUseCase",
  "getWorkoutByIdUseCase",
  "createWorkoutUseCase",
  "updateWorkoutUseCase",
  "addExerciseToWorkoutUseCase",
  "updateWorkoutExerciseUseCase",
  "removeExerciseFromWorkoutUseCase",
  "reorderWorkoutExercisesUseCase",
  "createSuperSeriesUseCase",
  "breakSuperSeriesUseCase",
  "mesocycleRepository",
  "getMesocyclesUseCase",
  "getMesocycleByIdUseCase",
  "getMesocycleWorkoutsUseCase",
  "getAthleteMesocyclesUseCase",
  "createMesocycleUseCase",
  "updateMesocycleUseCase",
  "defineMesocycleWeekUseCase",
  "redefineMesocycleWeekUseCase",
  "getExercisesUseCase",
  "createExerciseUseCase",
  "exerciseFavoriteRepository",
  "getExerciseFavoritesUseCase",
  "markExerciseFavoriteUseCase",
  "unmarkExerciseFavoriteUseCase",
  "trainerAthleteRepository",
  "athleteInvitationRepository",
  "getMyAthletesUseCase",
  "getTrainerAthletesUseCase",
  "linkAthleteToTrainerUseCase",
  "endTrainerAthleteUseCase",
  "getAthleteInvitationsUseCase",
  "inviteAthleteUseCase",
  "revokeAthleteInvitationUseCase",
  "acceptAthleteInvitationUseCase",
  "rejectAthleteInvitationUseCase",
];

describe("container", () => {
  it.each(keys)("resolves %s", (key) => {
    // Arrange & Act
    const dependency = container.resolve(key);

    // Assert
    expect(dependency).toBeDefined();
  });

  it("sends the stored access token with API requests", async () => {
    // Arrange
    localStorage.setItem("auth.accessToken", "stored-access");
    localStorage.setItem("auth.refreshToken", "stored-refresh");
    const fetch = vi.fn(async () => new Response("{}", { status: 200 }));
    vi.stubGlobal("fetch", fetch);

    // Act
    await container.resolve("httpClient").request({ method: "GET", path: "/users/me" });

    // Assert
    const [, init] = fetch.mock.calls[0] as unknown as [string, RequestInit];
    expect(init.headers).toMatchObject({ Authorization: "Bearer stored-access" });
  });

  afterEach(() => {
    localStorage.clear();
    vi.unstubAllGlobals();
  });
});
