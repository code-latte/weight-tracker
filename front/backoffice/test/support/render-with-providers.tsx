import type { ReactElement } from "react";
import { ThemeProvider } from "@mui/material/styles";
import { render } from "@testing-library/react";
import { I18nextProvider } from "react-i18next";
import type { Dependencies } from "@weight-tracker/core/application";
import { ok, type UserProfile } from "@weight-tracker/core/domain";
import { Container, type Registrations } from "@weight-tracker/core/infrastructure";
import i18n from "@/infrastructure/i18n";
import SessionProvider from "@/presentation/auth/components/session-provider";
import CurrentUserProvider from "@/presentation/common/components/current-user-provider";
import ExerciseCatalogProvider from "@/presentation/common/components/exercise-catalog-provider";
import ContainerProvider from "@/presentation/common/context/container-provider";
import theme from "@/presentation/common/theme";

/** The profile a signed-in view reads unless a test chooses another one. */
export const completeProfile: UserProfile = {
  id: "5f1c2f0e-9d5b-4a53-8c7e-0d6f7c1a2b3c",
  email: "trainer@example.com",
  name: "Ana",
  surname: "García",
  role: "TRAINER",
};

/** Registrations that do nothing; tests override the keys the view under test uses. */
const unusedDependencies: Registrations<Dependencies> = {
  httpClient: () => ({ request: async () => undefined as never }),
  keyValueStore: () => ({ get: async () => null, set: async () => {}, remove: async () => {} }),
  notificationService: () => ({ requestPermission: async () => false, notify: async () => {} }),
  authRepository: () => ({
    signup: async () => undefined as never,
    login: async () => undefined as never,
    refresh: async () => undefined as never,
  }),
  userRepository: () => ({
    getMe: async () => undefined as never,
    getTrainer: async () => undefined as never,
    getMyExerciseLastPerformance: async () => undefined as never,
    updateMe: async () => undefined as never,
    getAll: async () => undefined as never,
    create: async () => undefined as never,
    activate: async () => undefined as never,
    deactivate: async () => undefined as never,
  }),
  workoutRepository: () => ({
    getAll: async () => undefined as never,
    getById: async () => undefined as never,
    create: async () => undefined as never,
    update: async () => undefined as never,
    delete: async () => undefined as never,
    addExercise: async () => undefined as never,
    updateExercise: async () => undefined as never,
    removeExercise: async () => undefined as never,
    reorderExercises: async () => undefined as never,
    createSuperSeries: async () => undefined as never,
    breakSuperSeries: async () => undefined as never,
    addSetRecord: async () => undefined as never,
    updateSetRecord: async () => undefined as never,
    removeSetRecord: async () => undefined as never,
    reorderSetRecords: async () => undefined as never,
    review: async () => undefined as never,
    changeStatus: async () => undefined as never,
  }),
  workoutTemplateRepository: () => ({
    getAll: async () => undefined as never,
    getById: async () => undefined as never,
    create: async () => undefined as never,
    update: async () => undefined as never,
    delete: async () => undefined as never,
    addExercise: async () => undefined as never,
    updateExercise: async () => undefined as never,
    removeExercise: async () => undefined as never,
    reorderExercises: async () => undefined as never,
    createSuperSeries: async () => undefined as never,
    breakSuperSeries: async () => undefined as never,
    createWorkout: async () => undefined as never,
    createFromWorkout: async () => undefined as never,
  }),
  mesocycleRepository: () => ({
    getAll: async () => undefined as never,
    getById: async () => undefined as never,
    getWorkouts: async () => undefined as never,
    getByAthlete: async () => undefined as never,
    create: async () => undefined as never,
    update: async () => undefined as never,
    defineWeek: async () => undefined as never,
    redefineWeek: async () => undefined as never,
  }),
  exerciseRepository: () => ({
    getAll: async () => undefined as never,
    create: async () => undefined as never,
  }),
  exerciseFavoriteRepository: () => ({
    getAll: async () => undefined as never,
    mark: async () => undefined as never,
    unmark: async () => undefined as never,
  }),
  trainerAthleteRepository: () => ({
    getMine: async () => undefined as never,
    getByTrainer: async () => undefined as never,
    link: async () => undefined as never,
    end: async () => undefined as never,
  }),
  athleteInvitationRepository: () => ({
    getAll: async () => undefined as never,
    invite: async () => undefined as never,
    revoke: async () => undefined as never,
    accept: async () => undefined as never,
    reject: async () => undefined as never,
  }),
  customerLeadRepository: () => ({ getAll: async () => undefined as never }),
  signupUseCase: () => ({ execute: async () => undefined as never }),
  loginUseCase: () => ({ execute: async () => undefined as never }),
  getSessionUseCase: () => ({ execute: async () => ok(null) }),
  logoutUseCase: () => ({ execute: async () => undefined as never }),
  refreshSessionUseCase: () => ({ execute: async () => undefined as never }),
  getPreferredLanguageUseCase: () => ({ execute: async () => ok(null) }),
  setPreferredLanguageUseCase: () => ({ execute: async () => ok(undefined) }),
  getPreferredThemeModeUseCase: () => ({ execute: async () => ok(null) }),
  setPreferredThemeModeUseCase: () => ({ execute: async () => ok(undefined) }),
  getMyProfileUseCase: () => ({ execute: async () => ok(completeProfile) }),
  getUserTrainerUseCase: () => ({ execute: async () => undefined as never }),
  getMyExerciseLastPerformanceUseCase: () => ({ execute: async () => undefined as never }),
  updateMyProfileUseCase: () => ({ execute: async () => undefined as never }),
  getUsersUseCase: () => ({ execute: async () => undefined as never }),
  createUserUseCase: () => ({ execute: async () => undefined as never }),
  activateUserUseCase: () => ({ execute: async () => undefined as never }),
  deactivateUserUseCase: () => ({ execute: async () => undefined as never }),
  getWorkoutsUseCase: () => ({ execute: async () => undefined as never }),
  getWorkoutByIdUseCase: () => ({ execute: async () => undefined as never }),
  createWorkoutUseCase: () => ({ execute: async () => undefined as never }),
  updateWorkoutUseCase: () => ({ execute: async () => undefined as never }),
  deleteWorkoutUseCase: () => ({ execute: async () => undefined as never }),
  addExerciseToWorkoutUseCase: () => ({ execute: async () => undefined as never }),
  updateWorkoutExerciseUseCase: () => ({ execute: async () => undefined as never }),
  removeExerciseFromWorkoutUseCase: () => ({ execute: async () => undefined as never }),
  reorderWorkoutExercisesUseCase: () => ({ execute: async () => undefined as never }),
  createSuperSeriesUseCase: () => ({ execute: async () => undefined as never }),
  breakSuperSeriesUseCase: () => ({ execute: async () => undefined as never }),
  addSetRecordUseCase: () => ({ execute: async () => undefined as never }),
  updateSetRecordUseCase: () => ({ execute: async () => undefined as never }),
  removeSetRecordUseCase: () => ({ execute: async () => undefined as never }),
  reorderSetRecordsUseCase: () => ({ execute: async () => undefined as never }),
  reviewWorkoutUseCase: () => ({ execute: async () => undefined as never }),
  changeWorkoutStatusUseCase: () => ({ execute: async () => undefined as never }),
  getWorkoutTemplatesUseCase: () => ({ execute: async () => undefined as never }),
  getWorkoutTemplateByIdUseCase: () => ({ execute: async () => undefined as never }),
  createWorkoutTemplateUseCase: () => ({ execute: async () => undefined as never }),
  updateWorkoutTemplateUseCase: () => ({ execute: async () => undefined as never }),
  deleteWorkoutTemplateUseCase: () => ({ execute: async () => undefined as never }),
  addExerciseToWorkoutTemplateUseCase: () => ({ execute: async () => undefined as never }),
  updateWorkoutTemplateExerciseUseCase: () => ({ execute: async () => undefined as never }),
  removeExerciseFromWorkoutTemplateUseCase: () => ({ execute: async () => undefined as never }),
  reorderWorkoutTemplateExercisesUseCase: () => ({ execute: async () => undefined as never }),
  createWorkoutTemplateSuperSeriesUseCase: () => ({ execute: async () => undefined as never }),
  breakWorkoutTemplateSuperSeriesUseCase: () => ({ execute: async () => undefined as never }),
  createWorkoutFromTemplateUseCase: () => ({ execute: async () => undefined as never }),
  createWorkoutTemplateFromWorkoutUseCase: () => ({ execute: async () => undefined as never }),
  getMesocyclesUseCase: () => ({ execute: async () => undefined as never }),
  getMesocycleByIdUseCase: () => ({ execute: async () => undefined as never }),
  getMesocycleWorkoutsUseCase: () => ({ execute: async () => undefined as never }),
  getAthleteMesocyclesUseCase: () => ({ execute: async () => undefined as never }),
  createMesocycleUseCase: () => ({ execute: async () => undefined as never }),
  updateMesocycleUseCase: () => ({ execute: async () => undefined as never }),
  defineMesocycleWeekUseCase: () => ({ execute: async () => undefined as never }),
  redefineMesocycleWeekUseCase: () => ({ execute: async () => undefined as never }),
  // Every signed-in shell reads the catalog in the background, so the default answers an empty one.
  getExercisesUseCase: () => ({ execute: async () => ok([]) }),
  createExerciseUseCase: () => ({ execute: async () => undefined as never }),
  getExerciseFavoritesUseCase: () => ({ execute: async () => undefined as never }),
  markExerciseFavoriteUseCase: () => ({ execute: async () => undefined as never }),
  unmarkExerciseFavoriteUseCase: () => ({ execute: async () => undefined as never }),
  getMyAthletesUseCase: () => ({ execute: async () => undefined as never }),
  getTrainerAthletesUseCase: () => ({ execute: async () => undefined as never }),
  linkAthleteToTrainerUseCase: () => ({ execute: async () => undefined as never }),
  endTrainerAthleteUseCase: () => ({ execute: async () => undefined as never }),
  getAthleteInvitationsUseCase: () => ({ execute: async () => undefined as never }),
  inviteAthleteUseCase: () => ({ execute: async () => undefined as never }),
  revokeAthleteInvitationUseCase: () => ({ execute: async () => undefined as never }),
  acceptAthleteInvitationUseCase: () => ({ execute: async () => undefined as never }),
  rejectAthleteInvitationUseCase: () => ({ execute: async () => undefined as never }),
  getCustomerLeadsUseCase: () => ({ execute: async () => undefined as never }),
};

/** Renders a view in Spanish, with the real theme and a container of fakes. */
export default async function renderWithProviders(
  ui: ReactElement,
  dependencies: Partial<Registrations<Dependencies>> = {},
) {
  await i18n.changeLanguage("es");
  const container = new Container<Dependencies>({ ...unusedDependencies, ...dependencies });

  return render(
    <ThemeProvider theme={theme}>
      <I18nextProvider i18n={i18n}>
        <ContainerProvider container={container}>
          <SessionProvider>
            <CurrentUserProvider>
              <ExerciseCatalogProvider>{ui}</ExerciseCatalogProvider>
            </CurrentUserProvider>
          </SessionProvider>
        </ContainerProvider>
      </I18nextProvider>
    </ThemeProvider>,
  );
}
