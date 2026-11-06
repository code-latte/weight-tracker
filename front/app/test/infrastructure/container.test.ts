import type { Dependencies } from "@weight-tracker/core/application";
import container from "@/infrastructure/container";

const mockStoredTokens = new Map([
  ["auth.accessToken", "stored-access"],
  ["auth.refreshToken", "stored-refresh"],
]);

jest.mock("expo-sqlite", () => ({
  openDatabaseAsync: async () => ({
    execAsync: async () => {},
    getFirstAsync: async (_source: string, key: string) =>
      mockStoredTokens.has(key) ? { value: mockStoredTokens.get(key) } : null,
    runAsync: async () => {},
  }),
}));

const keys: (keyof Dependencies)[] = [
  "httpClient",
  "keyValueStore",
  "notificationService",
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
  "workoutTemplateRepository",
  "getWorkoutTemplatesUseCase",
  "getWorkoutTemplateByIdUseCase",
  "createWorkoutTemplateUseCase",
  "updateWorkoutTemplateUseCase",
  "deleteWorkoutTemplateUseCase",
  "addExerciseToWorkoutTemplateUseCase",
  "updateWorkoutTemplateExerciseUseCase",
  "removeExerciseFromWorkoutTemplateUseCase",
  "reorderWorkoutTemplateExercisesUseCase",
  "createWorkoutTemplateSuperSeriesUseCase",
  "breakWorkoutTemplateSuperSeriesUseCase",
  "createWorkoutFromTemplateUseCase",
  "createWorkoutTemplateFromWorkoutUseCase",
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
    const fetch = jest.fn(async () => new Response("{}", { status: 200 }));
    globalThis.fetch = fetch as unknown as typeof globalThis.fetch;

    // Act
    await container.resolve("httpClient").request({ method: "GET", path: "/users/me" });

    // Assert
    const [, init] = fetch.mock.calls[0] as unknown as [string, RequestInit];
    expect(init.headers).toMatchObject({ Authorization: "Bearer stored-access" });
  });
});
