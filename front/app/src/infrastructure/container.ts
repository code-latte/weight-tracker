import {
  AcceptAthleteInvitationUseCase,
  ActivateUserUseCase,
  AddExerciseToWorkoutUseCase,
  CreateExerciseUseCase,
  CreateUserUseCase,
  CreateWorkoutUseCase,
  DeactivateUserUseCase,
  type Dependencies,
  EndTrainerAthleteUseCase,
  GetAthleteInvitationsUseCase,
  GetExerciseFavoritesUseCase,
  GetExercisesUseCase,
  GetMyAthletesUseCase,
  GetMyProfileUseCase,
  GetPreferredLanguageUseCase,
  GetPreferredThemeModeUseCase,
  GetSessionUseCase,
  GetTrainerAthletesUseCase,
  GetUsersUseCase,
  GetWorkoutByIdUseCase,
  GetWorkoutsUseCase,
  InviteAthleteUseCase,
  LinkAthleteToTrainerUseCase,
  LoginUseCase,
  LogoutUseCase,
  MarkExerciseFavoriteUseCase,
  RefreshSessionUseCase,
  RejectAthleteInvitationUseCase,
  RemoveExerciseFromWorkoutUseCase,
  ReorderWorkoutExercisesUseCase,
  RevokeAthleteInvitationUseCase,
  SetPreferredLanguageUseCase,
  SetPreferredThemeModeUseCase,
  SignupUseCase,
  UnmarkExerciseFavoriteUseCase,
  UpdateMyProfileUseCase,
  UpdateWorkoutExerciseUseCase,
  UpdateWorkoutUseCase,
} from "@weight-tracker/core/application";
import {
  AthleteInvitationRepository,
  AuthRepository,
  Container,
  ExerciseFavoriteRepository,
  ExerciseRepository,
  FetchHttpClient,
  TrainerAthleteRepository,
  UserRepository,
  WorkoutRepository,
} from "@weight-tracker/core/infrastructure";
import NoOpNotificationService from "./no-op-notification-service";
import SqliteKeyValueStore from "./sqlite-key-value-store";

