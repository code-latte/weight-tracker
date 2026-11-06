import type { ReactElement } from "react";
import { render } from "@testing-library/react-native";
import { PaperProvider } from "react-native-paper";
import { I18nextProvider } from "react-i18next";
import type { Dependencies } from "@weight-tracker/core/application";
import { ok } from "@weight-tracker/core/domain";
import { Container, type Registrations } from "@weight-tracker/core/infrastructure";
import i18n from "@/infrastructure/i18n";
import SessionProvider from "@/presentation/auth/context/session-provider";
import ContainerProvider from "@/presentation/common/context/container-provider";
import theme from "@/presentation/common/theme";

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
    addExercise: async () => undefined as never,
    updateExercise: async () => undefined as never,
    removeExercise: async () => undefined as never,
    reorderExercises: async () => undefined as never,
    createSuperSeries: async () => undefined as never,
    breakSuperSeries: async () => undefined as never,
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
  signupUseCase: () => ({ execute: async () => undefined as never }),
  loginUseCase: () => ({ execute: async () => undefined as never }),
  getSessionUseCase: () => ({ execute: async () => ok(null) }),
  logoutUseCase: () => ({ execute: async () => undefined as never }),
  refreshSessionUseCase: () => ({ execute: async () => undefined as never }),
  getPreferredLanguageUseCase: () => ({ execute: async () => ok(null) }),
  setPreferredLanguageUseCase: () => ({ execute: async () => ok(undefined) }),
  getPreferredThemeModeUseCase: () => ({ execute: async () => ok(null) }),
  setPreferredThemeModeUseCase: () => ({ execute: async () => ok(undefined) }),
  getMyProfileUseCase: () => ({ execute: async () => undefined as never }),
  updateMyProfileUseCase: () => ({ execute: async () => undefined as never }),
  getUsersUseCase: () => ({ execute: async () => undefined as never }),
  createUserUseCase: () => ({ execute: async () => undefined as never }),
  activateUserUseCase: () => ({ execute: async () => undefined as never }),
  deactivateUserUseCase: () => ({ execute: async () => undefined as never }),
  getWorkoutsUseCase: () => ({ execute: async () => undefined as never }),
  getWorkoutByIdUseCase: () => ({ execute: async () => undefined as never }),
  createWorkoutUseCase: () => ({ execute: async () => undefined as never }),
  updateWorkoutUseCase: () => ({ execute: async () => undefined as never }),
  addExerciseToWorkoutUseCase: () => ({ execute: async () => undefined as never }),
  updateWorkoutExerciseUseCase: () => ({ execute: async () => undefined as never }),
  removeExerciseFromWorkoutUseCase: () => ({ execute: async () => undefined as never }),
  reorderWorkoutExercisesUseCase: () => ({ execute: async () => undefined as never }),
  createSuperSeriesUseCase: () => ({ execute: async () => undefined as never }),
  breakSuperSeriesUseCase: () => ({ execute: async () => undefined as never }),
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
  getExercisesUseCase: () => ({ execute: async () => undefined as never }),
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
};

/** Renders a view in Spanish, with the real theme and a container of fakes. */
export default async function renderWithProviders(
  ui: ReactElement,
  dependencies: Partial<Registrations<Dependencies>> = {},
) {
  await i18n.changeLanguage("es");
  const container = new Container<Dependencies>({ ...unusedDependencies, ...dependencies });

  return render(
    <PaperProvider theme={theme}>
      <I18nextProvider i18n={i18n}>
        <ContainerProvider container={container}>
          <SessionProvider>{ui}</SessionProvider>
        </ContainerProvider>
      </I18nextProvider>
    </PaperProvider>,
  );
}
