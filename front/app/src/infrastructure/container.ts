import {
  AcceptAthleteInvitationUseCase,
  ActivateUserUseCase,
  AddExerciseToWorkoutUseCase,
  BreakSuperSeriesUseCase,
  AddSetRecordUseCase,
  UpdateSetRecordUseCase,
  RemoveSetRecordUseCase,
  ReorderSetRecordsUseCase,
  ReviewWorkoutUseCase,
  ChangeWorkoutStatusUseCase,
  GetWorkoutTemplatesUseCase,
  GetWorkoutTemplateByIdUseCase,
  CreateWorkoutTemplateUseCase,
  UpdateWorkoutTemplateUseCase,
  DeleteWorkoutTemplateUseCase,
  AddExerciseToWorkoutTemplateUseCase,
  UpdateWorkoutTemplateExerciseUseCase,
  RemoveExerciseFromWorkoutTemplateUseCase,
  ReorderWorkoutTemplateExercisesUseCase,
  CreateWorkoutTemplateSuperSeriesUseCase,
  BreakWorkoutTemplateSuperSeriesUseCase,
  CreateWorkoutFromTemplateUseCase,
  CreateWorkoutTemplateFromWorkoutUseCase,
  CreateExerciseUseCase,
  CreateMesocycleUseCase,
  CreateUserUseCase,
  CreateSuperSeriesUseCase,
  CreateWorkoutUseCase,
  DeactivateUserUseCase,
  DefineMesocycleWeekUseCase,
  type Dependencies,
  EndTrainerAthleteUseCase,
  GetAthleteInvitationsUseCase,
  GetAthleteMesocyclesUseCase,
  GetExerciseFavoritesUseCase,
  GetExercisesUseCase,
  GetMesocycleByIdUseCase,
  GetMesocycleWorkoutsUseCase,
  GetMesocyclesUseCase,
  GetMyAthletesUseCase,
  GetMyProfileUseCase,
  GetUserTrainerUseCase,
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
  RedefineMesocycleWeekUseCase,
  RefreshSessionUseCase,
  RejectAthleteInvitationUseCase,
  RemoveExerciseFromWorkoutUseCase,
  ReorderWorkoutExercisesUseCase,
  RevokeAthleteInvitationUseCase,
  SetPreferredLanguageUseCase,
  SetPreferredThemeModeUseCase,
  SignupUseCase,
  UnmarkExerciseFavoriteUseCase,
  UpdateMesocycleUseCase,
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
  MesocycleRepository,
  TrainerAthleteRepository,
  UserRepository,
  WorkoutRepository,
  WorkoutTemplateRepository,
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
  workoutTemplateRepository: (c) => new WorkoutTemplateRepository(c.resolve("httpClient")),
  mesocycleRepository: (c) => new MesocycleRepository(c.resolve("httpClient")),
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
  getUserTrainerUseCase: (c) => new GetUserTrainerUseCase(c.resolve("userRepository")),
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
  createSuperSeriesUseCase: (c) => new CreateSuperSeriesUseCase(c.resolve("workoutRepository")),
  breakSuperSeriesUseCase: (c) => new BreakSuperSeriesUseCase(c.resolve("workoutRepository")),
  addSetRecordUseCase: (c) => new AddSetRecordUseCase(c.resolve("workoutRepository")),
  updateSetRecordUseCase: (c) => new UpdateSetRecordUseCase(c.resolve("workoutRepository")),
  removeSetRecordUseCase: (c) => new RemoveSetRecordUseCase(c.resolve("workoutRepository")),
  reorderSetRecordsUseCase: (c) => new ReorderSetRecordsUseCase(c.resolve("workoutRepository")),
  reviewWorkoutUseCase: (c) => new ReviewWorkoutUseCase(c.resolve("workoutRepository")),
  changeWorkoutStatusUseCase: (c) => new ChangeWorkoutStatusUseCase(c.resolve("workoutRepository")),
  getWorkoutTemplatesUseCase: (c) =>
    new GetWorkoutTemplatesUseCase(c.resolve("workoutTemplateRepository")),
  getWorkoutTemplateByIdUseCase: (c) =>
    new GetWorkoutTemplateByIdUseCase(c.resolve("workoutTemplateRepository")),
  createWorkoutTemplateUseCase: (c) =>
    new CreateWorkoutTemplateUseCase(c.resolve("workoutTemplateRepository")),
  updateWorkoutTemplateUseCase: (c) =>
    new UpdateWorkoutTemplateUseCase(c.resolve("workoutTemplateRepository")),
  deleteWorkoutTemplateUseCase: (c) =>
    new DeleteWorkoutTemplateUseCase(c.resolve("workoutTemplateRepository")),
  addExerciseToWorkoutTemplateUseCase: (c) =>
    new AddExerciseToWorkoutTemplateUseCase(c.resolve("workoutTemplateRepository")),
  updateWorkoutTemplateExerciseUseCase: (c) =>
    new UpdateWorkoutTemplateExerciseUseCase(c.resolve("workoutTemplateRepository")),
  removeExerciseFromWorkoutTemplateUseCase: (c) =>
    new RemoveExerciseFromWorkoutTemplateUseCase(c.resolve("workoutTemplateRepository")),
  reorderWorkoutTemplateExercisesUseCase: (c) =>
    new ReorderWorkoutTemplateExercisesUseCase(c.resolve("workoutTemplateRepository")),
  createWorkoutTemplateSuperSeriesUseCase: (c) =>
    new CreateWorkoutTemplateSuperSeriesUseCase(c.resolve("workoutTemplateRepository")),
  breakWorkoutTemplateSuperSeriesUseCase: (c) =>
    new BreakWorkoutTemplateSuperSeriesUseCase(c.resolve("workoutTemplateRepository")),
  createWorkoutFromTemplateUseCase: (c) =>
    new CreateWorkoutFromTemplateUseCase(c.resolve("workoutTemplateRepository")),
  createWorkoutTemplateFromWorkoutUseCase: (c) =>
    new CreateWorkoutTemplateFromWorkoutUseCase(c.resolve("workoutTemplateRepository")),
  getMesocyclesUseCase: (c) => new GetMesocyclesUseCase(c.resolve("mesocycleRepository")),
  getMesocycleByIdUseCase: (c) => new GetMesocycleByIdUseCase(c.resolve("mesocycleRepository")),
  getMesocycleWorkoutsUseCase: (c) =>
    new GetMesocycleWorkoutsUseCase(c.resolve("mesocycleRepository")),
  getAthleteMesocyclesUseCase: (c) =>
    new GetAthleteMesocyclesUseCase(c.resolve("mesocycleRepository")),
  createMesocycleUseCase: (c) => new CreateMesocycleUseCase(c.resolve("mesocycleRepository")),
  updateMesocycleUseCase: (c) => new UpdateMesocycleUseCase(c.resolve("mesocycleRepository")),
  defineMesocycleWeekUseCase: (c) =>
    new DefineMesocycleWeekUseCase(c.resolve("mesocycleRepository")),
  redefineMesocycleWeekUseCase: (c) =>
    new RedefineMesocycleWeekUseCase(c.resolve("mesocycleRepository")),
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