const container = new Container<Dependencies>({
  httpClient: (c) =>
    new FetchHttpClient({
      baseUrl: process.env.EXPO_PUBLIC_API_URL ?? "http://localhost:5199",
      // Every request carries the stored access token, when there is one.
      getAccessToken: async () => {
        const session = await c.resolve("getSessionUseCase").execute();
        return session.ok ? (session.value?.accessToken ?? null) : null;
      },
    }),
  keyValueStore: () => new SqliteKeyValueStore(),
  notificationService: () => new NoOpNotificationService(),
  authRepository: (c) => new AuthRepository(c.resolve("httpClient")),
  userRepository: (c) => new UserRepository(c.resolve("httpClient")),
  workoutRepository: (c) => new WorkoutRepository(c.resolve("httpClient")),
  exerciseRepository: (c) => new ExerciseRepository(c.resolve("httpClient")),
  exerciseFavoriteRepository: (c) => new ExerciseFavoriteRepository(c.resolve("httpClient")),
  trainerAthleteRepository: (c) => new TrainerAthleteRepository(c.resolve("httpClient")),
  athleteInvitationRepository: (c) => new AthleteInvitationRepository(c.resolve("httpClient")),
  signupUseCase: (c) =>
    new SignupUseCase(c.resolve("authRepository"), c.resolve("keyValueStore"), 1), // USER
  loginUseCase: (c) => new LoginUseCase(c.resolve("authRepository"), c.resolve("keyValueStore")),
  getSessionUseCase: (c) => new GetSessionUseCase(c.resolve("keyValueStore")),
  logoutUseCase: (c) => new LogoutUseCase(c.resolve("keyValueStore")),
  refreshSessionUseCase: (c) =>
    new RefreshSessionUseCase(c.resolve("authRepository"), c.resolve("keyValueStore")),
  getPreferredLanguageUseCase: (c) => new GetPreferredLanguageUseCase(c.resolve("keyValueStore")),
  setPreferredLanguageUseCase: (c) => new SetPreferredLanguageUseCase(c.resolve("keyValueStore")),
  getPreferredThemeModeUseCase: (c) => new GetPreferredThemeModeUseCase(c.resolve("keyValueStore")),
  setPreferredThemeModeUseCase: (c) => new SetPreferredThemeModeUseCase(c.resolve("keyValueStore")),
  getMyProfileUseCase: (c) => new GetMyProfileUseCase(c.resolve("userRepository")),
  updateMyProfileUseCase: (c) => new UpdateMyProfileUseCase(c.resolve("userRepository")),
  getUsersUseCase: (c) => new GetUsersUseCase(c.resolve("userRepository")),
  createUserUseCase: (c) => new CreateUserUseCase(c.resolve("userRepository")),
  activateUserUseCase: (c) => new ActivateUserUseCase(c.resolve("userRepository")),
  deactivateUserUseCase: (c) => new DeactivateUserUseCase(c.resolve("userRepository")),
  getWorkoutsUseCase: (c) => new GetWorkoutsUseCase(c.resolve("workoutRepository")),
  getWorkoutByIdUseCase: (c) => new GetWorkoutByIdUseCase(c.resolve("workoutRepository")),
  createWorkoutUseCase: (c) => new CreateWorkoutUseCase(c.resolve("workoutRepository")),
  updateWorkoutUseCase: (c) => new UpdateWorkoutUseCase(c.resolve("workoutRepository")),
  addExerciseToWorkoutUseCase: (c) =>
    new AddExerciseToWorkoutUseCase(c.resolve("workoutRepository")),
  updateWorkoutExerciseUseCase: (c) =>
    new UpdateWorkoutExerciseUseCase(c.resolve("workoutRepository")),
  removeExerciseFromWorkoutUseCase: (c) =>
    new RemoveExerciseFromWorkoutUseCase(c.resolve("workoutRepository")),
  reorderWorkoutExercisesUseCase: (c) =>
    new ReorderWorkoutExercisesUseCase(c.resolve("workoutRepository")),
  getExercisesUseCase: (c) => new GetExercisesUseCase(c.resolve("exerciseRepository")),
  createExerciseUseCase: (c) => new CreateExerciseUseCase(c.resolve("exerciseRepository")),
  getExerciseFavoritesUseCase: (c) =>
    new GetExerciseFavoritesUseCase(c.resolve("exerciseFavoriteRepository")),
  markExerciseFavoriteUseCase: (c) =>
    new MarkExerciseFavoriteUseCase(c.resolve("exerciseFavoriteRepository")),
  unmarkExerciseFavoriteUseCase: (c) =>
    new UnmarkExerciseFavoriteUseCase(c.resolve("exerciseFavoriteRepository")),
  getMyAthletesUseCase: (c) => new GetMyAthletesUseCase(c.resolve("trainerAthleteRepository")),
  getTrainerAthletesUseCase: (c) =>
    new GetTrainerAthletesUseCase(c.resolve("trainerAthleteRepository")),
  linkAthleteToTrainerUseCase: (c) =>
    new LinkAthleteToTrainerUseCase(c.resolve("trainerAthleteRepository")),
  endTrainerAthleteUseCase: (c) =>
    new EndTrainerAthleteUseCase(c.resolve("trainerAthleteRepository")),
  getAthleteInvitationsUseCase: (c) =>
    new GetAthleteInvitationsUseCase(c.resolve("athleteInvitationRepository")),
  inviteAthleteUseCase: (c) => new InviteAthleteUseCase(c.resolve("athleteInvitationRepository")),
  revokeAthleteInvitationUseCase: (c) =>
    new RevokeAthleteInvitationUseCase(c.resolve("athleteInvitationRepository")),
  acceptAthleteInvitationUseCase: (c) =>
    new AcceptAthleteInvitationUseCase(c.resolve("athleteInvitationRepository")),
  rejectAthleteInvitationUseCase: (c) =>
    new RejectAthleteInvitationUseCase(c.resolve("athleteInvitationRepository")),
});

export default container;
