import {
  AcceptAthleteInvitationUseCase,
  ActivateUserUseCase,
  AddExerciseToWorkoutUseCase,
  CreateUserUseCase,
  CreateWorkoutUseCase,
  DeactivateUserUseCase,
  type Dependencies,
  EndTrainerAthleteUseCase,
  GetAthleteInvitationsUseCase,
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
  RefreshSessionUseCase,
  RejectAthleteInvitationUseCase,
  RemoveExerciseFromWorkoutUseCase,
  ReorderWorkoutExercisesUseCase,
  RevokeAthleteInvitationUseCase,
  SetPreferredLanguageUseCase,
  SetPreferredThemeModeUseCase,
  SignupUseCase,
  UpdateMyProfileUseCase,
  UpdateWorkoutExerciseUseCase,
  UpdateWorkoutUseCase,
} from "@weight-tracker/core/application";
import {
  AthleteInvitationRepository,
  AuthRepository,
  Container,
  ExerciseRepository,
  FetchHttpClient,
  TrainerAthleteRepository,
  UserRepository,
  WorkoutRepository,
} from "@weight-tracker/core/infrastructure";
import LocalStorageKeyValueStore from "./local-storage-key-value-store";
import NoOpNotificationService from "./no-op-notification-service";

const container = new Container<Dependencies>({
  httpClient: (c) =>
    new FetchHttpClient({
      baseUrl: process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5199",
      // Every request carries the stored access token, when there is one.
      getAccessToken: async () => {
        const session = await c.resolve("getSessionUseCase").execute();
        return session.ok ? (session.value?.accessToken ?? null) : null;
      },
    }),
  keyValueStore: () => new LocalStorageKeyValueStore(),
  notificationService: () => new NoOpNotificationService(),
  authRepository: (c) => new AuthRepository(c.resolve("httpClient")),
  userRepository: (c) => new UserRepository(c.resolve("httpClient")),
  workoutRepository: (c) => new WorkoutRepository(c.resolve("httpClient")),
  exerciseRepository: (c) => new ExerciseRepository(c.resolve("httpClient")),
  trainerAthleteRepository: (c) => new TrainerAthleteRepository(c.resolve("httpClient")),
  athleteInvitationRepository: (c) => new AthleteInvitationRepository(c.resolve("httpClient")),
  signupUseCase: (c) =>
    new SignupUseCase(c.resolve("authRepository"), c.resolve("keyValueStore"), 2), // TRAINER
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
