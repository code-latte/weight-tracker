import type {
  AddExerciseToWorkoutInput,
  BreakSuperSeriesInput,
  AthleteInvitation,
  AuthResponse,
  CreatedAthleteInvitation,
  CreatedUserResponse,
  CustomerLead,
  CreateUserRequest,
  CreateExerciseRequest,
  CreateMesocycleRequest,
  CreateSuperSeriesInput,
  CreateWorkoutRequest,
  DefineMesocycleWeekInput,
  DeleteWorkoutInput,
  Exercise,
  ExerciseFavorite,
  GetAthleteMesocyclesInput,
  GetMesocycleByIdInput,
  GetMesocycleWorkoutsInput,
  GetWorkoutByIdInput,
  GetWorkoutsInput,
  InvitationResponseInput,
  InviteAthleteRequest,
  Mesocycle,
  RedefineMesocycleWeekInput,
  RemoveExerciseFromWorkoutInput,
  ReorderWorkoutExercisesInput,
  AddSetRecordInput,
  UpdateSetRecordInput,
  RemoveSetRecordInput,
  ReorderSetRecordsInput,
  ReviewWorkoutInput,
  ChangeWorkoutStatusInput,
  LoginRequest,
  Session,
  SignupCredentials,
  TrainerAthlete,
  TrainerAthleteInput,
  TrainerAthleteRelationship,
  UpdateMesocycleInput,
  UpdateProfileRequest,
  SuperSeries,
  UpdateWorkoutExerciseInput,
  UpdateWorkoutInput,
  UserActivationSummary,
  UserProfile,
  UserSummary,
  Workout,
  WorkoutById,
  WorkoutDetail,
  WorkoutExerciseDetail,
  WorkoutListItem,
  AddExerciseToWorkoutTemplateInput,
  BreakWorkoutTemplateSuperSeriesInput,
  CreateWorkoutFromTemplateInput,
  CreateWorkoutTemplateFromWorkoutInput,
  CreateWorkoutTemplateRequest,
  CreateWorkoutTemplateSuperSeriesInput,
  DeleteWorkoutTemplateInput,
  GetWorkoutTemplateByIdInput,
  GetWorkoutTemplatesInput,
  RemoveExerciseFromWorkoutTemplateInput,
  ReorderWorkoutTemplateExercisesInput,
  UpdateWorkoutTemplateExerciseInput,
  UpdateWorkoutTemplateInput,
  WorkoutTemplate,
} from "../domain";
import type {
  AthleteInvitationRepositoryInterface,
  AuthRepositoryInterface,
  CustomerLeadRepositoryInterface,
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
  WorkoutTemplateRepositoryInterface,
} from "./ports";

