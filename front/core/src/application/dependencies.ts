import type {
  AddExerciseToWorkoutInput,
  AthleteInvitation,
  AuthResponse,
  CreatedAthleteInvitation,
  CreatedUserResponse,
  CreateUserRequest,
  CreateExerciseRequest,
  CreateMesocycleRequest,
  CreateWorkoutRequest,
  DefineMesocycleWeekInput,
  Exercise,
  ExerciseFavorite,
  GetAthleteMesocyclesInput,
  GetMesocycleByIdInput,
  GetWorkoutByIdInput,
  GetWorkoutsInput,
  InvitationResponseInput,
  InviteAthleteRequest,
  Mesocycle,
  RedefineMesocycleWeekInput,
  RemoveExerciseFromWorkoutInput,
  ReorderWorkoutExercisesInput,
  LoginRequest,
  Session,
  SignupCredentials,
  TrainerAthlete,
  TrainerAthleteInput,
  TrainerAthleteRelationship,
  UpdateMesocycleInput,
  UpdateProfileRequest,
  UpdateWorkoutExerciseInput,
  UpdateWorkoutInput,
  UserActivationSummary,
  UserProfile,
  UserSummary,
  Workout,
  WorkoutDetail,
  WorkoutExerciseDetail,
  WorkoutListItem,
} from "../domain";
import type {
  AthleteInvitationRepositoryInterface,
  AuthRepositoryInterface,
  ExerciseFavoriteRepositoryInterface,
  ExerciseRepositoryInterface,
  HttpClientInterface,
  KeyValueStoreInterface,
  MesocycleRepositoryInterface,
  NotificationServiceInterface,
  TrainerAthleteRepositoryInterface,
  UseCaseInterface,
  UserRepositoryInterface,
  WorkoutRepositoryInterface,
} from "./ports";

/** Every port and use case a front's container must register, by key. */
export default interface Dependencies {
  httpClient: HttpClientInterface;
  keyValueStore: KeyValueStoreInterface;
  notificationService: NotificationServiceInterface;
  authRepository: AuthRepositoryInterface;
  userRepository: UserRepositoryInterface;
  workoutRepository: WorkoutRepositoryInterface;
  mesocycleRepository: MesocycleRepositoryInterface;
  exerciseRepository: ExerciseRepositoryInterface;
  exerciseFavoriteRepository: ExerciseFavoriteRepositoryInterface;
  trainerAthleteRepository: TrainerAthleteRepositoryInterface;
  athleteInvitationRepository: AthleteInvitationRepositoryInterface;
  signupUseCase: UseCaseInterface<SignupCredentials, AuthResponse>;
  loginUseCase: UseCaseInterface<LoginRequest, AuthResponse>;
  getSessionUseCase: UseCaseInterface<void, Session | null>;
  logoutUseCase: UseCaseInterface<void, void>;
  refreshSessionUseCase: UseCaseInterface<void, Session>;
  getPreferredLanguageUseCase: UseCaseInterface<void, string | null>;
  setPreferredLanguageUseCase: UseCaseInterface<string, void>;
  getPreferredThemeModeUseCase: UseCaseInterface<void, string | null>;
  setPreferredThemeModeUseCase: UseCaseInterface<string, void>;
  getMyProfileUseCase: UseCaseInterface<void, UserProfile>;
  updateMyProfileUseCase: UseCaseInterface<UpdateProfileRequest, UserProfile>;
  getUsersUseCase: UseCaseInterface<void, UserSummary[]>;
  createUserUseCase: UseCaseInterface<CreateUserRequest, CreatedUserResponse>;
  activateUserUseCase: UseCaseInterface<string, UserActivationSummary>;
  deactivateUserUseCase: UseCaseInterface<string, UserActivationSummary>;
  getWorkoutsUseCase: UseCaseInterface<GetWorkoutsInput, WorkoutListItem[]>;
  getWorkoutByIdUseCase: UseCaseInterface<GetWorkoutByIdInput, WorkoutDetail>;
  createWorkoutUseCase: UseCaseInterface<CreateWorkoutRequest, Workout>;
  updateWorkoutUseCase: UseCaseInterface<UpdateWorkoutInput, Workout>;
  addExerciseToWorkoutUseCase: UseCaseInterface<AddExerciseToWorkoutInput, WorkoutExerciseDetail>;
  updateWorkoutExerciseUseCase: UseCaseInterface<UpdateWorkoutExerciseInput, WorkoutExerciseDetail>;
  removeExerciseFromWorkoutUseCase: UseCaseInterface<RemoveExerciseFromWorkoutInput, void>;
  reorderWorkoutExercisesUseCase: UseCaseInterface<ReorderWorkoutExercisesInput, WorkoutDetail>;
  getMesocyclesUseCase: UseCaseInterface<void, Mesocycle[]>;
  getMesocycleByIdUseCase: UseCaseInterface<GetMesocycleByIdInput, Mesocycle>;
  getAthleteMesocyclesUseCase: UseCaseInterface<GetAthleteMesocyclesInput, Mesocycle[]>;
  createMesocycleUseCase: UseCaseInterface<CreateMesocycleRequest, Mesocycle>;
  updateMesocycleUseCase: UseCaseInterface<UpdateMesocycleInput, Mesocycle>;
  defineMesocycleWeekUseCase: UseCaseInterface<DefineMesocycleWeekInput, Mesocycle>;
  redefineMesocycleWeekUseCase: UseCaseInterface<RedefineMesocycleWeekInput, Mesocycle>;
  getExercisesUseCase: UseCaseInterface<void, Exercise[]>;
  createExerciseUseCase: UseCaseInterface<CreateExerciseRequest, Exercise>;
  getExerciseFavoritesUseCase: UseCaseInterface<void, ExerciseFavorite[]>;
  markExerciseFavoriteUseCase: UseCaseInterface<string, ExerciseFavorite>;
  unmarkExerciseFavoriteUseCase: UseCaseInterface<string, void>;
  getMyAthletesUseCase: UseCaseInterface<void, TrainerAthlete[]>;
  getTrainerAthletesUseCase: UseCaseInterface<string, TrainerAthlete[]>;
  linkAthleteToTrainerUseCase: UseCaseInterface<TrainerAthleteInput, TrainerAthleteRelationship>;
  endTrainerAthleteUseCase: UseCaseInterface<TrainerAthleteInput, TrainerAthleteRelationship>;
  getAthleteInvitationsUseCase: UseCaseInterface<void, AthleteInvitation[]>;
  inviteAthleteUseCase: UseCaseInterface<InviteAthleteRequest, CreatedAthleteInvitation>;
  revokeAthleteInvitationUseCase: UseCaseInterface<string, AthleteInvitation>;
  acceptAthleteInvitationUseCase: UseCaseInterface<InvitationResponseInput, AthleteInvitation>;
  rejectAthleteInvitationUseCase: UseCaseInterface<InvitationResponseInput, AthleteInvitation>;
}