/** Every port and use case a front's container must register, by key. */
export default interface Dependencies {
  httpClient: HttpClientInterface;
  keyValueStore: KeyValueStoreInterface;
  notificationService: NotificationServiceInterface;
  authRepository: AuthRepositoryInterface;
  userRepository: UserRepositoryInterface;
  workoutRepository: WorkoutRepositoryInterface;
  workoutTemplateRepository: WorkoutTemplateRepositoryInterface;
  mesocycleRepository: MesocycleRepositoryInterface;
  exerciseRepository: ExerciseRepositoryInterface;
  exerciseFavoriteRepository: ExerciseFavoriteRepositoryInterface;
  trainerAthleteRepository: TrainerAthleteRepositoryInterface;
  athleteInvitationRepository: AthleteInvitationRepositoryInterface;
  customerLeadRepository: CustomerLeadRepositoryInterface;
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
  getUserTrainerUseCase: UseCaseInterface<string, UserProfile | null>;
  updateMyProfileUseCase: UseCaseInterface<UpdateProfileRequest, UserProfile>;
  getUsersUseCase: UseCaseInterface<void, UserSummary[]>;
  createUserUseCase: UseCaseInterface<CreateUserRequest, CreatedUserResponse>;
  activateUserUseCase: UseCaseInterface<string, UserActivationSummary>;
  deactivateUserUseCase: UseCaseInterface<string, UserActivationSummary>;
  getWorkoutsUseCase: UseCaseInterface<GetWorkoutsInput, WorkoutListItem[]>;
  getWorkoutByIdUseCase: UseCaseInterface<GetWorkoutByIdInput, WorkoutById>;
  createWorkoutUseCase: UseCaseInterface<CreateWorkoutRequest, Workout>;
  updateWorkoutUseCase: UseCaseInterface<UpdateWorkoutInput, Workout>;
  deleteWorkoutUseCase: UseCaseInterface<DeleteWorkoutInput, void>;
  addExerciseToWorkoutUseCase: UseCaseInterface<AddExerciseToWorkoutInput, WorkoutExerciseDetail>;
  updateWorkoutExerciseUseCase: UseCaseInterface<UpdateWorkoutExerciseInput, WorkoutExerciseDetail>;
  removeExerciseFromWorkoutUseCase: UseCaseInterface<RemoveExerciseFromWorkoutInput, void>;
  reorderWorkoutExercisesUseCase: UseCaseInterface<ReorderWorkoutExercisesInput, WorkoutDetail>;
  createSuperSeriesUseCase: UseCaseInterface<CreateSuperSeriesInput, SuperSeries>;
  breakSuperSeriesUseCase: UseCaseInterface<BreakSuperSeriesInput, void>;
  addSetRecordUseCase: UseCaseInterface<AddSetRecordInput, WorkoutDetail>;
  updateSetRecordUseCase: UseCaseInterface<UpdateSetRecordInput, WorkoutDetail>;
  removeSetRecordUseCase: UseCaseInterface<RemoveSetRecordInput, WorkoutDetail>;
  reorderSetRecordsUseCase: UseCaseInterface<ReorderSetRecordsInput, WorkoutDetail>;
  reviewWorkoutUseCase: UseCaseInterface<ReviewWorkoutInput, WorkoutDetail>;
  changeWorkoutStatusUseCase: UseCaseInterface<ChangeWorkoutStatusInput, WorkoutDetail>;
  getWorkoutTemplatesUseCase: UseCaseInterface<GetWorkoutTemplatesInput, WorkoutTemplate[]>;
  getWorkoutTemplateByIdUseCase: UseCaseInterface<GetWorkoutTemplateByIdInput, WorkoutTemplate>;
  createWorkoutTemplateUseCase: UseCaseInterface<CreateWorkoutTemplateRequest, WorkoutTemplate>;
  updateWorkoutTemplateUseCase: UseCaseInterface<UpdateWorkoutTemplateInput, WorkoutTemplate>;
  deleteWorkoutTemplateUseCase: UseCaseInterface<DeleteWorkoutTemplateInput, void>;
  addExerciseToWorkoutTemplateUseCase: UseCaseInterface<
    AddExerciseToWorkoutTemplateInput,
    WorkoutExerciseDetail
  >;
  updateWorkoutTemplateExerciseUseCase: UseCaseInterface<
    UpdateWorkoutTemplateExerciseInput,
    WorkoutExerciseDetail
  >;
  removeExerciseFromWorkoutTemplateUseCase: UseCaseInterface<
    RemoveExerciseFromWorkoutTemplateInput,
    void
  >;
  reorderWorkoutTemplateExercisesUseCase: UseCaseInterface<
    ReorderWorkoutTemplateExercisesInput,
    WorkoutTemplate
  >;
  createWorkoutTemplateSuperSeriesUseCase: UseCaseInterface<
    CreateWorkoutTemplateSuperSeriesInput,
    SuperSeries
  >;
  breakWorkoutTemplateSuperSeriesUseCase: UseCaseInterface<
    BreakWorkoutTemplateSuperSeriesInput,
    void
  >;
  createWorkoutFromTemplateUseCase: UseCaseInterface<CreateWorkoutFromTemplateInput, Workout>;
  createWorkoutTemplateFromWorkoutUseCase: UseCaseInterface<
    CreateWorkoutTemplateFromWorkoutInput,
    WorkoutTemplate
  >;
  getMesocyclesUseCase: UseCaseInterface<void, Mesocycle[]>;
  getMesocycleByIdUseCase: UseCaseInterface<GetMesocycleByIdInput, Mesocycle>;
  getMesocycleWorkoutsUseCase: UseCaseInterface<GetMesocycleWorkoutsInput, WorkoutListItem[]>;
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
  getCustomerLeadsUseCase: UseCaseInterface<void, CustomerLead[]>;
}
