import { act, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { DomainError, fail, ok } from "@weight-tracker/core/domain";
import type {
  AddExerciseToWorkoutInput,
  BreakSuperSeriesInput,
  ChangeWorkoutStatusInput,
  CreateSuperSeriesInput,
  Exercise,
  ExerciseFavorite,
  ExerciseHistorySession,
  RemoveExerciseFromWorkoutInput,
  ReorderWorkoutExercisesInput,
  Result,
  Session,
  SuperSeries,
  UpdateWorkoutExerciseInput,
  UpdateWorkoutInput,
  UserProfile,
  Workout,
  WorkoutById,
  WorkoutByIdExercise,
  WorkoutDetail,
  WorkoutExerciseDetail,
  WorkoutSummary,
  WorkoutTemplate,
} from "@weight-tracker/core/domain";
import i18n from "@/infrastructure/i18n";
import theme from "@/presentation/common/theme";
import WorkoutView from "@/presentation/workouts/views/workout-view";
import LoadExerciseCatalog from "../../../support/load-exercise-catalog";
import renderWithProviders, { completeProfile } from "../../../support/render-with-providers";

const replace = vi.fn();
const push = vi.fn();
const navigation = vi.hoisted(() => ({ search: "id=w-1" }));

vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace, push }),
  usePathname: () => "/workout",
  useSearchParams: () => new URLSearchParams(navigation.search),
}));

/** jsdom has no layout, so the chart atoms are replaced by stubs that expose the points they were given. */
vi.mock("@/presentation/common/components/atoms/line-chart", () => ({
  default: ({ labels, values }: { labels: string[]; values: number[] }) => (
    <div data-testid="line-chart" data-labels={labels.join("|")} data-values={values.join("|")} />
  ),
}));
vi.mock("@/presentation/common/components/atoms/bar-chart", () => ({
  default: ({ labels, values }: { labels: string[]; values: number[] }) => (
    <div data-testid="bar-chart" data-labels={labels.join("|")} data-values={values.join("|")} />
  ),
}));

function catalogExercise(
  id: string,
  muscle: string,
  bodyPart: string,
  [es, en]: [string, string],
  videoUrl: string | null,
  type = "STRENGTH",
  ownerId: string | null = null,
): Exercise {
  return {
    id,
    freeExerciseDbId: null,
    videoUrl,
    muscle,
    bodyPart,
    type,
    translations: [
      { languageCode: "es", name: es },
      { languageCode: "en", name: en },
    ],
    ownerId,
    archivedAtUtc: null,
  };
}

const squat = catalogExercise(
  "ex-squat",
  "QUADRICEPS",
  "LEGS",
  ["Sentadilla", "Squat"],
  "https://www.youtube.com/watch?v=h_yZV27H684",
);
const legPress = catalogExercise(
  "ex-press",
  "QUADRICEPS",
  "LEGS",
  ["Prensa de piernas", "Leg press"],
  "https://example.com/leg-press.mp4",
);
const bench = catalogExercise(
  "ex-bench",
  "CHEST",
  "CHEST",
  ["Press de banca", "Bench press"],
  null,
);
const catalog = [squat, legPress, bench];

/** Two muscles for arms and a second one for legs, so the muscle pills have something to refine. */
const curl = catalogExercise("ex-curl", "BICEPS", "ARMS", ["Curl de bíceps", "Biceps curl"], null);
const pushdown = catalogExercise(
  "ex-pushdown",
  "TRICEPS",
  "ARMS",
  ["Extensión de tríceps", "Triceps pushdown"],
  null,
);
const hammerCurl = catalogExercise(
  "ex-hammer",
  "BICEPS",
  "ARMS",
  ["Curl martillo", "Hammer curl"],
  null,
);
const legCurl = catalogExercise(
  "ex-leg-curl",
  "HAMSTRINGS",
  "LEGS",
  ["Curl femoral", "Leg curl"],
  null,
);
const muscleCatalog = async () => ok([...catalog, curl, pushdown, hammerCurl, legCurl]);

/** One exercise of each other type and one of a type the backoffice does not know. */
const bike = catalogExercise(
  "ex-bike",
  "QUADRICEPS",
  "LEGS",
  ["Bicicleta", "Bike"],
  null,
  "CARDIO",
);
const hamstringStretch = catalogExercise(
  "ex-stretch",
  "HAMSTRINGS",
  "LEGS",
  ["Estiramiento de isquios", "Hamstring stretch"],
  null,
  "STRETCHING",
);
const treePose = catalogExercise(
  "ex-tree",
  "ABDOMINALS",
  "CORE",
  ["Postura del árbol", "Tree pose"],
  null,
  "YOGA",
);
const typeCatalog = async () => ok([...catalog, bike, hamstringStretch, treePose]);

/** Every catalog exercise of these tests, so a workout entry can carry its exercise's data as the API sends it. */
const knownExercises = new Map(
  [...catalog, curl, pushdown, hammerCurl, legCurl, bike, hamstringStretch, treePose].map(
    (exercise) => [exercise.id, exercise],
  ),
);

function workoutExercise(
  id: string,
  exerciseId: string,
  effort: string,
  sets: WorkoutExerciseDetail["sets"],
  { perSide = false, restMinutes = null }: Partial<WorkoutExerciseDetail> = {},
): WorkoutByIdExercise {
  const exercise = knownExercises.get(exerciseId);

  return {
    id,
    exerciseId,
    translations: exercise?.translations ?? [],
    videoUrl: exercise?.videoUrl ?? null,
    muscle: exercise?.muscle ?? "",
    bodyPart: exercise?.bodyPart ?? "",
    type: exercise?.type ?? "",
    effort,
    perSide,
    restMinutes,
    sets,
    superSeriesId: null,
    setRecords: [],
  };
}

const squatInWorkout = workoutExercise("we-1", squat.id, "HIGH", [
  { count: 3, target: { type: "Reps", value: 10, maxValue: null } },
]);
const benchInWorkout = workoutExercise("we-2", bench.id, "MID", [
  { count: 4, target: { type: "RepsRange", value: 8, maxValue: 12 } },
]);
const pressInWorkout = workoutExercise("we-3", legPress.id, "LOW", []);

const ana = {
  id: "5f1c2f0e-9d5b-4a53-8c7e-0d6f7c1a2b3c",
  email: "ana@example.com",
  name: "Ana",
  surname: "García",
  role: "USER",
};

const workout: WorkoutById = {
  id: "w-1",
  name: "Pierna",
  description: "Día 1",
  workoutDate: "2026-11-02",
  userId: "5f1c2f0e-9d5b-4a53-8c7e-0d6f7c1a2b3c",
  trainerId: null,
  status: "DRAFT",
  mesocycleId: null,
  mesocycleWeekNumber: null,
  startedAt: null,
  completedAt: null,
  reviewedAt: null,
  reviewedBy: null,
  resultsUpdatedAt: null,
  exercises: [squatInWorkout, benchInWorkout, pressInWorkout],
  athlete: ana,
  trainer: null,
  mesocycle: null,
};

/** What the API answers to an update: the saved data, with the list response's shape. */
function savedWorkout({ id, ...request }: UpdateWorkoutInput): Workout {
  return {
    ...workout,
    id,
    name: request.name ?? workout.name,
    description: request.description ?? null,
    workoutDate: request.workoutDate ?? workout.workoutDate,
    exercises: [],
  };
}

/** What the API answers to an exercise save: the entry with the sets, rest and per side it received. */
function entryOf(
  { exerciseId, effort, sets, perSide, restMinutes }: AddExerciseToWorkoutInput,
  id: string,
): WorkoutExerciseDetail {
  return {
    id,
    exerciseId: exerciseId ?? "",
    effort: effort ?? "",
    perSide: perSide ?? false,
    restMinutes: restMinutes ?? null,
    superSeriesId: null,
    setRecords: [],
    sets: (sets ?? []).map(({ count, targetType, target, maxTarget, targetMinutes }) => ({
      count: count ?? 0,
      target:
        targetType === "Minutes"
          ? { type: targetType, minutes: targetMinutes ?? null }
          : { type: targetType ?? "", value: target ?? null, maxValue: maxTarget ?? null },
    })),
  };
}

/** Fills the set form of the configuration dialog and adds the set to its list. */
async function addSet(
  user: ReturnType<typeof userEvent.setup>,
  { count, type, value, max }: { count: string; type?: string; value?: string; max?: string },
) {
  const form = within(configurationDialog()).getByRole("group", { name: "Añadir serie" });
  if (type) {
    await user.click(within(form).getByRole("combobox", { name: "Tipo" }));
    await user.click(screen.getByRole("option", { name: type }));
  }
  await user.type(within(form).getByRole("spinbutton", { name: "Series" }), count);
  if (value) {
    // A minutes value is typed as `M:SS`, in a text field.
    const role = type === "Minutos" ? "textbox" : "spinbutton";
    await user.type(within(form).getByRole(role, { name: "Valor" }), value);
  }
  if (max) {
    await user.type(within(form).getByRole("spinbutton", { name: "Máximo" }), max);
  }
  await user.click(within(configurationDialog()).getByRole("button", { name: "Añadir serie" }));
}

async function chooseEffort(user: ReturnType<typeof userEvent.setup>, effort: string) {
  await user.click(within(configurationDialog()).getByRole("combobox", { name: "Esfuerzo" }));
  await user.click(screen.getByRole("option", { name: effort }));
}

const setSummaries = () =>
  within(within(configurationDialog()).getByRole("list", { name: "Series" }))
    .getAllByRole("listitem")
    .map((item) => item.textContent);
/** A workout without exercises, so the catalog lists all of its exercises. */
const emptyWorkout: WorkoutById = { ...workout, exercises: [] };
const withoutExercises = async () => ok(emptyWorkout);

/** What the API answers when the workout is saved as a template. */
const savedTemplate: WorkoutTemplate = {
  id: "wt-9",
  name: "Pierna",
  description: "Día 1",
  trainerId: "6a2d3f1e-0e6c-4b64-9d8f-1e7a8d2b3c4d",
  exercises: [],
};

function favoriteOf(exerciseId: string): ExerciseFavorite {
  return { exerciseId, createdAtUtc: "2026-11-03T09:30:00Z" };
}

function renderWorkout({
  getWorkout = async () => ok(workout),
  getExercises = async () => ok(catalog),
  catalogLoadedFirst = false,
  session = async () => ok({ accessToken: "a", refreshToken: "r" }),
  updateWorkout = async (input) => ok(savedWorkout(input)),
  addExercise = async (input) => ok(entryOf(input, "we-new")),
  updateExercise = async (input) => ok(entryOf(input, "we-1")),
  removeExercise = async () => ok(undefined),
  reorderExercises = async () => ok(workout),
  createSuperSeries = async () => ok({ id: 7 }),
  breakSuperSeries = async () => ok(undefined),
  getFavorites = async () => ok([]),
  markFavorite = async (exerciseId) => ok(favoriteOf(exerciseId)),
  unmarkFavorite = async () => ok(undefined),
  profile = completeProfile,
  saveAsTemplate = async () => ok(savedTemplate),
  changeStatus = async ({ status }) => ok({ ...workout, status: status ?? workout.status }),
  getSummary = async () => fail(new DomainError("UNEXPECTED", "Not used")),
  getHistory = async () => ok([]),
}: {
  getSummary?: (input: { workoutId: string }) => Promise<Result<WorkoutSummary, DomainError>>;
  getHistory?: (input: {
    userId: string;
    exerciseId: string;
    limit: number;
  }) => Promise<Result<ExerciseHistorySession[], DomainError>>;
  changeStatus?: (input: ChangeWorkoutStatusInput) => Promise<Result<WorkoutDetail, DomainError>>;
  saveAsTemplate?: () => Promise<Result<WorkoutTemplate, DomainError>>;
  getWorkout?: () => Promise<Result<WorkoutById, DomainError>>;
  getExercises?: () => Promise<Result<Exercise[], DomainError>>;
  /** Opens the screen after the session's catalog was read, as after sign-in. */
  catalogLoadedFirst?: boolean;
  getFavorites?: () => Promise<Result<ExerciseFavorite[], DomainError>>;
  markFavorite?: (exerciseId: string) => Promise<Result<ExerciseFavorite, DomainError>>;
  unmarkFavorite?: (exerciseId: string) => Promise<Result<void, DomainError>>;
  profile?: UserProfile;
  session?: () => Promise<Result<Session | null, DomainError>>;
  updateWorkout?: (input: UpdateWorkoutInput) => Promise<Result<Workout, DomainError>>;
  addExercise?: (
    input: AddExerciseToWorkoutInput,
  ) => Promise<Result<WorkoutExerciseDetail, DomainError>>;
  updateExercise?: (
    input: UpdateWorkoutExerciseInput,
  ) => Promise<Result<WorkoutExerciseDetail, DomainError>>;
  removeExercise?: (input: RemoveExerciseFromWorkoutInput) => Promise<Result<void, DomainError>>;
  reorderExercises?: (
    input: ReorderWorkoutExercisesInput,
  ) => Promise<Result<WorkoutDetail, DomainError>>;
  createSuperSeries?: (input: CreateSuperSeriesInput) => Promise<Result<SuperSeries, DomainError>>;
  breakSuperSeries?: (input: BreakSuperSeriesInput) => Promise<Result<void, DomainError>>;
} = {}) {
  const readWorkout = vi.fn(getWorkout);
  const readSummary = vi.fn(getSummary);
  const readHistory = vi.fn(getHistory);
  const readCatalog = vi.fn(getExercises);
  // The workout carries its mesocycle: registered only to prove the screen never reads it.
  const readMesocycle = vi.fn(async () => fail(new DomainError("UNEXPECTED", "Not used")));
  const favorites = {
    read: vi.fn(getFavorites),
    mark: vi.fn(markFavorite),
    unmark: vi.fn(unmarkFavorite),
  };
  const mutations = {
    create: vi.fn(async () => fail(new DomainError("UNEXPECTED", ""))),
    update: vi.fn(updateWorkout),
    addExercise: vi.fn(addExercise),
    updateExercise: vi.fn(updateExercise),
    removeExercise: vi.fn(removeExercise),
    reorderExercises: vi.fn(reorderExercises),
    createSuperSeries: vi.fn(createSuperSeries),
    breakSuperSeries: vi.fn(breakSuperSeries),
    saveAsTemplate: vi.fn(saveAsTemplate),
    changeStatus: vi.fn(changeStatus),
  };

  return {
    readWorkout,
    readSummary,
    readHistory,
    readCatalog,
    readMesocycle,
    favorites,
    mutations,
    rendered: renderWithProviders(
      catalogLoadedFirst ? (
        <LoadExerciseCatalog>
          <WorkoutView />
        </LoadExerciseCatalog>
      ) : (
        <WorkoutView />
      ),
      {
        getSessionUseCase: () => ({ execute: session }),
        getMyProfileUseCase: () => ({ execute: async () => ok(profile) }),
        getWorkoutByIdUseCase: () => ({ execute: readWorkout }),
        getWorkoutSummaryUseCase: () => ({ execute: readSummary }),
        getUserExerciseHistoryUseCase: () => ({ execute: readHistory }),
        getMesocycleByIdUseCase: () => ({ execute: readMesocycle }),
        getExercisesUseCase: () => ({ execute: readCatalog }),
        getExerciseFavoritesUseCase: () => ({ execute: favorites.read }),
        markExerciseFavoriteUseCase: () => ({ execute: favorites.mark }),
        unmarkExerciseFavoriteUseCase: () => ({ execute: favorites.unmark }),
        createWorkoutUseCase: () => ({ execute: mutations.create }),
        updateWorkoutUseCase: () => ({ execute: mutations.update }),
        addExerciseToWorkoutUseCase: () => ({ execute: mutations.addExercise }),
        updateWorkoutExerciseUseCase: () => ({ execute: mutations.updateExercise }),
        removeExerciseFromWorkoutUseCase: () => ({ execute: mutations.removeExercise }),
        reorderWorkoutExercisesUseCase: () => ({ execute: mutations.reorderExercises }),
        createSuperSeriesUseCase: () => ({ execute: mutations.createSuperSeries }),
        breakSuperSeriesUseCase: () => ({ execute: mutations.breakSuperSeries }),
        createWorkoutTemplateFromWorkoutUseCase: () => ({ execute: mutations.saveAsTemplate }),
        changeWorkoutStatusUseCase: () => ({ execute: mutations.changeStatus }),
      },
    ),
  };
}

const propertiesSection = () =>
  screen.getByRole("region", { name: "Propiedades del entrenamiento" });
/** While a dialog is open the page behind it is hidden from assistive technology; `hidden` still reaches it. */
const exercisesSection = ({ hidden = false } = {}) =>
  screen.getByRole("region", { name: "Ejercicios", hidden });
const catalogDialog = () => screen.getByRole("dialog", { name: "Catálogo de ejercicios" });
const configurationDialog = () =>
  screen.getByRole("dialog", { name: "Configuración del ejercicio" });

/** The exercise cards of the catalog, without the items of its section sidebar; none while the list is empty. */
function catalogItems() {
  const list = within(catalogDialog()).queryByRole("list", { name: "Catálogo de ejercicios" });

  return list ? within(list).queryAllByRole("listitem") : [];
}

function catalogNames() {
  return catalogItems().map((item) => within(item).getAllByText(/./)[0]?.textContent);
}

async function loaded() {
  await screen.findByRole("region", { name: "Ejercicios" });
}

async function openCatalog(user: ReturnType<typeof userEvent.setup>) {
  await user.click(within(exercisesSection()).getByRole("button", { name: "Añadir ejercicio" }));
  await screen.findByRole("dialog", { name: "Catálogo de ejercicios" });
}

/** Opens the catalog and chooses the exercise named `name`, which opens its configuration. */
async function chooseFromCatalog(user: ReturnType<typeof userEvent.setup>, name: string) {
  await openCatalog(user);
  const item = catalogItems().find((candidate) => within(candidate).queryByText(name) !== null);
  await user.click(within(item!).getByRole("button", { name: "Elegir ejercicio" }));
  await screen.findByRole("dialog", { name: "Configuración del ejercicio" });
}

/** Opens the configuration of the listed exercise at `index` with its pencil. */
async function editExercise(user: ReturnType<typeof userEvent.setup>, index: number) {
  await user.click(workoutButtons("Editar ejercicio")[index]!);
  await screen.findByRole("dialog", { name: "Configuración del ejercicio" });
}

async function dialogsClosed() {
  await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
}

function workoutItems({ hidden = false } = {}) {
  return within(exercisesSection({ hidden })).queryAllByRole("listitem", { hidden });
}

/** Each workout exercise as its position and name, in the order listed. */
function workoutRows({ hidden = false } = {}) {
  return workoutItems({ hidden }).map((item) =>
    within(item)
      .getAllByText(/./)
      .slice(0, 2)
      .map((text) => text.textContent),
  );
}

function workoutButtons(name: string) {
  return within(exercisesSection()).getAllByRole("button", { name });
}

/**
 * jsdom has no layout, so every element would sit at the same place and dnd-kit's keyboard sensor could not move
 * between cards. Each list item gets its own cell instead, in rows of three as on a wide screen, by its place among its
 * siblings.
 */
function stubListItemRects() {
  return vi.spyOn(HTMLElement.prototype, "getBoundingClientRect").mockImplementation(function (
    this: HTMLElement,
  ) {
    const index =
      this.tagName === "LI" && this.parentElement
        ? Array.from(this.parentElement.children).indexOf(this)
        : 0;
    const left = (index % 3) * 310;
    const top = Math.floor(index / 3) * 160;
    return {
      x: left,
      y: top,
      top,
      left,
      right: left + 300,
      bottom: top + 150,
      width: 300,
      height: 150,
      toJSON: () => ({}),
    };
  });
}

/** Picks up the workout exercise at `from` by its handle, moves it `steps` cards to the right and drops it. */
async function moveRight(user: ReturnType<typeof userEvent.setup>, from: number, steps: number) {
  workoutButtons("Reordenar ejercicio")[from]!.focus();
  await user.keyboard("[Space]");
  for (let step = 0; step < steps; step += 1) {
    await user.keyboard("[ArrowRight]");
  }
  await user.keyboard("[Space]");
}

/** A workout whose second and third exercises, Press de banca and Prensa de piernas, form superseries 1. */
const groupedWorkout: WorkoutById = {
  ...workout,
  exercises: [
    squatInWorkout,
    { ...benchInWorkout, superSeriesId: 1 },
    { ...pressInWorkout, superSeriesId: 1 },
  ],
};
const withGroup = async () => ok(groupedWorkout);

/** The exercise cards as their position and name, in the order listed, without the group cards around them. */
function exerciseRows() {
  return workoutItems()
    .filter((item) => within(item).queryByRole("list") === null)
    .map((item) =>
      within(item)
        .getAllByText(/./)
        .slice(0, 2)
        .map((text) => text.textContent),
    );
}

/** The exercise card named `name`, inside a group card or not. */
function exerciseItem(name: string) {
  return workoutItems().find(
    (item) => within(item).queryByRole("list") === null && within(item).queryByText(name) !== null,
  )!;
}

const selectionBox = (name: string) =>
  within(exerciseItem(name)).queryByRole("checkbox", { name: "Seleccionar para superserie" });

const groupButton = () =>
  within(exercisesSection()).queryByRole("button", { name: "Agrupar en superserie" });

const breakDialog = () => screen.queryByRole("dialog", { name: "¿Romper superserie?" });

async function check(user: ReturnType<typeof userEvent.setup>, ...names: string[]) {
  for (const name of names) {
    await user.click(selectionBox(name)!);
  }
}

function mutationCalls(mutations: ReturnType<typeof renderWorkout>["mutations"]) {
  return Object.values(mutations).reduce((total, mock) => total + mock.mock.calls.length, 0);
}

describe("WorkoutView", () => {
  beforeEach(() => {
    replace.mockClear();
    push.mockClear();
    navigation.search = "id=w-1";
    // Only `Date` is faked, so user events, dnd-kit and promises keep running; the current year is 2026.
    vi.useFakeTimers({ toFake: ["Date"] });
    vi.setSystemTime(new Date(2026, 9, 29, 12));
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it("reads the workout and the catalog once", async () => {
    // Arrange & Act
    const { readWorkout, readCatalog, rendered } = renderWorkout();
    await rendered;

    // Assert
    await loaded();
    expect(readWorkout).toHaveBeenCalledTimes(1);
    expect(readWorkout).toHaveBeenCalledWith({ id: "w-1" });
    expect(readCatalog).toHaveBeenCalledTimes(1);
    expect(screen.getByRole("navigation", { name: "Menú principal" })).toBeInTheDocument();
  });

  it("uses the session's catalog and does not read it on opening once it was read", async () => {
    // Arrange
    const { readWorkout, readCatalog, rendered } = renderWorkout({ catalogLoadedFirst: true });

    // Act
    await rendered;
    await loaded();

    // Assert
    expect(readWorkout).toHaveBeenCalledTimes(1);
    expect(readCatalog).toHaveBeenCalledTimes(1);
  });

  it("sends a visitor without a session to login and reads no workout", async () => {
    // Arrange & Act
    const { readWorkout, rendered } = renderWorkout({ session: async () => ok(null) });
    await rendered;

    // Assert
    await waitFor(() => expect(replace).toHaveBeenCalledWith("/login"));
    expect(readWorkout).not.toHaveBeenCalled();
  });

  it("shows the properties and the exercises cards in that order, and no dialog", async () => {
    // Arrange & Act
    await renderWorkout().rendered;

    // Assert
    await loaded();
    const regions = screen
      .getAllByRole("region")
      .map((region) => within(region).queryByRole("heading", { level: 2 })?.textContent)
      .filter(Boolean);
    expect(regions).toEqual(["Propiedades del entrenamiento", "Ejercicios"]);
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  });

  it("opens the catalog from Añadir ejercicio without a request", async () => {
    // Arrange
    const { mutations, rendered } = renderWorkout();
    await rendered;
    await loaded();
    const user = userEvent.setup();

    // Act
    await user.click(within(exercisesSection()).getByRole("button", { name: "Añadir ejercicio" }));

    // Assert
    expect(
      await screen.findByRole("dialog", { name: "Catálogo de ejercicios" }),
    ).toBeInTheDocument();
    expect(mutationCalls(mutations)).toBe(0);
  });

  it("closes the catalog with Escape without changing the workout or sending a request", async () => {
    // Arrange
    const { mutations, rendered } = renderWorkout();
    await rendered;
    await loaded();
    const user = userEvent.setup();
    await openCatalog(user);

    // Act
    await user.keyboard("{Escape}");

    // Assert
    await dialogsClosed();
    expect(workoutRows().map(([, name]) => name)).toEqual([
      "Sentadilla",
      "Press de banca",
      "Prensa de piernas",
    ]);
    expect(mutationCalls(mutations)).toBe(0);
  });

  it("closes the catalog with Cerrar without a request", async () => {
    // Arrange
    const { mutations, rendered } = renderWorkout();
    await rendered;
    await loaded();
    const user = userEvent.setup();
    await openCatalog(user);

    // Act
    await user.click(within(catalogDialog()).getByRole("button", { name: "Cerrar" }));

    // Assert
    await dialogsClosed();
    expect(mutationCalls(mutations)).toBe(0);
  });

  it("shows one pill per body part and every catalog exercise", async () => {
    // Arrange
    await renderWorkout({ getWorkout: withoutExercises }).rendered;
    await loaded();
    const user = userEvent.setup();

    // Act
    await openCatalog(user);

    // Assert
    const pills = within(
      within(catalogDialog()).getByRole("group", { name: "Partes del cuerpo" }),
    ).getAllByRole("button", { pressed: false });
    expect(pills.map((pill) => pill.textContent)).toEqual(["Pecho", "Piernas"]);
    expect(catalogNames()).toEqual(["Sentadilla", "Prensa de piernas", "Press de banca"]);
  });

  it("shows the English name under the name in the selector, and never offers the video", async () => {
    // Arrange
    await renderWorkout({ getWorkout: withoutExercises }).rendered;
    await loaded();
    const user = userEvent.setup();

    // Act
    await openCatalog(user);

    // Assert
    const item = catalogItems().find((candidate) => within(candidate).queryByText("Sentadilla"))!;
    expect(within(item).getByText("Squat")).toBeInTheDocument();
    expect(within(catalogDialog()).queryByRole("button", { name: "Ver vídeo" })).toBeNull();
  });

  it("filters the catalog by name, ignoring case and accents", async () => {
    // Arrange
    await renderWorkout({ getWorkout: withoutExercises }).rendered;
    await loaded();
    const user = userEvent.setup();
    await openCatalog(user);

    // Act
    await user.type(
      within(catalogDialog()).getByRole("searchbox", { name: "Buscar ejercicio" }),
      "SENTADÍ",
    );

    // Assert
    expect(catalogNames()).toEqual(["Sentadilla"]);
  });

  it("combines the body-part pill and the text", async () => {
    // Arrange
    await renderWorkout({ getWorkout: withoutExercises }).rendered;
    await loaded();
    const user = userEvent.setup();
    await openCatalog(user);
    await user.click(within(catalogDialog()).getByRole("button", { name: "Piernas" }));

    // Act
    await user.type(
      within(catalogDialog()).getByRole("searchbox", { name: "Buscar ejercicio" }),
      "prensa",
    );

    // Assert
    expect(catalogNames()).toEqual(["Prensa de piernas"]);
  });

  it("clears the body part when its pill is selected again", async () => {
    // Arrange
    await renderWorkout({ getWorkout: withoutExercises }).rendered;
    await loaded();
    const user = userEvent.setup();
    await openCatalog(user);
    const pill = within(catalogDialog()).getByRole("button", { name: "Pecho" });
    await user.click(pill);
    expect(catalogNames()).toEqual(["Press de banca"]);

    // Act
    await user.click(pill);

    // Assert
    expect(pill).toHaveAttribute("aria-pressed", "false");
    expect(catalogNames()).toHaveLength(3);
  });

  it("says so when no exercise matches the filters", async () => {
    // Arrange
    await renderWorkout().rendered;
    await loaded();
    const user = userEvent.setup();
    await openCatalog(user);

    // Act
    await user.type(
      within(catalogDialog()).getByRole("searchbox", { name: "Buscar ejercicio" }),
      "remo",
    );

    // Assert
    expect(
      within(catalogDialog()).getByText("Ningún ejercicio coincide con el filtro."),
    ).toBeInTheDocument();
    expect(catalogNames()).toEqual([]);
  });

  it("keeps the filters when the catalog is opened again", async () => {
    // Arrange
    await renderWorkout({ getWorkout: withoutExercises }).rendered;
    await loaded();
    const user = userEvent.setup();
    await openCatalog(user);
    await user.type(
      within(catalogDialog()).getByRole("searchbox", { name: "Buscar ejercicio" }),
      "prensa",
    );
    await user.keyboard("{Escape}");
    await dialogsClosed();

    // Act
    await openCatalog(user);

    // Assert
    expect(
      within(catalogDialog()).getByRole("searchbox", { name: "Buscar ejercicio" }),
    ).toHaveValue("prensa");
    expect(catalogNames()).toEqual(["Prensa de piernas"]);
  });

  it("shows each catalog exercise in its own outlined card with Elegir ejercicio", async () => {
    // Arrange
    await renderWorkout({ getWorkout: withoutExercises }).rendered;
    await loaded();
    const user = userEvent.setup();

    // Act
    await openCatalog(user);

    // Assert
    const items = catalogItems();
    expect(items).toHaveLength(3);
    for (const item of items) {
      expect(item.querySelectorAll(".MuiCard-root.MuiPaper-outlined")).toHaveLength(1);
      expect(within(item).getByRole("button", { name: "Elegir ejercicio" })).toBeInTheDocument();
    }
  });

  it("labels each exercise with its body part and muscle", async () => {
    // Arrange
    await renderWorkout({ getWorkout: withoutExercises, getExercises: muscleCatalog }).rendered;
    await loaded();
    const user = userEvent.setup();

    // Act
    await openCatalog(user);

    // Assert
    const [squatItem] = catalogItems();
    expect(within(squatItem!).getByText("Piernas")).toBeInTheDocument();
    expect(within(squatItem!).getByText("Cuádriceps")).toBeInTheDocument();
  });

  it("shows a workout exercise missing from the catalog by its id, without labels", async () => {
    // Arrange
    const orphan = workoutExercise("we-9", "ex-unknown", "MID", []);
    const getWorkout = async () => ok({ ...workout, exercises: [orphan] });

    // Act
    await renderWorkout({ getWorkout }).rendered;

    // Assert
    await loaded();
    const [item] = workoutItems();
    expect(item).toHaveTextContent("ex-unknown");
    expect(within(item!).queryByText("Cuádriceps")).not.toBeInTheDocument();
  });

  it("opens a chosen exercise's configuration empty, without listing it or sending a request", async () => {
    // Arrange
    const withTwo = { ...workout, exercises: [squatInWorkout, benchInWorkout] };
    const { mutations, readWorkout, rendered } = renderWorkout({
      getWorkout: async () => ok(withTwo),
    });
    await rendered;
    await loaded();
    const user = userEvent.setup();
    await openCatalog(user);

    // Act
    await user.click(within(catalogDialog()).getByRole("button", { name: "Elegir ejercicio" }));

    // Assert
    const dialog = await screen.findByRole("dialog", { name: "Configuración del ejercicio" });
    await waitFor(() =>
      expect(
        screen.queryByRole("dialog", { name: "Catálogo de ejercicios" }),
      ).not.toBeInTheDocument(),
    );
    expect(within(dialog).getByRole("heading", { name: "Prensa de piernas" })).toBeInTheDocument();
    expect(within(dialog).getByRole("combobox", { name: "Esfuerzo" })).not.toHaveTextContent(/\w/);
    expect(within(dialog).getByText("Todavía no hay series.")).toBeInTheDocument();
    const form = within(dialog).getByRole("group", { name: "Añadir serie" });
    expect(within(form).getByRole("spinbutton", { name: "Series" })).toHaveValue(null);
    expect(within(form).getByRole("spinbutton", { name: "Valor" })).toHaveValue(null);
    expect(workoutItems({ hidden: true })).toHaveLength(2);
    expect(mutationCalls(mutations)).toBe(0);
    expect(readWorkout).toHaveBeenCalledTimes(1);
  });

  it("hides from the catalog the exercises already in the workout", async () => {
    // Arrange
    await renderWorkout({ getWorkout: async () => ok({ ...workout, exercises: [squatInWorkout] }) })
      .rendered;
    await loaded();
    const user = userEvent.setup();

    // Act
    await openCatalog(user);

    // Assert
    expect(catalogNames()).toEqual(["Prensa de piernas", "Press de banca"]);
  });

  it("leaves an added exercise out of the catalog", async () => {
    // Arrange
    await renderWorkout({ getWorkout: withoutExercises }).rendered;
    await loaded();
    const user = userEvent.setup();
    await chooseFromCatalog(user, "Prensa de piernas");
    await chooseEffort(user, "RIR 0");
    await addSet(user, { count: "2", value: "15" });
    await user.click(within(configurationDialog()).getByRole("button", { name: "Guardar" }));
    await dialogsClosed();

    // Act
    await openCatalog(user);

    // Assert
    expect(catalogNames()).toEqual(["Sentadilla", "Press de banca"]);
  });

  it("closes a chosen exercise with Cancelar without adding it or sending a request", async () => {
    // Arrange
    const withTwo = { ...workout, exercises: [squatInWorkout, benchInWorkout] };
    const { mutations, rendered } = renderWorkout({ getWorkout: async () => ok(withTwo) });
    await rendered;
    await loaded();
    const user = userEvent.setup();
    await chooseFromCatalog(user, "Prensa de piernas");
    await addSet(user, { count: "2", value: "15" });

    // Act
    await user.click(within(configurationDialog()).getByRole("button", { name: "Cancelar" }));

    // Assert
    await dialogsClosed();
    expect(workoutRows().map(([, name]) => name)).toEqual(["Sentadilla", "Press de banca"]);
    expect(mutationCalls(mutations)).toBe(0);
  });

  it("lists a dismissed exercise in the catalog again", async () => {
    // Arrange
    await renderWorkout({ getWorkout: withoutExercises }).rendered;
    await loaded();
    const user = userEvent.setup();
    await chooseFromCatalog(user, "Prensa de piernas");
    await user.click(within(configurationDialog()).getByRole("button", { name: "Cancelar" }));
    await dialogsClosed();

    // Act
    await openCatalog(user);

    // Assert
    expect(catalogNames()).toEqual(["Sentadilla", "Prensa de piernas", "Press de banca"]);
  });

  it("returns the focus to Añadir ejercicio when an add flow ends", async () => {
    // Arrange
    await renderWorkout({ getWorkout: withoutExercises }).rendered;
    await loaded();
    const user = userEvent.setup();
    await chooseFromCatalog(user, "Sentadilla");

    // Act
    await user.click(within(configurationDialog()).getByRole("button", { name: "Cancelar" }));

    // Assert
    await dialogsClosed();
    await waitFor(() =>
      expect(
        within(exercisesSection()).getByRole("button", { name: "Añadir ejercicio" }),
      ).toHaveFocus(),
    );
  });

  it("closes an edited exercise with Escape without a request", async () => {
    // Arrange
    const { mutations, rendered } = renderWorkout();
    await rendered;
    await loaded();
    const user = userEvent.setup();
    await editExercise(user, 0);
    await chooseEffort(user, "RIR 5+");

    // Act
    await user.keyboard("{Escape}");

    // Assert
    await dialogsClosed();
    expect(mutationCalls(mutations)).toBe(0);
  });

  it("shows the stored values again when a dismissed exercise is opened again", async () => {
    // Arrange
    await renderWorkout().rendered;
    await loaded();
    const user = userEvent.setup();
    await editExercise(user, 0);
    await chooseEffort(user, "RIR 5+");
    await user.click(within(configurationDialog()).getByRole("button", { name: "Quitar serie" }));
    await user.click(within(configurationDialog()).getByRole("button", { name: "Cancelar" }));
    await dialogsClosed();

    // Act
    await editExercise(user, 0);

    // Assert
    expect(
      within(configurationDialog()).getByRole("combobox", { name: "Esfuerzo" }),
    ).toHaveTextContent("RIR 0");
    expect(setSummaries()).toEqual(["3 × 10 repeticiones"]);
  });

  it("shows another exercise's stored values after one is cancelled", async () => {
    // Arrange
    await renderWorkout().rendered;
    await loaded();
    const user = userEvent.setup();
    await editExercise(user, 0);
    await chooseEffort(user, "RIR 5+");
    await user.click(within(configurationDialog()).getByRole("button", { name: "Cancelar" }));
    await dialogsClosed();

    // Act
    await editExercise(user, 1);

    // Assert
    expect(
      within(configurationDialog()).getByRole("combobox", { name: "Esfuerzo" }),
    ).toHaveTextContent("RIR 3");
  });

  it("disables Cancelar while the exercise is being saved", async () => {
    // Arrange
    await renderWorkout({ getWorkout: withoutExercises, addExercise: () => new Promise(() => {}) })
      .rendered;
    await loaded();
    const user = userEvent.setup();
    await chooseFromCatalog(user, "Sentadilla");
    await chooseEffort(user, "RIR 0");
    await addSet(user, { count: "3", value: "10" });

    // Act
    await user.click(within(configurationDialog()).getByRole("button", { name: "Guardar" }));

    // Assert
    expect(within(configurationDialog()).getByRole("button", { name: "Cancelar" })).toBeDisabled();
    expect(within(configurationDialog()).getByRole("button", { name: "Guardar" })).toBeDisabled();
  });

  it("keeps the dialog open on Escape while the exercise is being saved", async () => {
    // Arrange
    await renderWorkout({ getWorkout: withoutExercises, addExercise: () => new Promise(() => {}) })
      .rendered;
    await loaded();
    const user = userEvent.setup();
    await chooseFromCatalog(user, "Sentadilla");
    await chooseEffort(user, "RIR 0");
    await addSet(user, { count: "3", value: "10" });
    await user.click(within(configurationDialog()).getByRole("button", { name: "Guardar" }));

    // Act
    await user.keyboard("{Escape}");

    // Assert
    expect(configurationDialog()).toBeInTheDocument();
  });

  it("fills the properties card with the workout's data and no exercises", async () => {
    // Arrange & Act
    await renderWorkout().rendered;

    // Assert
    await loaded();
    const card = propertiesSection();
    expect(within(card).getByRole("textbox", { name: "Nombre" })).toHaveValue("Pierna");
    expect(
      within(within(card).getByRole("group", { name: "Semana" })).getByText(
        "Semana del 2 nov – 8 nov",
      ),
    ).toBeInTheDocument();
    expect(within(card).getByRole("textbox", { name: "Descripción" })).toHaveValue("Día 1");
    expect(within(card).queryByRole("list")).not.toBeInTheDocument();
  });

  it("shows the edit heading, a way back to the list and Guardar inside the properties card", async () => {
    // Arrange & Act
    await renderWorkout().rendered;

    // Assert
    await loaded();
    expect(
      screen.getByRole("heading", { level: 1, name: "Editar entrenamiento" }),
    ).toBeInTheDocument();
    expect(screen.queryByRole("heading", { level: 1, name: "Pierna" })).not.toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Volver" })).toHaveAttribute(
      "href",
      "/assigned-workouts",
    );
    expect(
      within(propertiesSection()).getByRole("button", { name: "Guardar" }),
    ).toBeInTheDocument();
  });

  it("places Guardar after the description", async () => {
    // Arrange & Act
    await renderWorkout().rendered;

    // Assert
    await loaded();
    const card = propertiesSection();
    const description = within(card).getByRole("textbox", { name: "Descripción" });
    const save = within(card).getByRole("button", { name: "Guardar" });
    expect(
      description.compareDocumentPosition(save) & Node.DOCUMENT_POSITION_FOLLOWING,
    ).toBeTruthy();
  });

  it("shows the Ejercicios title and Añadir ejercicio in the exercises card", async () => {
    // Arrange & Act
    await renderWorkout().rendered;

    // Assert
    await loaded();
    const card = exercisesSection();
    expect(within(card).getByRole("heading", { level: 2, name: "Ejercicios" })).toBeInTheDocument();
    expect(within(card).getByRole("button", { name: "Añadir ejercicio" })).toBeInTheDocument();
  });

  it("scrolls the catalog inside its dialog", async () => {
    // Arrange
    await renderWorkout({ getWorkout: withoutExercises }).rendered;
    await loaded();
    const user = userEvent.setup();

    // Act
    await openCatalog(user);

    // Assert
    expect(
      within(catalogDialog()).getByRole("list", { name: "Catálogo de ejercicios" }),
    ).toHaveStyle({ overflowY: "auto" });
  });

  it("lets the exercises card grow with its list instead of scrolling it", async () => {
    // Arrange & Act
    await renderWorkout().rendered;

    // Assert
    await loaded();
    const list = within(exercisesSection()).getByRole("list", { name: "Ejercicios" });
    expect(list).not.toHaveStyle({ maxHeight: "min(60vh, 640px)" });
    expect(list).not.toHaveStyle({ overflowY: "auto" });
  });

  it("fills body-part labels, outlines muscle labels in the same colour and leaves out a single muscle", async () => {
    // Arrange
    await renderWorkout({ getWorkout: withoutExercises, getExercises: muscleCatalog }).rendered;
    await loaded();
    const user = userEvent.setup();

    // Act
    await openCatalog(user);

    // Assert
    const [squatItem, , benchItem] = catalogItems();
    const chip = (item: HTMLElement, text: string) =>
      within(item).getByText(text).closest(".MuiChip-root");
    const benchLabels = within(benchItem!).getAllByText("Pecho");
    expect(benchLabels).toHaveLength(1);
    expect(benchLabels[0]!.closest(".MuiChip-root")).toHaveClass(
      "MuiChip-colorError",
      "MuiChip-filled",
    );
    expect(chip(squatItem!, "Piernas")).toHaveClass("MuiChip-colorSuccess", "MuiChip-filled");
    expect(chip(squatItem!, "Cuádriceps")).toHaveClass("MuiChip-colorSuccess", "MuiChip-outlined");
  });

  it("colours the labels of the workout's exercises and of the configuration the same way", async () => {
    // Arrange
    await renderWorkout({ getExercises: muscleCatalog }).rendered;
    await loaded();
    const user = userEvent.setup();
    const [squatItem] = workoutItems();
    expect(within(squatItem!).getByText("Piernas").closest(".MuiChip-root")).toHaveClass(
      "MuiChip-colorSuccess",
      "MuiChip-filled",
    );
    expect(within(squatItem!).getByText("Cuádriceps").closest(".MuiChip-root")).toHaveClass(
      "MuiChip-colorSuccess",
      "MuiChip-outlined",
    );

    // Act
    await editExercise(user, 0);

    // Assert
    const dialog = configurationDialog();
    expect(within(dialog).getByText("Piernas").closest(".MuiChip-root")).toHaveClass(
      "MuiChip-colorSuccess",
      "MuiChip-filled",
    );
    expect(within(dialog).getByText("Cuádriceps").closest(".MuiChip-root")).toHaveClass(
      "MuiChip-colorSuccess",
      "MuiChip-outlined",
    );
  });

  it("shows the body-part pills under their label and no muscle row before a body part is chosen", async () => {
    // Arrange
    await renderWorkout({ getWorkout: withoutExercises, getExercises: muscleCatalog }).rendered;
    await loaded();
    const user = userEvent.setup();

    // Act
    await openCatalog(user);

    // Assert
    const bodyParts = within(catalogDialog()).getByRole("group", { name: "Partes del cuerpo" });
    expect(within(bodyParts).getByText("Partes del cuerpo")).toBeInTheDocument();
    expect(
      within(bodyParts)
        .getAllByRole("button")
        .map((pill) => pill.textContent),
    ).toEqual(["Brazos", "Pecho", "Piernas"]);
    expect(
      within(catalogDialog()).queryByRole("group", { name: "Músculos" }),
    ).not.toBeInTheDocument();
    expect(within(catalogDialog()).queryByText("Músculos")).not.toBeInTheDocument();
  });

  it("shows no muscle row for a body part whose listed exercises work one muscle", async () => {
    // Arrange
    await renderWorkout({ getWorkout: withoutExercises, getExercises: muscleCatalog }).rendered;
    await loaded();
    const user = userEvent.setup();
    await openCatalog(user);

    // Act
    await user.click(
      within(within(catalogDialog()).getByRole("group", { name: "Partes del cuerpo" })).getByRole(
        "button",
        { name: "Pecho" },
      ),
    );

    // Assert
    expect(catalogNames()).toEqual(["Press de banca"]);
    expect(
      within(catalogDialog()).queryByRole("group", { name: "Músculos" }),
    ).not.toBeInTheDocument();
    expect(within(catalogDialog()).queryByText("Músculos")).not.toBeInTheDocument();
  });

  it("leaves out a single muscle in the workout list but keeps it in the configuration", async () => {
    // Arrange
    await renderWorkout().rendered;
    await loaded();
    const user = userEvent.setup();
    const benchItem = workoutItems()[1]!;
    expect(within(benchItem).getAllByText("Pecho")).toHaveLength(1);

    // Act
    await editExercise(user, 1);

    // Assert
    expect(within(configurationDialog()).getAllByText("Pecho")).toHaveLength(2);
  });

  it("shows the muscles of the selected body part under their label", async () => {
    // Arrange
    await renderWorkout({ getWorkout: withoutExercises, getExercises: muscleCatalog }).rendered;
    await loaded();
    const user = userEvent.setup();
    await openCatalog(user);

    // Act
    await user.click(
      within(within(catalogDialog()).getByRole("group", { name: "Partes del cuerpo" })).getByRole(
        "button",
        { name: "Brazos" },
      ),
    );

    // Assert
    const muscles = within(catalogDialog()).getByRole("group", { name: "Músculos" });
    expect(within(muscles).getByText("Músculos")).toBeInTheDocument();
    expect(
      within(muscles)
        .getAllByRole("button")
        .map((pill) => pill.textContent),
    ).toEqual(["Bíceps", "Tríceps"]);
  });

  it("fills a selected pill with its colour and outlines the others in theirs", async () => {
    // Arrange
    await renderWorkout({ getWorkout: withoutExercises, getExercises: muscleCatalog }).rendered;
    await loaded();
    const user = userEvent.setup();
    await openCatalog(user);
    const bodyParts = within(catalogDialog()).getByRole("group", { name: "Partes del cuerpo" });

    // Act
    await user.click(within(bodyParts).getByRole("button", { name: "Piernas" }));

    // Assert
    expect(within(bodyParts).getByRole("button", { name: "Piernas" })).toHaveClass(
      "MuiChip-colorSuccess",
      "MuiChip-filled",
    );
    expect(within(bodyParts).getByRole("button", { name: "Brazos" })).toHaveClass(
      "MuiChip-colorWarning",
      "MuiChip-outlined",
    );
    const muscles = within(catalogDialog()).getByRole("group", { name: "Músculos" });
    for (const pill of within(muscles).getAllByRole("button")) {
      expect(pill).toHaveClass("MuiChip-colorSuccess", "MuiChip-outlined");
    }
  });

  it("filters the catalog by the selected body part and muscle", async () => {
    // Arrange
    await renderWorkout({ getWorkout: withoutExercises, getExercises: muscleCatalog }).rendered;
    await loaded();
    const user = userEvent.setup();
    await openCatalog(user);
    await user.click(
      within(within(catalogDialog()).getByRole("group", { name: "Partes del cuerpo" })).getByRole(
        "button",
        { name: "Brazos" },
      ),
    );

    // Act
    await user.click(
      within(within(catalogDialog()).getByRole("group", { name: "Músculos" })).getByRole("button", {
        name: "Bíceps",
      }),
    );

    // Assert
    expect(catalogNames()).toEqual(["Curl de bíceps", "Curl martillo"]);
  });

  it("clears the muscle when another body part is selected", async () => {
    // Arrange
    await renderWorkout({ getWorkout: withoutExercises, getExercises: muscleCatalog }).rendered;
    await loaded();
    const user = userEvent.setup();
    await openCatalog(user);
    const bodyParts = within(catalogDialog()).getByRole("group", { name: "Partes del cuerpo" });
    await user.click(within(bodyParts).getByRole("button", { name: "Brazos" }));
    await user.click(
      within(within(catalogDialog()).getByRole("group", { name: "Músculos" })).getByRole("button", {
        name: "Bíceps",
      }),
    );

    // Act
    await user.click(within(bodyParts).getByRole("button", { name: "Piernas" }));

    // Assert
    const muscles = within(catalogDialog()).getByRole("group", { name: "Músculos" });
    expect(
      within(muscles)
        .getAllByRole("button")
        .map((pill) => pill.textContent),
    ).toEqual(["Cuádriceps", "Isquiotibiales"]);
    expect(within(muscles).queryByRole("button", { pressed: true })).not.toBeInTheDocument();
    expect(catalogNames()).toEqual(["Sentadilla", "Prensa de piernas", "Curl femoral"]);
  });

  it("clears the muscle and hides its row when the body part is cleared", async () => {
    // Arrange
    await renderWorkout({ getWorkout: withoutExercises, getExercises: muscleCatalog }).rendered;
    await loaded();
    const user = userEvent.setup();
    await openCatalog(user);
    const bodyParts = within(catalogDialog()).getByRole("group", { name: "Partes del cuerpo" });
    await user.click(within(bodyParts).getByRole("button", { name: "Brazos" }));
    await user.click(
      within(within(catalogDialog()).getByRole("group", { name: "Músculos" })).getByRole("button", {
        name: "Bíceps",
      }),
    );

    // Act
    await user.click(within(bodyParts).getByRole("button", { name: "Brazos" }));

    // Assert
    expect(
      within(catalogDialog()).queryByRole("group", { name: "Músculos" }),
    ).not.toBeInTheDocument();
    expect(catalogNames()).toHaveLength(7);
  });

  it("shows the message under a blank name and sends nothing", async () => {
    // Arrange
    const { mutations, rendered } = renderWorkout();
    await rendered;
    await loaded();
    const user = userEvent.setup();
    await user.clear(within(propertiesSection()).getByRole("textbox", { name: "Nombre" }));

    // Act
    await user.click(screen.getByRole("button", { name: "Guardar" }));

    // Assert
    expect(
      within(propertiesSection()).getByRole("textbox", { name: "Nombre" }),
    ).toHaveAccessibleDescription("Este campo es obligatorio.");
    expect(mutationCalls(mutations)).toBe(0);
  });

  it("shows the message under a too long description and sends nothing", async () => {
    // Arrange
    const { mutations, rendered } = renderWorkout();
    await rendered;
    await loaded();
    const user = userEvent.setup();
    const description = within(propertiesSection()).getByRole("textbox", { name: "Descripción" });
    await user.clear(description);
    await user.click(description);
    await user.paste("a".repeat(1025));

    // Act
    await user.click(screen.getByRole("button", { name: "Guardar" }));

    // Assert
    expect(description).toHaveAccessibleDescription("No puede superar los 1024 caracteres.");
    expect(mutationCalls(mutations)).toBe(0);
  });

  it("saves the trimmed data with the athlete and trainer as read, once", async () => {
    // Arrange
    const withTrainer = { ...workout, trainerId: "6a2d3f1e-0e6c-4b64-9d8f-1e7a8d2b3c4d" };
    const { mutations, rendered } = renderWorkout({ getWorkout: async () => ok(withTrainer) });
    await rendered;
    await loaded();
    const user = userEvent.setup();
    const name = within(propertiesSection()).getByRole("textbox", { name: "Nombre" });
    await user.clear(name);
    await user.type(name, "  Pierna B  ");

    // Act
    await user.click(screen.getByRole("button", { name: "Guardar" }));

    // Assert
    await waitFor(() => expect(mutations.update).toHaveBeenCalledTimes(1));
    expect(mutations.update).toHaveBeenCalledWith({
      id: "w-1",
      name: "Pierna B",
      // The Wednesday of the loaded week.
      workoutDate: "2026-11-04",
      description: "Día 1",
      userId: withTrainer.userId,
      trainerId: withTrainer.trainerId,
    });
    expect(mutations.create).not.toHaveBeenCalled();
    expect(mutations.addExercise).not.toHaveBeenCalled();
  });

  it("sends the Wednesday of the previous week after Semana anterior", async () => {
    // Arrange
    const { mutations, rendered } = renderWorkout();
    await rendered;
    await loaded();
    const user = userEvent.setup();
    await user.click(
      within(within(propertiesSection()).getByRole("group", { name: "Semana" })).getByRole(
        "button",
        { name: "Semana anterior" },
      ),
    );

    // Act
    await user.click(screen.getByRole("button", { name: "Guardar" }));

    // Assert
    await waitFor(() => expect(mutations.update).toHaveBeenCalledTimes(1));
    expect(mutations.update.mock.calls[0]?.[0]).toMatchObject({ workoutDate: "2026-10-28" });
  });

  it("sends no description when it is left blank", async () => {
    // Arrange
    const { mutations, rendered } = renderWorkout();
    await rendered;
    await loaded();
    const user = userEvent.setup();
    await user.clear(within(propertiesSection()).getByRole("textbox", { name: "Descripción" }));

    // Act
    await user.click(screen.getByRole("button", { name: "Guardar" }));

    // Assert
    await waitFor(() => expect(mutations.update).toHaveBeenCalledTimes(1));
    expect(mutations.update.mock.calls[0]?.[0]).not.toHaveProperty("description");
  });

  it("disables Guardar while the workout is being saved", async () => {
    // Arrange
    const { rendered } = renderWorkout({ updateWorkout: () => new Promise(() => {}) });
    await rendered;
    await loaded();
    const user = userEvent.setup();

    // Act
    await user.click(screen.getByRole("button", { name: "Guardar" }));

    // Assert
    expect(await screen.findByRole("button", { name: "Guardar" })).toBeDisabled();
  });

  it("confirms the save and keeps the saved name in its field", async () => {
    // Arrange
    await renderWorkout().rendered;
    await loaded();
    const user = userEvent.setup();
    const name = within(propertiesSection()).getByRole("textbox", { name: "Nombre" });
    await user.clear(name);
    await user.type(name, "Pierna B");

    // Act
    await user.click(screen.getByRole("button", { name: "Guardar" }));

    // Assert
    expect(await screen.findByText("Entrenamiento guardado.")).toBeInTheDocument();
    expect(within(propertiesSection()).getByRole("textbox", { name: "Nombre" })).toHaveValue(
      "Pierna B",
    );
  });

  it("hides the confirmation when a field changes again", async () => {
    // Arrange
    await renderWorkout().rendered;
    await loaded();
    const user = userEvent.setup();
    await user.click(screen.getByRole("button", { name: "Guardar" }));
    await screen.findByText("Entrenamiento guardado.");

    // Act
    await user.type(within(propertiesSection()).getByRole("textbox", { name: "Nombre" }), "!");

    // Assert
    expect(screen.queryByText("Entrenamiento guardado.")).not.toBeInTheDocument();
  });

  it("shows the error and keeps the values when the API refuses", async () => {
    // Arrange
    const { rendered } = renderWorkout({
      updateWorkout: async () => fail(new DomainError("FORBIDDEN", "")),
    });
    await rendered;
    await loaded();
    const user = userEvent.setup();
    const name = within(propertiesSection()).getByRole("textbox", { name: "Nombre" });
    await user.clear(name);
    await user.type(name, "Pierna B");

    // Act
    await user.click(screen.getByRole("button", { name: "Guardar" }));

    // Assert
    expect(
      await screen.findByText("No tienes permiso para realizar esta acción."),
    ).toBeInTheDocument();
    expect(name).toHaveValue("Pierna B");
    expect(screen.queryByText("Entrenamiento guardado.")).not.toBeInTheDocument();
  });

  it("shows each workout exercise's sets comma separated and its effort, on their own lines", async () => {
    // Arrange
    const sets: WorkoutExerciseDetail["sets"] = [
      { count: 2, target: { type: "Reps", value: 15, maxValue: null } },
      { count: 1, target: { type: "RepsRange", value: 10, maxValue: 12 } },
      { count: 1, target: { type: "MaxReps", value: null, maxValue: null } },
    ];
    const getWorkout = async () =>
      ok({ ...workout, exercises: [workoutExercise("we-1", squat.id, "HIGH", sets)] });

    // Act
    await renderWorkout({ getWorkout }).rendered;

    // Assert
    await loaded();
    const [item] = workoutItems();
    const setsLine = within(item!).getByText("2 × 15, 1 × 10–12, 1 × máx");
    const effortLine = within(item!).getByText("Esfuerzo: RIR 0");
    expect(setsLine).not.toBe(effortLine);
    expect(
      setsLine.compareDocumentPosition(effortLine) & Node.DOCUMENT_POSITION_FOLLOWING,
    ).toBeTruthy();
  });

  it("closes the dialog and lists an added exercise last, as the API answered it", async () => {
    // Arrange
    const withTwo = { ...workout, exercises: [squatInWorkout, benchInWorkout] };
    await renderWorkout({ getWorkout: async () => ok(withTwo) }).rendered;
    await loaded();
    const user = userEvent.setup();
    await chooseFromCatalog(user, "Prensa de piernas");
    await chooseEffort(user, "RIR 3");
    await addSet(user, { count: "3", value: "10" });

    // Act
    await user.click(within(configurationDialog()).getByRole("button", { name: "Guardar" }));

    // Assert
    await dialogsClosed();
    expect(workoutRows()).toEqual([
      ["1", "Sentadilla"],
      ["2", "Press de banca"],
      ["3", "Prensa de piernas"],
    ]);
    const item = workoutItems()[2]!;
    expect(within(item).getByText("3 × 10", { exact: true })).toBeInTheDocument();
    expect(within(item).getByText("Esfuerzo: RIR 3")).toBeInTheDocument();
  });

  it("closes the dialog and updates an edited exercise as the API answered it", async () => {
    // Arrange
    await renderWorkout().rendered;
    await loaded();
    const user = userEvent.setup();
    await editExercise(user, 0);
    await addSet(user, { count: "2", value: "15" });

    // Act
    await user.click(within(configurationDialog()).getByRole("button", { name: "Guardar" }));

    // Assert
    await dialogsClosed();
    const [item] = workoutItems();
    expect(within(item!).getByText("3 × 10, 2 × 15")).toBeInTheDocument();
  });

  it("shows each listed exercise in its own outlined card", async () => {
    // Arrange & Act
    await renderWorkout().rendered;

    // Assert
    await loaded();
    const items = workoutItems();
    expect(items).toHaveLength(3);
    for (const item of items) {
      expect(item.querySelectorAll(".MuiCard-root.MuiPaper-outlined")).toHaveLength(1);
    }
  });

  it("puts the handle, position, name and actions first, then the labels, then the sets", async () => {
    // Arrange & Act
    await renderWorkout({ getExercises: muscleCatalog }).rendered;

    // Assert
    await loaded();
    const [item] = workoutItems();
    const inOrder = [
      within(item!).getByRole("button", { name: "Reordenar ejercicio" }),
      within(item!).getByText("1", { exact: true }),
      within(item!).getByText("Sentadilla"),
      within(item!).getByRole("button", { name: "Quitar ejercicio" }),
      within(item!).getByRole("button", { name: "Editar ejercicio" }),
      within(item!).getByText("Piernas"),
      within(item!).getByText("Cuádriceps"),
      within(item!).getByText("3 × 10", { exact: true }),
    ];
    for (const [index, node] of inOrder.slice(1).entries()) {
      expect(
        inOrder[index]!.compareDocumentPosition(node) & Node.DOCUMENT_POSITION_FOLLOWING,
      ).toBeTruthy();
    }
    const firstRow = within(item!).getByText("Sentadilla").closest("p")!.parentElement!
      .parentElement!;
    expect(firstRow).toContainElement(
      within(item!).getByRole("button", { name: "Editar ejercicio" }),
    );
    expect(firstRow).not.toContainElement(within(item!).getByText("Piernas"));
  });

  it("lists the workout's exercises in order with their actions", async () => {
    // Arrange & Act
    await renderWorkout().rendered;

    // Assert
    await loaded();
    expect(workoutRows()).toEqual([
      ["1", "Sentadilla"],
      ["2", "Press de banca"],
      ["3", "Prensa de piernas"],
    ]);
    for (const item of workoutItems()) {
      expect(within(item).getByRole("button", { name: "Reordenar ejercicio" })).toBeEnabled();
      expect(within(item).getByRole("button", { name: "Quitar ejercicio" })).toBeInTheDocument();
      expect(within(item).getByRole("button", { name: "Editar ejercicio" })).toBeInTheDocument();
    }
  });

  describe("reordering", () => {
    let rects: ReturnType<typeof stubListItemRects>;

    beforeEach(() => {
      rects = stubListItemRects();
    });

    afterEach(() => {
      rects.mockRestore();
    });

    it("moves a dropped exercise at once and sends the full saved order, once", async () => {
      // Arrange
      const { mutations, rendered } = renderWorkout({
        reorderExercises: () => new Promise(() => {}),
      });
      await rendered;
      await loaded();
      const user = userEvent.setup();

      // Act
      await moveRight(user, 0, 1);

      // Assert
      expect(workoutRows()).toEqual([
        ["1", "Press de banca"],
        ["2", "Sentadilla"],
        ["3", "Prensa de piernas"],
      ]);
      expect(mutations.reorderExercises).toHaveBeenCalledTimes(1);
      expect(mutations.reorderExercises).toHaveBeenCalledWith({
        workoutId: "w-1",
        exerciseIds: ["ex-bench", "ex-squat", "ex-press"],
      });
    });

    it("keeps the new order and shows no error when the API accepts it", async () => {
      // Arrange
      await renderWorkout().rendered;
      await loaded();
      const user = userEvent.setup();

      // Act
      await moveRight(user, 0, 2);

      // Assert
      await waitFor(() =>
        expect(
          workoutButtons("Reordenar ejercicio").every((handle) => !handle.hasAttribute("disabled")),
        ).toBe(true),
      );
      expect(workoutRows()).toEqual([
        ["1", "Press de banca"],
        ["2", "Prensa de piernas"],
        ["3", "Sentadilla"],
      ]);
      expect(within(exercisesSection()).queryByRole("alert")).not.toBeInTheDocument();
    });

    it("puts the previous order back and shows the error when the API refuses it", async () => {
      // Arrange
      await renderWorkout({ reorderExercises: async () => fail(new DomainError("CONFLICT", "")) })
        .rendered;
      await loaded();
      const user = userEvent.setup();

      // Act
      await moveRight(user, 0, 1);

      // Assert
      expect(
        await within(exercisesSection()).findByText(
          "La operación entra en conflicto con el estado actual.",
        ),
      ).toBeInTheDocument();
      expect(workoutRows()).toEqual([
        ["1", "Sentadilla"],
        ["2", "Press de banca"],
        ["3", "Prensa de piernas"],
      ]);
    });

    it("sends nothing when the exercise is dropped where it was", async () => {
      // Arrange
      const { mutations, rendered } = renderWorkout();
      await rendered;
      await loaded();
      const user = userEvent.setup();

      // Act
      await moveRight(user, 0, 0);

      // Assert
      expect(workoutRows().map(([, name]) => name)).toEqual([
        "Sentadilla",
        "Press de banca",
        "Prensa de piernas",
      ]);
      expect(mutationCalls(mutations)).toBe(0);
    });

    it("sends nothing when the drag is cancelled with Escape", async () => {
      // Arrange
      const { mutations, rendered } = renderWorkout();
      await rendered;
      await loaded();
      const user = userEvent.setup();
      workoutButtons("Reordenar ejercicio")[0]!.focus();
      await user.keyboard("[Space]");
      await user.keyboard("[ArrowRight]");

      // Act
      await user.keyboard("[Escape]");

      // Assert
      expect(workoutRows().map(([, name]) => name)).toEqual([
        "Sentadilla",
        "Press de banca",
        "Prensa de piernas",
      ]);
      expect(mutationCalls(mutations)).toBe(0);
    });

    it("disables every handle while the reorder is in progress", async () => {
      // Arrange
      await renderWorkout({ reorderExercises: () => new Promise(() => {}) }).rendered;
      await loaded();
      const user = userEvent.setup();

      // Act
      await moveRight(user, 0, 1);

      // Assert
      for (const handle of workoutButtons("Reordenar ejercicio")) {
        expect(handle).toBeDisabled();
      }
    });
  });

  describe("superseries", () => {
    it("shows a checkbox only on the exercises outside a superseries", async () => {
      // Arrange
      await renderWorkout({ getWorkout: withGroup }).rendered;

      // Act
      await loaded();

      // Assert
      expect(selectionBox("Sentadilla")).toBeInTheDocument();
      expect(selectionBox("Press de banca")).not.toBeInTheDocument();
      expect(selectionBox("Prensa de piernas")).not.toBeInTheDocument();
    });

    it("treats an exercise whose superseries has no other member as outside one", async () => {
      // Arrange
      const lone: WorkoutById = {
        ...workout,
        exercises: [{ ...squatInWorkout, superSeriesId: 5 }, benchInWorkout, pressInWorkout],
      };
      await renderWorkout({ getWorkout: async () => ok(lone) }).rendered;

      // Act
      await loaded();

      // Assert
      expect(selectionBox("Sentadilla")).toBeInTheDocument();
      expect(
        within(exercisesSection()).queryByRole("button", { name: "Romper superserie" }),
      ).not.toBeInTheDocument();
    });

    it("hides Agrupar en superserie with a single exercise checked", async () => {
      // Arrange
      await renderWorkout().rendered;
      await loaded();
      const user = userEvent.setup();

      // Act
      await check(user, "Sentadilla");

      // Assert
      expect(groupButton()).not.toBeInTheDocument();
    });

    it("shows Agrupar en superserie with two exercises checked", async () => {
      // Arrange
      await renderWorkout().rendered;
      await loaded();
      const user = userEvent.setup();

      // Act
      await check(user, "Sentadilla", "Press de banca");

      // Assert
      expect(groupButton()).toBeInTheDocument();
    });

    it("hides Agrupar en superserie again when one of two is unchecked", async () => {
      // Arrange
      await renderWorkout().rendered;
      await loaded();
      const user = userEvent.setup();
      await check(user, "Sentadilla", "Press de banca");

      // Act
      await check(user, "Press de banca");

      // Assert
      expect(groupButton()).not.toBeInTheDocument();
    });

    it("groups the checked exercises and brings them together, sending the new order once", async () => {
      // Arrange
      const { mutations, rendered } = renderWorkout();
      await rendered;
      await loaded();
      const user = userEvent.setup();
      await check(user, "Sentadilla", "Prensa de piernas");

      // Act
      await user.click(groupButton()!);

      // Assert
      await waitFor(() => expect(mutations.reorderExercises).toHaveBeenCalledTimes(1));
      expect(mutations.createSuperSeries).toHaveBeenCalledTimes(1);
      expect(mutations.createSuperSeries).toHaveBeenCalledWith({
        workoutId: "w-1",
        exerciseIds: ["we-1", "we-3"],
      });
      expect(mutations.reorderExercises).toHaveBeenCalledWith({
        workoutId: "w-1",
        exerciseIds: ["ex-squat", "ex-press", "ex-bench"],
      });
      expect(exerciseRows()).toEqual([
        ["1", "Sentadilla"],
        ["2", "Prensa de piernas"],
        ["3", "Press de banca"],
      ]);
      expect(
        within(exercisesSection()).getByRole("list", {
          name: "superserie Sentadilla, Prensa de piernas",
        }),
      ).toBeInTheDocument();
      expect(groupButton()).not.toBeInTheDocument();
      expect(selectionBox("Press de banca")).not.toBeChecked();
    });

    it("sends no reorder when the grouped exercises are already together", async () => {
      // Arrange
      const { mutations, rendered } = renderWorkout();
      await rendered;
      await loaded();
      const user = userEvent.setup();
      await check(user, "Sentadilla", "Press de banca");

      // Act
      await user.click(groupButton()!);

      // Assert
      expect(
        await within(exercisesSection()).findByRole("button", { name: "Romper superserie" }),
      ).toBeInTheDocument();
      expect(mutations.createSuperSeries).toHaveBeenCalledTimes(1);
      expect(mutations.reorderExercises).not.toHaveBeenCalled();
      expect(exerciseRows().map(([, name]) => name)).toEqual([
        "Sentadilla",
        "Press de banca",
        "Prensa de piernas",
      ]);
    });

    it("keeps the list and the checked exercises when the API refuses the grouping", async () => {
      // Arrange
      const { mutations, rendered } = renderWorkout({
        createSuperSeries: async () => fail(new DomainError("VALIDATION", "")),
      });
      await rendered;
      await loaded();
      const user = userEvent.setup();
      await check(user, "Sentadilla", "Prensa de piernas");

      // Act
      await user.click(groupButton()!);

      // Assert
      expect(
        await within(exercisesSection()).findByText(
          "Solo se pueden agrupar en superserie ejercicios con el mismo número total de series.",
        ),
      ).toBeInTheDocument();
      expect(mutations.reorderExercises).not.toHaveBeenCalled();
      expect(selectionBox("Sentadilla")).toBeChecked();
      expect(selectionBox("Prensa de piernas")).toBeChecked();
      expect(exerciseRows().map(([, name]) => name)).toEqual([
        "Sentadilla",
        "Press de banca",
        "Prensa de piernas",
      ]);
    });

    it("keeps the grouped exercises in their previous places when the reorder is refused", async () => {
      // Arrange
      await renderWorkout({ reorderExercises: async () => fail(new DomainError("CONFLICT", "")) })
        .rendered;
      await loaded();
      const user = userEvent.setup();
      await check(user, "Sentadilla", "Prensa de piernas");

      // Act
      await user.click(groupButton()!);

      // Assert
      expect(
        await within(exercisesSection()).findByText(
          "La operación entra en conflicto con el estado actual.",
        ),
      ).toBeInTheDocument();
      expect(exerciseRows()).toEqual([
        ["1", "Sentadilla"],
        ["2", "Press de banca"],
        ["3", "Prensa de piernas"],
      ]);
      expect(selectionBox("Sentadilla")).not.toBeInTheDocument();
      expect(selectionBox("Prensa de piernas")).not.toBeInTheDocument();
      expect(selectionBox("Press de banca")).toBeInTheDocument();
    });

    it("shows the members of a superseries inside one group card with a single handle", async () => {
      // Arrange
      await renderWorkout({ getWorkout: withGroup }).rendered;

      // Act
      await loaded();

      // Assert
      const group = within(exercisesSection()).getByRole("list", {
        name: "superserie Press de banca, Prensa de piernas",
      });
      expect(within(group).getAllByRole("listitem")).toHaveLength(2);
      expect(within(exercisesSection()).getByText("Superserie")).toBeInTheDocument();
      expect(within(group).queryByRole("button", { name: "Reordenar ejercicio" })).toBeNull();
      expect(workoutButtons("Romper superserie")).toHaveLength(1);
      expect(workoutButtons("Reordenar superserie")).toHaveLength(1);
      expect(workoutButtons("Reordenar ejercicio")).toHaveLength(1);
      expect(exerciseRows()).toEqual([
        ["1", "Sentadilla"],
        ["2", "Press de banca"],
        ["3", "Prensa de piernas"],
      ]);
    });

    it("shows a group card per run when the members of a superseries are apart", async () => {
      // Arrange
      const apart: WorkoutById = {
        ...workout,
        exercises: [
          { ...squatInWorkout, superSeriesId: 1 },
          benchInWorkout,
          { ...pressInWorkout, superSeriesId: 1 },
        ],
      };
      await renderWorkout({ getWorkout: async () => ok(apart) }).rendered;

      // Act
      await loaded();

      // Assert
      expect(workoutButtons("Romper superserie")).toHaveLength(2);
      expect(selectionBox("Press de banca")).toBeInTheDocument();
    });

    it("asks for confirmation before breaking a superseries", async () => {
      // Arrange
      const { mutations, rendered } = renderWorkout({ getWorkout: withGroup });
      await rendered;
      await loaded();
      const user = userEvent.setup();

      // Act
      await user.click(workoutButtons("Romper superserie")[0]!);

      // Assert
      expect(await screen.findByRole("dialog", { name: "¿Romper superserie?" })).toBeVisible();
      expect(mutationCalls(mutations)).toBe(0);
    });

    it("closes the confirmation without a request on Cancelar", async () => {
      // Arrange
      const { mutations, rendered } = renderWorkout({ getWorkout: withGroup });
      await rendered;
      await loaded();
      const user = userEvent.setup();
      await user.click(workoutButtons("Romper superserie")[0]!);
      await screen.findByRole("dialog", { name: "¿Romper superserie?" });

      // Act
      await user.click(within(breakDialog()!).getByRole("button", { name: "Cancelar" }));

      // Assert
      await dialogsClosed();
      expect(mutationCalls(mutations)).toBe(0);
      expect(workoutButtons("Romper superserie")).toHaveLength(1);
    });

    it("breaks the superseries on confirmation and gives its members their checkbox back", async () => {
      // Arrange
      const { mutations, rendered } = renderWorkout({ getWorkout: withGroup });
      await rendered;
      await loaded();
      const user = userEvent.setup();
      await user.click(workoutButtons("Romper superserie")[0]!);
      await screen.findByRole("dialog", { name: "¿Romper superserie?" });

      // Act
      await user.click(within(breakDialog()!).getByRole("button", { name: "Romper" }));

      // Assert
      await dialogsClosed();
      expect(mutations.breakSuperSeries).toHaveBeenCalledTimes(1);
      expect(mutations.breakSuperSeries).toHaveBeenCalledWith({
        workoutId: "w-1",
        superSeriesId: 1,
      });
      expect(
        within(exercisesSection()).queryByRole("button", { name: "Romper superserie" }),
      ).not.toBeInTheDocument();
      expect(selectionBox("Press de banca")).toBeInTheDocument();
      expect(selectionBox("Prensa de piernas")).toBeInTheDocument();
      expect(exerciseRows()).toEqual([
        ["1", "Sentadilla"],
        ["2", "Press de banca"],
        ["3", "Prensa de piernas"],
      ]);
    });

    it("keeps the group card and shows the error when the API refuses the break", async () => {
      // Arrange
      await renderWorkout({
        getWorkout: withGroup,
        breakSuperSeries: async () => fail(new DomainError("NOT_FOUND", "")),
      }).rendered;
      await loaded();
      const user = userEvent.setup();
      await user.click(workoutButtons("Romper superserie")[0]!);
      await screen.findByRole("dialog", { name: "¿Romper superserie?" });

      // Act
      await user.click(within(breakDialog()!).getByRole("button", { name: "Romper" }));

      // Assert
      await dialogsClosed();
      expect(
        within(exercisesSection()).getByText("No hemos encontrado lo que buscas."),
      ).toBeInTheDocument();
      expect(workoutButtons("Romper superserie")).toHaveLength(1);
    });

    it("keeps the generic message when the API refuses a break as invalid", async () => {
      // Arrange
      await renderWorkout({
        getWorkout: withGroup,
        breakSuperSeries: async () => fail(new DomainError("VALIDATION", "")),
      }).rendered;
      await loaded();
      const user = userEvent.setup();
      await user.click(workoutButtons("Romper superserie")[0]!);
      await screen.findByRole("dialog", { name: "¿Romper superserie?" });

      // Act
      await user.click(within(breakDialog()!).getByRole("button", { name: "Romper" }));

      // Assert
      await dialogsClosed();
      expect(
        await within(exercisesSection()).findByText("Revisa los datos introducidos."),
      ).toBeInTheDocument();
    });

    it("disables Agrupar en superserie while the grouping is in progress", async () => {
      // Arrange
      await renderWorkout({ createSuperSeries: () => new Promise(() => {}) }).rendered;
      await loaded();
      const user = userEvent.setup();
      await check(user, "Sentadilla", "Press de banca");

      // Act
      await user.click(groupButton()!);

      // Assert
      expect(groupButton()).toBeDisabled();
    });

    it("disables the confirmation while the break is in progress", async () => {
      // Arrange
      await renderWorkout({
        getWorkout: withGroup,
        breakSuperSeries: () => new Promise(() => {}),
      }).rendered;
      await loaded();
      const user = userEvent.setup();
      await user.click(workoutButtons("Romper superserie")[0]!);
      await screen.findByRole("dialog", { name: "¿Romper superserie?" });

      // Act
      await user.click(within(breakDialog()!).getByRole("button", { name: "Romper" }));

      // Assert
      expect(within(breakDialog()!).getByRole("button", { name: "Romper" })).toBeDisabled();
    });

    describe("dragging a group", () => {
      let rects: ReturnType<typeof stubListItemRects>;

      beforeEach(() => {
        rects = stubListItemRects();
      });

      afterEach(() => {
        rects.mockRestore();
      });

      it("moves every member of the group together and sends the full order once", async () => {
        // Arrange
        const { mutations, rendered } = renderWorkout({
          getWorkout: withGroup,
          reorderExercises: () => new Promise(() => {}),
        });
        await rendered;
        await loaded();
        const user = userEvent.setup();
        workoutButtons("Reordenar superserie")[0]!.focus();
        await user.keyboard("[Space]");
        await user.keyboard("[ArrowLeft]");

        // Act
        await user.keyboard("[Space]");

        // Assert
        expect(exerciseRows()).toEqual([
          ["1", "Press de banca"],
          ["2", "Prensa de piernas"],
          ["3", "Sentadilla"],
        ]);
        expect(mutations.reorderExercises).toHaveBeenCalledTimes(1);
        expect(mutations.reorderExercises).toHaveBeenCalledWith({
          workoutId: "w-1",
          exerciseIds: ["ex-bench", "ex-press", "ex-squat"],
        });
      });
    });
  });

  it("removes an exercise through the API", async () => {
    // Arrange
    const { mutations, rendered } = renderWorkout({
      getWorkout: async () => ok({ ...workout, exercises: [squatInWorkout] }),
    });
    await rendered;
    await loaded();
    const user = userEvent.setup();

    // Act
    await user.click(workoutButtons("Quitar ejercicio")[0]!);

    // Assert
    await waitFor(() => expect(workoutItems()).toHaveLength(0));
    expect(mutations.removeExercise).toHaveBeenCalledTimes(1);
    expect(mutations.removeExercise).toHaveBeenCalledWith({
      workoutId: "w-1",
      exerciseId: "ex-squat",
    });
  });

  it("lists a removed exercise in the catalog again", async () => {
    // Arrange
    await renderWorkout({ getWorkout: async () => ok({ ...workout, exercises: [squatInWorkout] }) })
      .rendered;
    await loaded();
    const user = userEvent.setup();
    await user.click(workoutButtons("Quitar ejercicio")[0]!);
    await waitFor(() => expect(workoutItems()).toHaveLength(0));

    // Act
    await openCatalog(user);

    // Assert
    expect(catalogNames()).toEqual(["Sentadilla", "Prensa de piernas", "Press de banca"]);
  });

  it("disables Quitar ejercicio while the removal is in progress", async () => {
    // Arrange
    await renderWorkout({ removeExercise: () => new Promise(() => {}) }).rendered;
    await loaded();
    const user = userEvent.setup();
    const [squatItem] = workoutItems();

    // Act
    await user.click(within(squatItem!).getByRole("button", { name: "Quitar ejercicio" }));

    // Assert
    expect(within(squatItem!).getByRole("button", { name: "Quitar ejercicio" })).toBeDisabled();
  });

  it("shows the error and keeps the exercise when the API refuses the removal", async () => {
    // Arrange
    await renderWorkout({ removeExercise: async () => fail(new DomainError("CONFLICT", "")) })
      .rendered;
    await loaded();
    const user = userEvent.setup();

    // Act
    await user.click(workoutButtons("Quitar ejercicio")[0]!);

    // Assert
    expect(
      await within(exercisesSection()).findByText(
        "La operación entra en conflicto con el estado actual.",
      ),
    ).toBeInTheDocument();
    expect(workoutItems()).toHaveLength(3);
  });

  it("says so when the workout has no exercises", async () => {
    // Arrange & Act
    await renderWorkout({ getWorkout: async () => ok({ ...workout, exercises: [] }) }).rendered;

    // Assert
    expect(
      await screen.findByText("Este entrenamiento aún no tiene ejercicios."),
    ).toBeInTheDocument();
  });

  it("opens a listed exercise's configuration with the pencil, without a request", async () => {
    // Arrange
    const { mutations, rendered } = renderWorkout();
    await rendered;
    await loaded();
    const user = userEvent.setup();

    // Act
    await user.click(workoutButtons("Editar ejercicio")[1]!);

    // Assert
    const dialog = await screen.findByRole("dialog", { name: "Configuración del ejercicio" });
    expect(within(dialog).getByRole("heading", { name: "Press de banca" })).toBeInTheDocument();
    expect(within(dialog).getAllByText("Pecho")).toHaveLength(2);
    expect(within(dialog).getByRole("combobox", { name: "Esfuerzo" })).toHaveTextContent("RIR 3");
    expect(setSummaries()).toEqual(["4 × 8–12 repeticiones"]);
    expect(within(dialog).getByRole("button", { name: "Cancelar" })).toBeEnabled();
    expect(within(dialog).getByRole("button", { name: "Guardar" })).toBeEnabled();
    expect(mutationCalls(mutations)).toBe(0);
  });

  it("shows a squat's name, labels, effort and sets in the dialog", async () => {
    // Arrange
    await renderWorkout().rendered;
    await loaded();
    const user = userEvent.setup();

    // Act
    await editExercise(user, 0);

    // Assert
    const dialog = configurationDialog();
    expect(within(dialog).getByRole("heading", { name: "Sentadilla" })).toBeInTheDocument();
    expect(within(dialog).getByText("Piernas")).toBeInTheDocument();
    expect(within(dialog).getByText("Cuádriceps")).toBeInTheDocument();
    expect(within(dialog).getByRole("combobox", { name: "Esfuerzo" })).toHaveTextContent("RIR 0");
    expect(setSummaries()).toEqual(["3 × 10 repeticiones"]);
  });

  it("lists several sets in order with a summary each", async () => {
    // Arrange
    const sets: WorkoutExerciseDetail["sets"] = [
      { count: 2, target: { type: "Reps", value: 15, maxValue: null } },
      { count: 1, target: { type: "RepsRange", value: 10, maxValue: 12 } },
      { count: 3, target: { type: "Duration", value: 40, maxValue: null } },
      { count: 1, target: { type: "MaxReps", value: null, maxValue: null } },
    ];
    const getWorkout = async () =>
      ok({ ...workout, exercises: [workoutExercise("we-1", squat.id, "HIGH", sets)] });
    await renderWorkout({ getWorkout }).rendered;
    await loaded();
    const user = userEvent.setup();

    // Act
    await editExercise(user, 0);

    // Assert
    expect(setSummaries()).toEqual([
      "2 × 15 repeticiones",
      "1 × 10–12 repeticiones",
      "3 × 40 s",
      "1 × máximo",
    ]);
  });

  it("removes a set from the list without a request", async () => {
    // Arrange
    const { mutations, rendered } = renderWorkout();
    await rendered;
    await loaded();
    const user = userEvent.setup();
    await editExercise(user, 0);
    await addSet(user, { count: "1", type: "Máximas repeticiones" });

    // Act
    await user.click(
      within(configurationDialog()).getAllByRole("button", { name: "Quitar serie" })[0]!,
    );

    // Assert
    expect(setSummaries()).toEqual(["1 × máximo"]);
    expect(mutationCalls(mutations)).toBe(0);
  });

  it("adds a set from the form to the end of the list and empties the form", async () => {
    // Arrange
    const { mutations, rendered } = renderWorkout();
    await rendered;
    await loaded();
    const user = userEvent.setup();
    await editExercise(user, 0);

    // Act
    await addSet(user, { count: "2", value: "15" });

    // Assert
    expect(setSummaries()).toEqual(["3 × 10 repeticiones", "2 × 15 repeticiones"]);
    const form = within(configurationDialog()).getByRole("group", { name: "Añadir serie" });
    expect(within(form).getByRole("spinbutton", { name: "Series" })).toHaveValue(null);
    expect(within(form).getByRole("spinbutton", { name: "Valor" })).toHaveValue(null);
    expect(mutationCalls(mutations)).toBe(0);
  });

  it("asks for no value when the set is max reps", async () => {
    // Arrange
    await renderWorkout().rendered;
    await loaded();
    const user = userEvent.setup();
    await editExercise(user, 0);
    const form = within(configurationDialog()).getByRole("group", { name: "Añadir serie" });

    // Act
    await user.click(within(form).getByRole("combobox", { name: "Tipo" }));
    await user.click(screen.getByRole("option", { name: "Máximas repeticiones" }));

    // Assert
    expect(within(form).queryByRole("spinbutton", { name: "Valor" })).not.toBeInTheDocument();
    expect(within(form).queryByRole("spinbutton", { name: "Máximo" })).not.toBeInTheDocument();
  });

  it("reports an invalid set and keeps the list unchanged", async () => {
    // Arrange
    await renderWorkout().rendered;
    await loaded();
    const user = userEvent.setup();
    await editExercise(user, 0);
    const form = within(configurationDialog()).getByRole("group", { name: "Añadir serie" });
    await user.type(within(form).getByRole("spinbutton", { name: "Series" }), "0");

    // Act
    await user.click(within(configurationDialog()).getByRole("button", { name: "Añadir serie" }));

    // Assert
    expect(within(form).getByRole("spinbutton", { name: "Series" })).toHaveAccessibleDescription(
      "Introduce un número entero mayor que 0.",
    );
    expect(within(form).getByRole("spinbutton", { name: "Valor" })).toHaveAccessibleDescription(
      "Introduce un número entero mayor que 0.",
    );
    expect(setSummaries()).toEqual(["3 × 10 repeticiones"]);
  });

  it("asks for an effort and a set before saving a chosen exercise", async () => {
    // Arrange
    const { mutations, rendered } = renderWorkout({ getWorkout: withoutExercises });
    await rendered;
    await loaded();
    const user = userEvent.setup();
    await chooseFromCatalog(user, "Sentadilla");

    // Act
    await user.click(within(configurationDialog()).getByRole("button", { name: "Guardar" }));

    // Assert
    expect(within(configurationDialog()).getByText("Elige un esfuerzo.")).toBeInTheDocument();
    expect(
      within(configurationDialog()).getByText("Añade al menos una serie."),
    ).toBeInTheDocument();
    expect(mutationCalls(mutations)).toBe(0);
  });

  it("adds a chosen exercise to the workout with its effort and sets, once", async () => {
    // Arrange
    const { mutations, rendered } = renderWorkout({ getWorkout: withoutExercises });
    await rendered;
    await loaded();
    const user = userEvent.setup();
    await chooseFromCatalog(user, "Prensa de piernas");
    await chooseEffort(user, "RIR 0");
    await addSet(user, { count: "2", value: "15" });
    await addSet(user, { count: "1", type: "Máximas repeticiones" });

    // Act
    await user.click(within(configurationDialog()).getByRole("button", { name: "Guardar" }));

    // Assert
    await waitFor(() => expect(mutations.addExercise).toHaveBeenCalledTimes(1));
    expect(mutations.addExercise).toHaveBeenCalledWith({
      workoutId: "w-1",
      exerciseId: "ex-press",
      effort: "HIGH",
      sets: [
        { count: 2, targetType: "Reps", target: 15 },
        { count: 1, targetType: "MaxReps" },
      ],
      perSide: false,
    });
    expect(mutations.updateExercise).not.toHaveBeenCalled();
  });

  it("replaces a listed exercise's effort and sets, once", async () => {
    // Arrange
    const { mutations, rendered } = renderWorkout();
    await rendered;
    await loaded();
    const user = userEvent.setup();
    await editExercise(user, 0);
    await addSet(user, { count: "1", type: "Rango de repeticiones", value: "8", max: "12" });
    await user.click(
      within(configurationDialog()).getAllByRole("button", { name: "Quitar serie" })[0]!,
    );

    // Act
    await user.click(within(configurationDialog()).getByRole("button", { name: "Guardar" }));

    // Assert
    await waitFor(() => expect(mutations.updateExercise).toHaveBeenCalledTimes(1));
    expect(mutations.updateExercise).toHaveBeenCalledWith({
      workoutId: "w-1",
      exerciseId: "ex-squat",
      effort: "HIGH",
      sets: [{ count: 1, targetType: "RepsRange", target: 8, maxTarget: 12 }],
      perSide: false,
    });
    expect(mutations.addExercise).not.toHaveBeenCalled();
  });

  it("keeps the dialog open with the error and the sets when the API refuses the exercise", async () => {
    // Arrange
    const { rendered } = renderWorkout({
      getWorkout: withoutExercises,
      addExercise: async () => fail(new DomainError("CONFLICT", "")),
    });
    await rendered;
    await loaded();
    const user = userEvent.setup();
    await chooseFromCatalog(user, "Sentadilla");
    await chooseEffort(user, "RIR 3");
    await addSet(user, { count: "3", value: "10" });

    // Act
    await user.click(within(configurationDialog()).getByRole("button", { name: "Guardar" }));

    // Assert
    expect(
      await within(configurationDialog()).findByText(
        "La operación entra en conflicto con el estado actual.",
      ),
    ).toBeInTheDocument();
    expect(setSummaries()).toEqual(["3 × 10 repeticiones"]);
    expect(workoutItems({ hidden: true })).toHaveLength(0);
  });

  it("embeds a YouTube video without tracking cookies", async () => {
    // Arrange
    await renderWorkout().rendered;
    await loaded();
    const user = userEvent.setup();

    // Act
    await editExercise(user, 0);

    // Assert
    expect(within(configurationDialog()).getByTitle("Sentadilla")).toHaveAttribute(
      "src",
      "https://www.youtube-nocookie.com/embed/h_yZV27H684",
    );
  });

  it("shows the video before the exercise's name", async () => {
    // Arrange
    await renderWorkout().rendered;
    await loaded();
    const user = userEvent.setup();

    // Act
    await editExercise(user, 0);

    // Assert
    const dialog = configurationDialog();
    const video = within(dialog).getByTitle("Sentadilla");
    const name = within(dialog).getByRole("heading", { level: 3, name: "Sentadilla" });
    expect(video.compareDocumentPosition(name) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
  });

  it("links any other video in a new tab", async () => {
    // Arrange
    await renderWorkout().rendered;
    await loaded();
    const user = userEvent.setup();

    // Act
    await editExercise(user, 2);

    // Assert
    const link = within(configurationDialog()).getByRole("link", { name: "Ver vídeo" });
    expect(link).toHaveAttribute("href", "https://example.com/leg-press.mp4");
    expect(link).toHaveAttribute("target", "_blank");
    expect(within(configurationDialog()).queryByTitle("Prensa de piernas")).not.toBeInTheDocument();
  });

  it("shows no video section for an exercise without video", async () => {
    // Arrange
    await renderWorkout().rendered;
    await loaded();
    const user = userEvent.setup();

    // Act
    await editExercise(user, 1);

    // Assert
    expect(
      within(configurationDialog()).queryByRole("region", { name: "Vídeo" }),
    ).not.toBeInTheDocument();
  });

  it("shows the skeleton while the workout loads", async () => {
    // Arrange & Act
    await renderWorkout({ getWorkout: () => new Promise(() => {}) }).rendered;

    // Assert
    expect(await screen.findByLabelText("Cargando")).toBeInTheDocument();
    expect(screen.queryByRole("region", { name: "Ejercicios" })).not.toBeInTheDocument();
  });

  it("shows not found without reading when there is no id", async () => {
    // Arrange
    navigation.search = "";

    // Act
    const { readWorkout, rendered } = renderWorkout();
    await rendered;

    // Assert
    expect(await screen.findByText("No hemos encontrado este entrenamiento.")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Volver a entrenamientos" })).toHaveAttribute(
      "href",
      "/assigned-workouts",
    );
    expect(readWorkout).not.toHaveBeenCalled();
  });

  it("shows not found when the API does not find the workout", async () => {
    // Arrange & Act
    await renderWorkout({ getWorkout: async () => fail(new DomainError("NOT_FOUND", "")) })
      .rendered;

    // Assert
    expect(await screen.findByText("No hemos encontrado este entrenamiento.")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Volver a entrenamientos" })).toBeInTheDocument();
  });

  it("reads the catalog again when Reintentar is pressed after it failed", async () => {
    // Arrange
    const getExercises = vi
      .fn<() => Promise<Result<Exercise[], DomainError>>>()
      .mockResolvedValueOnce(fail(new DomainError("NETWORK", "")))
      .mockResolvedValueOnce(ok(catalog));
    const { readCatalog, rendered } = renderWorkout({ getExercises, getWorkout: withoutExercises });
    await rendered;
    const user = userEvent.setup();
    await screen.findByText("No hay conexión con el servidor.");

    // Act
    await user.click(screen.getByRole("button", { name: "Reintentar" }));

    // Assert
    await loaded();
    expect(propertiesSection()).toBeInTheDocument();
    expect(readCatalog).toHaveBeenCalledTimes(2);
  });

  it("follows the active language", async () => {
    // Arrange
    await renderWorkout({ getWorkout: withoutExercises, getExercises: muscleCatalog }).rendered;
    await loaded();
    const user = userEvent.setup();
    await openCatalog(user);

    // Act
    await act(() => i18n.changeLanguage("en"));

    // Assert
    const dialog = screen.getByRole("dialog", { name: "Exercise catalog" });
    expect(within(dialog).getByRole("searchbox", { name: "Search exercise" })).toBeInTheDocument();
    expect(within(dialog).getByRole("button", { name: "Legs" })).toBeInTheDocument();
    expect(within(dialog).getAllByText("Quadriceps").length).toBeGreaterThan(0);
    expect(within(dialog).getByText("Squat")).toBeInTheDocument();
    expect(
      screen.getByRole("heading", { name: "Workout properties", hidden: true }),
    ).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Add exercise", hidden: true })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Save", hidden: true })).toBeInTheDocument();
  });

  describe("exercise type", () => {
    const typeGroup = () => within(catalogDialog()).getByRole("group", { name: "Tipo" });
    const typePill = (name: string) => within(typeGroup()).getByRole("button", { name });

    async function effortOptions(user: ReturnType<typeof userEvent.setup>) {
      await user.click(within(configurationDialog()).getByRole("combobox", { name: "Esfuerzo" }));
      return screen.getAllByRole("option").map((option) => option.textContent);
    }

    it("shows the type pills first, under Tipo, outlined in their colours", async () => {
      // Arrange
      await renderWorkout({ getWorkout: withoutExercises }).rendered;
      await loaded();
      const user = userEvent.setup();

      // Act
      await openCatalog(user);

      // Assert
      const pills = within(typeGroup()).getAllByRole("button");
      expect(pills.map((pill) => pill.textContent)).toEqual(["Fuerza", "Cardio", "Estiramiento"]);
      expect(pills[0]).toHaveClass("MuiChip-colorStrength", "MuiChip-outlined");
      expect(pills[1]).toHaveClass("MuiChip-colorCardio", "MuiChip-outlined");
      expect(pills[2]).toHaveClass("MuiChip-colorStretching", "MuiChip-outlined");
      const typeLabel = within(catalogDialog()).getByText("Tipo");
      const bodyPartsLabel = within(catalogDialog()).getByText("Partes del cuerpo");
      expect(
        typeLabel.compareDocumentPosition(bodyPartsLabel) & Node.DOCUMENT_POSITION_FOLLOWING,
      ).toBeTruthy();
    });

    it("keeps only the selected type's exercises, fills its pill and keeps the other rows", async () => {
      // Arrange
      await renderWorkout({ getWorkout: withoutExercises, getExercises: typeCatalog }).rendered;
      await loaded();
      const user = userEvent.setup();
      await openCatalog(user);

      // Act
      await user.click(typePill("Cardio"));

      // Assert
      expect(catalogNames()).toEqual(["Bicicleta"]);
      expect(typePill("Cardio")).toHaveClass("MuiChip-colorCardio", "MuiChip-filled");
      expect(
        within(catalogDialog()).getByRole("group", { name: "Partes del cuerpo" }),
      ).toBeInTheDocument();
    });

    it("clears the type when its pill is selected again", async () => {
      // Arrange
      await renderWorkout({ getWorkout: withoutExercises, getExercises: typeCatalog }).rendered;
      await loaded();
      const user = userEvent.setup();
      await openCatalog(user);
      await user.click(typePill("Cardio"));

      // Act
      await user.click(typePill("Cardio"));

      // Assert
      expect(typePill("Cardio")).toHaveAttribute("aria-pressed", "false");
      expect(catalogNames()).toHaveLength(6);
    });

    it("combines the type with a body part", async () => {
      // Arrange
      await renderWorkout({ getWorkout: withoutExercises, getExercises: typeCatalog }).rendered;
      await loaded();
      const user = userEvent.setup();
      await openCatalog(user);
      await user.click(typePill("Fuerza"));

      // Act
      await user.click(
        within(within(catalogDialog()).getByRole("group", { name: "Partes del cuerpo" })).getByRole(
          "button",
          { name: "Piernas" },
        ),
      );

      // Assert
      expect(catalogNames()).toEqual(["Sentadilla", "Prensa de piernas"]);
    });

    it("keeps the type when the catalog opens again", async () => {
      // Arrange
      await renderWorkout({ getWorkout: withoutExercises, getExercises: typeCatalog }).rendered;
      await loaded();
      const user = userEvent.setup();
      await openCatalog(user);
      await user.click(typePill("Cardio"));
      await user.keyboard("{Escape}");
      await dialogsClosed();

      // Act
      await openCatalog(user);

      // Assert
      expect(typePill("Cardio")).toHaveAttribute("aria-pressed", "true");
      expect(catalogNames()).toEqual(["Bicicleta"]);
    });

    /** The type icon drawn behind a card, if any; MUI icons carry a `data-testid` named after them. */
    const watermarkOf = (item: HTMLElement) =>
      item.querySelector<HTMLElement>("[data-watermark]")?.querySelector<SVGElement>("svg") ?? null;
    const catalogItem = (name: string) =>
      catalogItems().find((item) => within(item).queryByText(name) !== null)!;

    it("draws each catalog card's type icon in its pastel tone and none for an unknown type", async () => {
      // Arrange
      await renderWorkout({ getWorkout: withoutExercises, getExercises: typeCatalog }).rendered;
      await loaded();
      const user = userEvent.setup();

      // Act
      await openCatalog(user);

      // Assert
      const expected = [
        ["Sentadilla", "FitnessCenterIcon", theme.palette.strength.pastel],
        ["Bicicleta", "FavoriteIcon", theme.palette.cardio.pastel],
        ["Estiramiento de isquios", "SelfImprovementIcon", theme.palette.stretching.pastel],
      ] as const;
      for (const [name, icon, color] of expected) {
        const watermark = watermarkOf(catalogItem(name));
        expect(watermark).toHaveAttribute("data-testid", icon);
        expect(watermark).toHaveStyle({ color });
      }
      expect(watermarkOf(catalogItem("Postura del árbol"))).toBeNull();
    });

    it("draws the icon behind the content, bottom-right, out of the layout and the pointer's way", async () => {
      // Arrange
      await renderWorkout({ getWorkout: withoutExercises, getExercises: typeCatalog }).rendered;
      await loaded();
      const user = userEvent.setup();

      // Act
      await openCatalog(user);

      // Assert
      const holder = catalogItem("Sentadilla").querySelector<HTMLElement>("[data-watermark]")!;
      expect(holder).toHaveAttribute("aria-hidden", "true");
      // Inset by half the width of the two buttons (star and "Elegir ejercicio"), so they do not sit over the icon.
      expect(holder).toHaveStyle({
        position: "absolute",
        right: "40px",
        bottom: "0px",
        height: "75%",
        pointerEvents: "none",
      });
    });

    it("draws a workout card's icon from its catalog type and none for a missing exercise", async () => {
      // Arrange
      const getWorkout = async () =>
        ok({
          ...workout,
          exercises: [
            workoutExercise("we-bike", bike.id, "MID", []),
            workoutExercise("we-gone", "ex-unknown", "MID", []),
          ],
        });

      // Act
      await renderWorkout({ getWorkout, getExercises: typeCatalog }).rendered;

      // Assert
      await loaded();
      const [bikeItem, goneItem] = workoutItems();
      expect(watermarkOf(bikeItem!)).toHaveAttribute("data-testid", "FavoriteIcon");
      expect(watermarkOf(bikeItem!)).toHaveStyle({ color: theme.palette.cardio.pastel });
      expect(watermarkOf(goneItem!)).toBeNull();
      // Nothing to keep clear of in the workout list: the icon reaches the card's edge.
      expect(bikeItem!.querySelector("[data-watermark]")).toHaveStyle({ right: "0px" });
    });

    it("offers the RIR scale for a strength exercise", async () => {
      // Arrange
      await renderWorkout({ getWorkout: withoutExercises, getExercises: typeCatalog }).rendered;
      await loaded();
      const user = userEvent.setup();
      await chooseFromCatalog(user, "Sentadilla");

      // Act
      const options = await effortOptions(user);

      // Assert
      expect(options).toEqual(["RIR 5+", "RIR 4", "RIR 3", "RIR 1-2", "RIR 0"]);
    });

    it("offers the RPE scale for a cardio exercise", async () => {
      // Arrange
      await renderWorkout({ getWorkout: withoutExercises, getExercises: typeCatalog }).rendered;
      await loaded();
      const user = userEvent.setup();
      await chooseFromCatalog(user, "Bicicleta");

      // Act
      const options = await effortOptions(user);

      // Assert
      expect(options).toEqual(["RPE 1-2", "RPE 3-4", "RPE 5-6", "RPE 7-8", "RPE 9-10"]);
    });

    it("offers the intensity scale for a stretching exercise", async () => {
      // Arrange
      await renderWorkout({ getWorkout: withoutExercises, getExercises: typeCatalog }).rendered;
      await loaded();
      const user = userEvent.setup();
      await chooseFromCatalog(user, "Estiramiento de isquios");

      // Act
      const options = await effortOptions(user);

      // Assert
      expect(options).toEqual(["Muy suave", "Suave", "Moderado", "Intenso", "Muy intenso"]);
    });

    it("keeps the generic scale for an unknown type", async () => {
      // Arrange
      await renderWorkout({ getWorkout: withoutExercises, getExercises: typeCatalog }).rendered;
      await loaded();
      const user = userEvent.setup();
      await chooseFromCatalog(user, "Postura del árbol");

      // Act
      const options = await effortOptions(user);

      // Assert
      expect(options).toEqual(["Bajo", "Medio-bajo", "Medio", "Medio-alto", "Alto"]);
    });

    it("shows a strength exercise's effort in RIR in the workout list", async () => {
      // Arrange & Act
      await renderWorkout().rendered;

      // Assert
      await loaded();
      const benchItem = workoutItems()[1]!;
      expect(within(benchItem).getByText("Esfuerzo: RIR 3")).toBeInTheDocument();
    });

    it("follows the active language for the types and the stretching scale", async () => {
      // Arrange
      await renderWorkout({ getWorkout: withoutExercises, getExercises: typeCatalog }).rendered;
      await loaded();
      const user = userEvent.setup();
      await openCatalog(user);

      // Act
      await act(() => i18n.changeLanguage("en"));

      // Assert
      const types = within(screen.getByRole("dialog", { name: "Exercise catalog" })).getByRole(
        "group",
        { name: "Type" },
      );
      expect(
        within(types)
          .getAllByRole("button")
          .map((pill) => pill.textContent),
      ).toEqual(["Strength", "Cardio", "Stretching"]);
    });

    it("offers the stretching scale in English", async () => {
      // Arrange
      await renderWorkout({ getWorkout: withoutExercises, getExercises: typeCatalog }).rendered;
      await loaded();
      const user = userEvent.setup();
      await chooseFromCatalog(user, "Estiramiento de isquios");
      await effortOptions(user);

      // Act
      await act(() => i18n.changeLanguage("en"));

      // Assert
      expect(screen.getAllByRole("option").map((option) => option.textContent)).toEqual([
        "Very light",
        "Light",
        "Moderate",
        "Intense",
        "Very intense",
      ]);
    });
  });

  describe("sections and favorites", () => {
    const otherUserId = "6a7b8c9d-0e1f-4a2b-8c3d-4e5f6a7b8c9d";
    const mine = catalogExercise(
      "ex-mine",
      "BICEPS",
      "ARMS",
      ["Mi curl", "My curl"],
      null,
      "STRENGTH",
      completeProfile.id,
    );
    const theirs = catalogExercise(
      "ex-theirs",
      "TRICEPS",
      "ARMS",
      ["Fondos de otro", "Their dips"],
      null,
      "STRENGTH",
      otherUserId,
    );
    const ownedCatalog = async () => ok([...catalog, mine, theirs]);
    const favoritesOf =
      (...ids: string[]) =>
      async () =>
        ok(ids.map(favoriteOf));

    const sectionList = () => within(catalogDialog()).getByRole("list", { name: "Secciones" });
    const section = (name: string) =>
      within(sectionList()).getByRole("button", { name: new RegExp(`^${name}( \\(\\d+\\))?$`) });
    const sectionNames = () =>
      within(sectionList())
        .getAllByRole("button")
        .map((button) => button.textContent);
    const card = (name: string) =>
      catalogItems().find((item) => within(item).queryByText(name) !== null)!;
    const star = (name: string, label = "Añadir a favoritos") =>
      within(card(name)).getByRole("button", { name: label });
    const stars = () =>
      within(catalogDialog()).getAllByRole("button", { name: /a favoritos|de favoritos/ });

    it("reads the favorites once, beside the workout and the catalog", async () => {
      // Arrange & Act
      const { favorites, rendered } = renderWorkout();
      await rendered;

      // Assert
      await loaded();
      expect(favorites.read).toHaveBeenCalledTimes(1);
    });

    it("shows the sections with their icons and Todos selected, listing the whole catalog", async () => {
      // Arrange
      await renderWorkout({ getWorkout: withoutExercises }).rendered;
      await loaded();
      const user = userEvent.setup();

      // Act
      await openCatalog(user);

      // Assert
      expect(sectionNames()).toEqual(["Todos (3)", "Mis ejercicios (0)", "Favoritos (0)"]);
      for (const name of ["Todos", "Mis ejercicios", "Favoritos"]) {
        expect(section(name).querySelector("svg")).not.toBeNull();
      }
      expect(section("Todos")).toHaveAttribute("aria-current", "true");
      expect(section("Mis ejercicios")).not.toHaveAttribute("aria-current");
      expect(section("Favoritos")).not.toHaveAttribute("aria-current");
      expect(catalogNames()).toEqual(["Sentadilla", "Prensa de piernas", "Press de banca"]);
    });

    it("selects the clicked section and deselects the previous one", async () => {
      // Arrange
      await renderWorkout({ getWorkout: withoutExercises }).rendered;
      await loaded();
      const user = userEvent.setup();
      await openCatalog(user);

      // Act
      await user.click(section("Favoritos"));

      // Assert
      expect(section("Favoritos")).toHaveAttribute("aria-current", "true");
      expect(section("Todos")).not.toHaveAttribute("aria-current");
    });

    it("lists only the exercises the user owns under Mis ejercicios", async () => {
      // Arrange
      await renderWorkout({ getWorkout: withoutExercises, getExercises: ownedCatalog }).rendered;
      await loaded();
      const user = userEvent.setup();
      await openCatalog(user);

      // Act
      await user.click(section("Mis ejercicios"));

      // Assert
      expect(catalogNames()).toEqual(["Mi curl"]);
    });

    it("keeps another user's exercise, and the system ones, under Todos only", async () => {
      // Arrange
      await renderWorkout({ getWorkout: withoutExercises, getExercises: ownedCatalog }).rendered;
      await loaded();
      const user = userEvent.setup();

      // Act
      await openCatalog(user);

      // Assert
      expect(catalogNames()).toEqual([
        "Sentadilla",
        "Prensa de piernas",
        "Press de banca",
        "Mi curl",
        "Fondos de otro",
      ]);
    });

    it("lists the exercises of an ADMIN's own under Mis ejercicios", async () => {
      // Arrange
      const admin: UserProfile = { ...completeProfile, id: "admin-1", role: "ADMIN" };
      const adminCurl = catalogExercise(
        "ex-admin-curl",
        "BICEPS",
        "ARMS",
        ["Curl del admin", "Admin curl"],
        null,
        "STRENGTH",
        "admin-1",
      );
      await renderWorkout({
        getWorkout: withoutExercises,
        getExercises: async () => ok([...catalog, mine, adminCurl]),
        profile: admin,
      }).rendered;
      await loaded();
      const user = userEvent.setup();
      await openCatalog(user);

      // Act
      await user.click(section("Mis ejercicios"));

      // Assert
      expect(catalogNames()).toEqual(["Curl del admin"]);
    });

    it("says so when the user owns no exercise", async () => {
      // Arrange
      await renderWorkout({ getWorkout: withoutExercises }).rendered;
      await loaded();
      const user = userEvent.setup();
      await openCatalog(user);

      // Act
      await user.click(section("Mis ejercicios"));

      // Assert
      expect(
        within(catalogDialog()).getByText("Ningún ejercicio coincide con el filtro."),
      ).toBeInTheDocument();
    });

    it("lists only the favorites under Favoritos", async () => {
      // Arrange
      await renderWorkout({
        getWorkout: withoutExercises,
        getFavorites: favoritesOf("ex-squat", "ex-bench"),
      }).rendered;
      await loaded();
      const user = userEvent.setup();
      await openCatalog(user);

      // Act
      await user.click(section("Favoritos"));

      // Assert
      expect(catalogNames()).toEqual(["Sentadilla", "Press de banca"]);
    });

    it("ignores a favorite whose exercise is not in the catalog, without an error", async () => {
      // Arrange
      await renderWorkout({
        getWorkout: withoutExercises,
        getFavorites: favoritesOf("ex-gone", "ex-squat"),
      }).rendered;
      await loaded();
      const user = userEvent.setup();
      await openCatalog(user);

      // Act
      await user.click(section("Favoritos"));

      // Assert
      expect(catalogNames()).toEqual(["Sentadilla"]);
      expect(within(catalogDialog()).queryByRole("alert")).not.toBeInTheDocument();
    });

    it("takes an unmarked exercise out of Favoritos at once, without reading the favorites again", async () => {
      // Arrange
      const { favorites, rendered } = renderWorkout({
        getWorkout: withoutExercises,
        getFavorites: favoritesOf("ex-squat"),
      });
      await rendered;
      await loaded();
      const user = userEvent.setup();
      await openCatalog(user);
      await user.click(section("Favoritos"));

      // Act
      await user.click(star("Sentadilla", "Quitar de favoritos"));

      // Assert
      await waitFor(() => expect(catalogNames()).toEqual([]));
      expect(
        within(catalogDialog()).getByText("Ningún ejercicio coincide con el filtro."),
      ).toBeInTheDocument();
      expect(favorites.read).toHaveBeenCalledTimes(1);
    });

    it("narrows a section with a pill and sends no request", async () => {
      // Arrange
      const { favorites, mutations, rendered } = renderWorkout({
        getWorkout: withoutExercises,
        getFavorites: favoritesOf("ex-squat", "ex-bench"),
      });
      await rendered;
      await loaded();
      const user = userEvent.setup();
      await openCatalog(user);
      await user.click(section("Favoritos"));

      // Act
      await user.click(within(catalogDialog()).getByRole("button", { name: "Piernas" }));

      // Assert
      expect(catalogNames()).toEqual(["Sentadilla"]);
      expect(favorites.read).toHaveBeenCalledTimes(1);
      expect(favorites.mark).not.toHaveBeenCalled();
      expect(favorites.unmark).not.toHaveBeenCalled();
      expect(mutationCalls(mutations)).toBe(0);
    });

    it("narrows a section with the text", async () => {
      // Arrange
      await renderWorkout({
        getWorkout: withoutExercises,
        getFavorites: favoritesOf("ex-squat", "ex-bench"),
      }).rendered;
      await loaded();
      const user = userEvent.setup();
      await openCatalog(user);
      await user.click(section("Favoritos"));

      // Act
      await user.type(
        within(catalogDialog()).getByRole("searchbox", { name: "Buscar ejercicio" }),
        "banca",
      );

      // Assert
      expect(catalogNames()).toEqual(["Press de banca"]);
    });

    it("keeps the section when the catalog is opened again", async () => {
      // Arrange
      await renderWorkout({ getWorkout: withoutExercises, getExercises: ownedCatalog }).rendered;
      await loaded();
      const user = userEvent.setup();
      await openCatalog(user);
      await user.click(section("Mis ejercicios"));

      // Act
      await user.click(within(catalogDialog()).getByRole("button", { name: "Cerrar" }));
      await dialogsClosed();
      await openCatalog(user);

      // Assert
      expect(section("Mis ejercicios")).toHaveAttribute("aria-current", "true");
      expect(catalogNames()).toEqual(["Mi curl"]);
    });

    it("shows the star before Elegir ejercicio on each card", async () => {
      // Arrange
      await renderWorkout({ getWorkout: withoutExercises }).rendered;
      await loaded();
      const user = userEvent.setup();

      // Act
      await openCatalog(user);

      // Assert
      for (const item of catalogItems()) {
        expect(
          within(item)
            .getAllByRole("button")
            .map((button) => button.getAttribute("aria-label")),
        ).toEqual(["Añadir a favoritos", "Elegir ejercicio"]);
      }
    });

    it("marks an exercise as a favorite and fills its star", async () => {
      // Arrange
      const { favorites, rendered } = renderWorkout({ getWorkout: withoutExercises });
      await rendered;
      await loaded();
      const user = userEvent.setup();
      await openCatalog(user);

      // Act
      await user.click(star("Sentadilla"));

      // Assert
      await waitFor(() => expect(star("Sentadilla", "Quitar de favoritos")).toBeInTheDocument());
      expect(favorites.mark).toHaveBeenCalledTimes(1);
      expect(favorites.mark).toHaveBeenCalledWith("ex-squat");
      expect(favorites.unmark).not.toHaveBeenCalled();
      expect(star("Prensa de piernas")).toBeInTheDocument();
    });

    it("unmarks a favorite and outlines its star", async () => {
      // Arrange
      const { favorites, rendered } = renderWorkout({
        getWorkout: withoutExercises,
        getFavorites: favoritesOf("ex-squat"),
      });
      await rendered;
      await loaded();
      const user = userEvent.setup();
      await openCatalog(user);

      // Act
      await user.click(star("Sentadilla", "Quitar de favoritos"));

      // Assert
      await waitFor(() => expect(star("Sentadilla")).toBeInTheDocument());
      expect(favorites.unmark).toHaveBeenCalledTimes(1);
      expect(favorites.unmark).toHaveBeenCalledWith("ex-squat");
      expect(favorites.mark).not.toHaveBeenCalled();
    });

    it("shows a favorite from the first moment the catalog opens", async () => {
      // Arrange
      await renderWorkout({
        getWorkout: withoutExercises,
        getFavorites: favoritesOf("ex-bench"),
      }).rendered;
      await loaded();
      const user = userEvent.setup();

      // Act
      await openCatalog(user);

      // Assert
      await waitFor(() => expect(star("Press de banca", "Quitar de favoritos")).toBeEnabled());
      expect(star("Sentadilla")).toBeInTheDocument();
    });

    it("disables the stars while the favorites load", async () => {
      // Arrange
      await renderWorkout({
        getWorkout: withoutExercises,
        getFavorites: () => new Promise(() => {}),
      }).rendered;
      await loaded();
      const user = userEvent.setup();

      // Act
      await openCatalog(user);

      // Assert
      expect(stars()).toHaveLength(3);
      for (const button of stars()) {
        expect(button).toBeDisabled();
      }
    });

    it("disables only the star whose request is in progress", async () => {
      // Arrange
      let answer: (result: Result<ExerciseFavorite, DomainError>) => void = () => {};
      const { rendered } = renderWorkout({
        getWorkout: withoutExercises,
        markFavorite: () => new Promise((resolve) => (answer = resolve)),
      });
      await rendered;
      await loaded();
      const user = userEvent.setup();
      await openCatalog(user);

      // Act
      await user.click(star("Sentadilla"));

      // Assert
      expect(star("Sentadilla")).toBeDisabled();
      expect(star("Prensa de piernas")).toBeEnabled();
      await act(async () => answer(ok(favoriteOf("ex-squat"))));
      await waitFor(() => expect(star("Sentadilla", "Quitar de favoritos")).toBeEnabled());
    });

    it.each([
      ["CONFLICT", "La operación entra en conflicto con el estado actual."],
      ["NOT_FOUND", "No hemos encontrado lo que buscas."],
      ["NETWORK", "No hay conexión con el servidor."],
    ])(
      "keeps the star outlined and shows the %s message when marking is refused",
      async (code, message) => {
        // Arrange
        await renderWorkout({
          getWorkout: withoutExercises,
          markFavorite: async () => fail(new DomainError(code as DomainError["code"], "")),
        }).rendered;
        await loaded();
        const user = userEvent.setup();
        await openCatalog(user);

        // Act
        await user.click(star("Sentadilla"));

        // Assert
        expect(await within(catalogDialog()).findByRole("alert")).toHaveTextContent(message);
        expect(star("Sentadilla")).toBeEnabled();
      },
    );

    it("keeps the star filled and shows the NETWORK message when unmarking gets no response", async () => {
      // Arrange
      await renderWorkout({
        getWorkout: withoutExercises,
        getFavorites: favoritesOf("ex-squat"),
        unmarkFavorite: async () => fail(new DomainError("NETWORK", "")),
      }).rendered;
      await loaded();
      const user = userEvent.setup();
      await openCatalog(user);

      // Act
      await user.click(star("Sentadilla", "Quitar de favoritos"));

      // Assert
      expect(await within(catalogDialog()).findByRole("alert")).toHaveTextContent(
        "No hay conexión con el servidor.",
      );
      expect(star("Sentadilla", "Quitar de favoritos")).toBeEnabled();
    });

    it("clears the star error when another star is used", async () => {
      // Arrange
      let refuse = true;
      await renderWorkout({
        getWorkout: withoutExercises,
        markFavorite: async (exerciseId) =>
          refuse ? fail(new DomainError("CONFLICT", "")) : ok(favoriteOf(exerciseId)),
      }).rendered;
      await loaded();
      const user = userEvent.setup();
      await openCatalog(user);
      await user.click(star("Sentadilla"));
      await within(catalogDialog()).findByRole("alert");
      refuse = false;

      // Act
      await user.click(star("Prensa de piernas"));

      // Assert
      await waitFor(() =>
        expect(star("Prensa de piernas", "Quitar de favoritos")).toBeInTheDocument(),
      );
      expect(within(catalogDialog()).queryByRole("alert")).not.toBeInTheDocument();
    });

    it("keeps the catalog usable and disables the stars and Favoritos when the favorites fail to load", async () => {
      // Arrange
      await renderWorkout({
        getWorkout: withoutExercises,
        getFavorites: async () => fail(new DomainError("NETWORK", "")),
      }).rendered;
      await loaded();
      const user = userEvent.setup();

      // Act
      await openCatalog(user);

      // Assert
      expect(
        await within(catalogDialog()).findByText("No se pudieron cargar los favoritos"),
      ).toBeInTheDocument();
      expect(section("Favoritos")).toHaveAttribute("aria-disabled", "true");
      for (const button of stars()) {
        expect(button).toBeDisabled();
      }
      expect(catalogNames()).toEqual(["Sentadilla", "Prensa de piernas", "Press de banca"]);
    });

    it("still opens an exercise's configuration when the favorites fail to load", async () => {
      // Arrange
      await renderWorkout({
        getWorkout: withoutExercises,
        getFavorites: async () => fail(new DomainError("NETWORK", "")),
      }).rendered;
      await loaded();
      const user = userEvent.setup();
      await openCatalog(user);

      // Act
      await user.click(
        within(card("Sentadilla")).getByRole("button", { name: "Elegir ejercicio" }),
      );

      // Assert
      expect(
        await screen.findByRole("dialog", { name: "Configuración del ejercicio" }),
      ).toBeInTheDocument();
    });

    it("shows no star on the workout's exercises or in the configuration dialog", async () => {
      // Arrange
      await renderWorkout({ getFavorites: favoritesOf("ex-squat") }).rendered;
      await loaded();
      const user = userEvent.setup();

      // Act
      await editExercise(user, 0);

      // Assert
      expect(
        screen.queryByRole("button", { name: /favoritos/i, hidden: true }),
      ).not.toBeInTheDocument();
    });

    it("follows the active language for the sections and the star", async () => {
      // Arrange
      await renderWorkout({ getWorkout: withoutExercises }).rendered;
      await loaded();
      const user = userEvent.setup();
      await openCatalog(user);

      // Act
      await act(() => i18n.changeLanguage("en"));

      // Assert
      const dialog = screen.getByRole("dialog", { name: "Exercise catalog" });
      const sections = within(dialog).getByRole("list", { name: "Sections" });
      expect(
        within(sections)
          .getAllByRole("button")
          .map((button) => button.textContent),
      ).toEqual(["All (3)", "My exercises (0)", "Favorites (0)"]);
      expect(within(dialog).getAllByRole("button", { name: "Add to favorites" })).toHaveLength(3);
    });

    it("shows the favorites load error in English", async () => {
      // Arrange
      await renderWorkout({
        getWorkout: withoutExercises,
        getFavorites: async () => fail(new DomainError("NETWORK", "")),
      }).rendered;
      await loaded();
      const user = userEvent.setup();
      await openCatalog(user);
      await within(catalogDialog()).findByText("No se pudieron cargar los favoritos");

      // Act
      await act(() => i18n.changeLanguage("en"));

      // Assert
      expect(
        within(screen.getByRole("dialog", { name: "Exercise catalog" })).getByText(
          "Favorites could not be loaded",
        ),
      ).toBeInTheDocument();
    });
  });

  describe("minutes, rest and per side", () => {
    const plank = workoutExercise(
      "we-1",
      squat.id,
      "HIGH",
      [
        { count: 3, target: { type: "Minutes", minutes: 1.5 } },
        { count: 1, target: { type: "Minutes", minutes: 20 } },
      ],
      { perSide: true, restMinutes: 1.5 },
    );
    const withPlank = async () => ok({ ...workout, exercises: [plank] });
    const restField = () =>
      within(configurationDialog()).getByRole("textbox", { name: "Descanso entre series" });
    const perSideSwitch = () =>
      within(configurationDialog()).getByRole("switch", { name: "Por lado" });

    it("offers minutes after the other target types", async () => {
      // Arrange
      await renderWorkout().rendered;
      await loaded();
      const user = userEvent.setup();
      await editExercise(user, 0);
      const form = within(configurationDialog()).getByRole("group", { name: "Añadir serie" });

      // Act
      await user.click(within(form).getByRole("combobox", { name: "Tipo" }));

      // Assert
      expect(screen.getAllByRole("option").map((option) => option.textContent)).toEqual([
        "Repeticiones",
        "Segundos",
        "Máximas repeticiones",
        "Rango de repeticiones",
        "Minutos",
      ]);
    });

    it("adds a minutes set typed as M:SS without a request", async () => {
      // Arrange
      const { mutations, rendered } = renderWorkout();
      await rendered;
      await loaded();
      const user = userEvent.setup();
      await editExercise(user, 0);

      // Act
      await addSet(user, { count: "3", type: "Minutos", value: "1:30" });

      // Assert
      expect(setSummaries()).toEqual(["3 × 10 repeticiones", "3 × 1:30 min"]);
      expect(mutationCalls(mutations)).toBe(0);
    });

    it.each(["1.5", "1:60", "0"])("reports the invalid minutes %j", async (value) => {
      // Arrange
      await renderWorkout().rendered;
      await loaded();
      const user = userEvent.setup();
      await editExercise(user, 0);

      // Act
      await addSet(user, { count: "1", type: "Minutos", value });

      // Assert
      const form = within(configurationDialog()).getByRole("group", { name: "Añadir serie" });
      expect(within(form).getByRole("textbox", { name: "Valor" })).toHaveAccessibleDescription(
        "Usa minutos (2) o minutos:segundos (1:30).",
      );
      expect(setSummaries()).toEqual(["3 × 10 repeticiones"]);
    });

    it("opens a chosen exercise with no rest and not per side", async () => {
      // Arrange
      await renderWorkout({ getWorkout: withoutExercises }).rendered;
      await loaded();
      const user = userEvent.setup();

      // Act
      await chooseFromCatalog(user, "Sentadilla");

      // Assert
      expect(restField()).toHaveValue("");
      expect(perSideSwitch()).not.toBeChecked();
    });

    it("loads the stored rest, per side and minutes as M or M:SS", async () => {
      // Arrange
      await renderWorkout({ getWorkout: withPlank }).rendered;
      await loaded();
      const user = userEvent.setup();

      // Act
      await editExercise(user, 0);

      // Assert
      expect(restField()).toHaveValue("1:30");
      expect(perSideSwitch()).toBeChecked();
      expect(setSummaries()).toEqual(["3 × 1:30 min por lado", "1 × 20 min por lado"]);
    });

    it("shows whole minutes of rest without seconds", async () => {
      // Arrange
      const getWorkout = async () =>
        ok({
          ...workout,
          exercises: [workoutExercise("we-1", squat.id, "HIGH", [], { restMinutes: 2 })],
        });
      await renderWorkout({ getWorkout }).rendered;
      await loaded();
      const user = userEvent.setup();

      // Act
      await editExercise(user, 0);

      // Assert
      expect(restField()).toHaveValue("2");
    });

    it("marks every set per side once the switch is on, without a request", async () => {
      // Arrange
      const { mutations, rendered } = renderWorkout();
      await rendered;
      await loaded();
      const user = userEvent.setup();
      await editExercise(user, 0);

      // Act
      await user.click(perSideSwitch());

      // Assert
      expect(setSummaries()).toEqual(["3 × 10 repeticiones por lado"]);
      expect(mutationCalls(mutations)).toBe(0);
    });

    it("sends minutes and rest as decimals and per side, once", async () => {
      // Arrange
      const { mutations, rendered } = renderWorkout({ getWorkout: withoutExercises });
      await rendered;
      await loaded();
      const user = userEvent.setup();
      await chooseFromCatalog(user, "Prensa de piernas");
      await chooseEffort(user, "RIR 3");
      await addSet(user, { count: "3", type: "Minutos", value: "1:30" });
      await user.type(restField(), "2");
      await user.click(perSideSwitch());

      // Act
      await user.click(within(configurationDialog()).getByRole("button", { name: "Guardar" }));

      // Assert
      await waitFor(() => expect(mutations.addExercise).toHaveBeenCalledTimes(1));
      expect(mutations.addExercise).toHaveBeenCalledWith({
        workoutId: "w-1",
        exerciseId: "ex-press",
        effort: "MID",
        sets: [{ count: 3, targetType: "Minutes", targetMinutes: 1.5 }],
        perSide: true,
        restMinutes: 2,
      });
    });

    it("keeps a stored exercise's rest and per side when only its sets change", async () => {
      // Arrange
      const { mutations, rendered } = renderWorkout({ getWorkout: withPlank });
      await rendered;
      await loaded();
      const user = userEvent.setup();
      await editExercise(user, 0);
      await user.click(
        within(configurationDialog()).getAllByRole("button", { name: "Quitar serie" })[1]!,
      );

      // Act
      await user.click(within(configurationDialog()).getByRole("button", { name: "Guardar" }));

      // Assert
      await waitFor(() => expect(mutations.updateExercise).toHaveBeenCalledTimes(1));
      expect(mutations.updateExercise).toHaveBeenCalledWith({
        workoutId: "w-1",
        exerciseId: "ex-squat",
        effort: "HIGH",
        sets: [{ count: 3, targetType: "Minutes", targetMinutes: 1.5 }],
        perSide: true,
        restMinutes: 1.5,
      });
    });

    it("reports an invalid rest and sends nothing", async () => {
      // Arrange
      const { mutations, rendered } = renderWorkout();
      await rendered;
      await loaded();
      const user = userEvent.setup();
      await editExercise(user, 0);
      await user.type(restField(), "1:75");

      // Act
      await user.click(within(configurationDialog()).getByRole("button", { name: "Guardar" }));

      // Assert
      expect(restField()).toHaveAccessibleDescription("Usa minutos (2) o minutos:segundos (1:30).");
      expect(mutationCalls(mutations)).toBe(0);
    });

    it("keeps the rest and per side as edited when the API refuses them", async () => {
      // Arrange
      const { rendered } = renderWorkout({
        updateExercise: async () => fail(new DomainError("VALIDATION", "")),
      });
      await rendered;
      await loaded();
      const user = userEvent.setup();
      await editExercise(user, 0);
      await user.type(restField(), "0:45");
      await user.click(perSideSwitch());

      // Act
      await user.click(within(configurationDialog()).getByRole("button", { name: "Guardar" }));

      // Assert
      expect(
        await within(configurationDialog()).findByText("Revisa los datos introducidos."),
      ).toBeInTheDocument();
      expect(restField()).toHaveValue("0:45");
      expect(perSideSwitch()).toBeChecked();
    });

    it("shows minutes, per side and rest in the workout list", async () => {
      // Arrange / Act
      await renderWorkout({ getWorkout: withPlank }).rendered;

      // Assert
      await loaded();
      const [item] = workoutItems();
      const setsLine = within(item!).getByText("3 × 1:30 min, 1 × 20 min · por lado");
      const effortLine = within(item!).getByText("Esfuerzo: RIR 0");
      const restLine = within(item!).getByText("Descanso: 1:30 min");
      expect(
        setsLine.compareDocumentPosition(effortLine) & Node.DOCUMENT_POSITION_FOLLOWING,
      ).toBeTruthy();
      expect(
        effortLine.compareDocumentPosition(restLine) & Node.DOCUMENT_POSITION_FOLLOWING,
      ).toBeTruthy();
    });

    it("shows no rest line for an exercise without rest", async () => {
      // Arrange / Act
      await renderWorkout().rendered;

      // Assert
      await loaded();
      const [item] = workoutItems();
      expect(within(item!).queryByText(/^Descanso/)).not.toBeInTheDocument();
    });

    it("shows rest and per side in English", async () => {
      // Arrange
      await renderWorkout({ getWorkout: withPlank }).rendered;
      await loaded();
      const user = userEvent.setup();
      await editExercise(user, 0);

      // Act
      await act(() => i18n.changeLanguage("en"));

      // Assert
      const dialog = screen.getByRole("dialog", { name: "Exercise configuration" });
      expect(
        within(dialog).getByRole("textbox", { name: "Rest between sets" }),
      ).toHaveAccessibleDescription("Minutes or m:ss, e.g. 1:30");
      expect(within(dialog).getByRole("switch", { name: "Per side" })).toBeChecked();
      expect(
        within(within(dialog).getByRole("list", { name: "Sets" }))
          .getAllByRole("listitem")
          .map((item) => item.textContent),
      ).toEqual(["3 × 1:30 min per side", "1 × 20 min per side"]);
    });
  });

  describe("context and placement", () => {
    const luis = {
      id: "6a2d3f1e-0e6c-4b64-9d8f-1e7a8d2b3c4d",
      email: "luis@example.com",
      name: "Luis",
      surname: "Pérez",
      role: "TRAINER",
    };
    const admin: UserProfile = { ...completeProfile, id: "admin-1", role: "ADMIN" };
    const prescribed: WorkoutById = { ...workout, trainerId: luis.id, trainer: luis };
    /** Placed in week 2 of mesocycle `m-1`, which dates it on the Wednesday of 2026-11-02's week. */
    const placed: WorkoutById = {
      ...prescribed,
      workoutDate: "2026-11-04",
      mesocycleId: "m-1",
      mesocycleWeekNumber: 2,
      mesocycle: {
        id: "m-1",
        name: "Hipertrofia",
        objective: "Ganar masa",
        startDate: "2026-10-26",
        durationInWeeks: 4,
        weeks: [],
      },
    };
    const strip = () => screen.getByRole("region", { name: "Datos del entrenamiento" });

    it("shows the athlete above the properties card", async () => {
      // Arrange & Act
      await renderWorkout().rendered;

      // Assert
      await loaded();
      expect(within(strip()).getByText("AG")).toBeInTheDocument();
      expect(within(strip()).getByText("Ana García")).toBeInTheDocument();
      expect(within(strip()).getByText("Atleta")).toBeInTheDocument();
      expect(
        strip().compareDocumentPosition(propertiesSection()) & Node.DOCUMENT_POSITION_FOLLOWING,
      ).toBeTruthy();
    });

    it("shows the athlete's email when they have no name", async () => {
      // Arrange
      const nameless = { ...workout, athlete: { ...ana, name: "", surname: "" } };

      // Act
      await renderWorkout({ getWorkout: async () => ok(nameless) }).rendered;

      // Assert
      await loaded();
      expect(within(strip()).getByText("A")).toBeInTheDocument();
      expect(within(strip()).getByText("ana@example.com")).toBeInTheDocument();
    });

    it("shows the trainer to an admin", async () => {
      // Arrange & Act
      await renderWorkout({ getWorkout: async () => ok(prescribed), profile: admin }).rendered;

      // Assert
      await loaded();
      expect(within(strip()).getByText("LP")).toBeInTheDocument();
      expect(within(strip()).getByText("Luis Pérez")).toBeInTheDocument();
      expect(within(strip()).getByText("Entrenador")).toBeInTheDocument();
    });

    it("hides the trainer from a trainer", async () => {
      // Arrange & Act
      await renderWorkout({ getWorkout: async () => ok(prescribed) }).rendered;

      // Assert
      await loaded();
      expect(within(strip()).queryByText("Entrenador")).not.toBeInTheDocument();
      expect(within(strip()).queryByText("Luis Pérez")).not.toBeInTheDocument();
    });

    it("shows no trainer entry to an admin when the workout has none", async () => {
      // Arrange & Act
      await renderWorkout({ profile: admin }).rendered;

      // Assert
      await loaded();
      expect(within(strip()).queryByText("Entrenador")).not.toBeInTheDocument();
    });

    it("shows a placed workout's mesocycle week as a breadcrumb from the workout itself", async () => {
      // Arrange
      const { readMesocycle, rendered } = renderWorkout({
        getWorkout: async () => ok(placed),
      });

      // Act
      await rendered;
      await loaded();

      // Assert
      const breadcrumb = await within(strip()).findByRole("navigation", {
        name: "Mesociclo y semana",
      });
      expect(breadcrumb).toHaveTextContent("Hipertrofia/Semana 2");
      expect(within(strip()).queryByRole("link")).not.toBeInTheDocument();
      expect(readMesocycle).not.toHaveBeenCalled();
    });

    it("shows no mesocycle for a standalone workout", async () => {
      // Arrange
      const { readMesocycle, rendered } = renderWorkout();

      // Act
      await rendered;
      await loaded();

      // Assert
      expect(within(strip()).queryByText("Mesociclo")).not.toBeInTheDocument();
      expect(readMesocycle).not.toHaveBeenCalled();
    });

    it("leads Volver back to the mesocycle of a placed workout", async () => {
      // Arrange & Act
      await renderWorkout({
        getWorkout: async () => ok(placed),
      }).rendered;

      // Assert
      await loaded();
      expect(screen.getByRole("link", { name: "Volver" })).toHaveAttribute(
        "href",
        "/mesocycle?id=m-1",
      );
    });

    it("locks the week of a placed workout", async () => {
      // Arrange & Act
      await renderWorkout({
        getWorkout: async () => ok(placed),
      }).rendered;

      // Assert
      await loaded();
      const week = within(propertiesSection()).getByRole("group", { name: "Semana" });
      expect(week).toHaveTextContent("Semana del 2 nov – 8 nov");
      expect(within(week).getByRole("button", { name: "Semana anterior" })).toBeDisabled();
      expect(within(week).getByRole("button", { name: "Semana siguiente" })).toBeDisabled();
    });

    it("saves a placed workout with its placement and no date", async () => {
      // Arrange
      const { mutations, rendered } = renderWorkout({
        getWorkout: async () => ok(placed),
      });
      await rendered;
      await loaded();
      const user = userEvent.setup();
      const name = within(propertiesSection()).getByRole("textbox", { name: "Nombre" });
      await user.clear(name);
      await user.type(name, "Pierna B");

      // Act
      await user.click(screen.getByRole("button", { name: "Guardar" }));

      // Assert
      await waitFor(() => expect(mutations.update).toHaveBeenCalledTimes(1));
      expect(mutations.update).toHaveBeenCalledWith({
        id: "w-1",
        name: "Pierna B",
        description: "Día 1",
        userId: placed.userId,
        trainerId: luis.id,
        mesocycleId: "m-1",
        mesocycleWeekNumber: 2,
      });
    });

    it("saves a standalone workout without a placement", async () => {
      // Arrange
      const { mutations, rendered } = renderWorkout();
      await rendered;
      await loaded();
      const user = userEvent.setup();

      // Act
      await user.click(screen.getByRole("button", { name: "Guardar" }));

      // Assert
      await waitFor(() => expect(mutations.update).toHaveBeenCalledTimes(1));
      const [input] = mutations.update.mock.calls[0]!;
      expect(input).not.toHaveProperty("mesocycleId");
      expect(input).not.toHaveProperty("mesocycleWeekNumber");
      expect(input).toHaveProperty("workoutDate", "2026-11-04");
    });

    it("keeps showing the athlete after a save", async () => {
      // Arrange
      const { mutations, rendered } = renderWorkout();
      await rendered;
      await loaded();
      const user = userEvent.setup();

      // Act
      await user.click(screen.getByRole("button", { name: "Guardar" }));

      // Assert
      expect(await screen.findByText("Entrenamiento guardado.")).toBeInTheDocument();
      expect(mutations.update).toHaveBeenCalledTimes(1);
      expect(within(strip()).getByText("Ana García")).toBeInTheDocument();
    });

    it("shows the strip in the active language", async () => {
      // Arrange
      await renderWorkout({
        getWorkout: async () => ok(placed),
        profile: admin,
      }).rendered;
      await loaded();
      await within(strip()).findByRole("navigation", { name: "Mesociclo y semana" });

      // Act
      await act(() => i18n.changeLanguage("en"));

      // Assert
      const english = screen.getByRole("region", { name: "Workout details" });
      expect(within(english).getByText("Athlete")).toBeInTheDocument();
      expect(within(english).getByText("Trainer")).toBeInTheDocument();
      expect(within(english).getByText("Mesocycle")).toBeInTheDocument();
      expect(
        within(english).getByRole("navigation", { name: "Mesocycle and week" }),
      ).toHaveTextContent("Hipertrofia/Week 2");
    });
  });

  describe("Guardar como plantilla", () => {
    const saveAsTemplateButton = () =>
      within(propertiesSection()).getByRole("button", { name: "Guardar como plantilla" });

    it("shows an outlined Guardar como plantilla right before Guardar", async () => {
      // Arrange & Act
      await renderWorkout().rendered;
      await loaded();

      // Assert
      const buttons = within(propertiesSection())
        .getAllByRole("button")
        .map((button) => button.textContent);
      const index = buttons.indexOf("Guardar como plantilla");
      expect(index).toBeGreaterThan(-1);
      expect(buttons[index + 1]).toBe("Guardar");
      expect(saveAsTemplateButton()).toHaveClass("MuiButton-outlined");
      expect(saveAsTemplateButton()).toBeEnabled();
    });

    it("is disabled with its hint while the properties have unsaved edits", async () => {
      // Arrange
      await renderWorkout().rendered;
      await loaded();
      const user = userEvent.setup();

      // Act
      await user.type(within(propertiesSection()).getByRole("textbox", { name: "Nombre" }), " B");

      // Assert
      expect(saveAsTemplateButton()).toBeDisabled();
      expect(
        within(propertiesSection()).getByLabelText(
          "Guarda los cambios antes de crear la plantilla.",
        ),
      ).toBeInTheDocument();
    });

    it("is enabled again once the edits are saved", async () => {
      // Arrange
      await renderWorkout().rendered;
      await loaded();
      const user = userEvent.setup();
      await user.type(within(propertiesSection()).getByRole("textbox", { name: "Nombre" }), " B");

      // Act
      await user.click(within(propertiesSection()).getByRole("button", { name: "Guardar" }));

      // Assert
      await screen.findByText("Entrenamiento guardado.");
      expect(saveAsTemplateButton()).toBeEnabled();
    });

    it("creates a template from the workout once, without updating it, and links to it", async () => {
      // Arrange
      const { mutations, rendered } = renderWorkout();
      await rendered;
      await loaded();
      const user = userEvent.setup();

      // Act
      await user.click(saveAsTemplateButton());

      // Assert
      expect(await screen.findByText("Plantilla creada.")).toBeInTheDocument();
      expect(mutations.saveAsTemplate).toHaveBeenCalledTimes(1);
      expect(mutations.saveAsTemplate).toHaveBeenCalledWith({ workoutId: "w-1" });
      expect(mutations.update).not.toHaveBeenCalled();
      expect(screen.getByRole("link", { name: "Ver plantilla" })).toHaveAttribute(
        "href",
        "/workout-template?id=wt-9",
      );
      expect(push).not.toHaveBeenCalled();
    });

    it("shows the error and leaves the workout as it was when the API refuses it", async () => {
      // Arrange
      const { mutations, rendered } = renderWorkout({
        saveAsTemplate: async () => fail(new DomainError("VALIDATION", "")),
      });
      await rendered;
      await loaded();
      const user = userEvent.setup();

      // Act
      await user.click(saveAsTemplateButton());

      // Assert
      expect(await screen.findByRole("alert")).toBeInTheDocument();
      expect(screen.queryByText("Plantilla creada.")).not.toBeInTheDocument();
      expect(mutations.update).not.toHaveBeenCalled();
    });

    it("reads in English", async () => {
      // Arrange
      await renderWorkout().rendered;
      await loaded();

      // Act
      await act(() => i18n.changeLanguage("en"));

      // Assert
      expect(
        within(screen.getByRole("region", { name: "Workout properties" })).getByRole("button", {
          name: "Save as template",
        }),
      ).toBeInTheDocument();
    });
  });

  describe("status change", () => {
    const withStatus = (status: string) => async () => ok({ ...workout, status });
    const statusButton = (name: string) =>
      within(propertiesSection()).queryByRole("button", { name });
    const publishDialog = () =>
      screen.queryByRole("dialog", { name: "¿Publicar el entrenamiento?" });
    const backToDraftDialog = () => screen.queryByRole("dialog", { name: "¿Volver a borrador?" });
    const lockNotice = "El atleta ha empezado este entrenamiento y no puedes editarlo";
    /** The page behind a closing dialog stays hidden from assistive technology until its transition ends. */
    const propertiesShown = () =>
      screen.findByRole("region", { name: "Propiedades del entrenamiento" });

    it("offers Publicar right after Guardar on a draft workout, and no Volver a borrador", async () => {
      // Arrange & Act
      await renderWorkout().rendered;
      await loaded();

      // Assert
      const buttons = within(propertiesSection())
        .getAllByRole("button")
        .map((button) => button.textContent);
      expect(buttons[buttons.indexOf("Guardar") + 1]).toBe("Publicar");
      expect(statusButton("Publicar")).toHaveClass(
        "MuiButton-contained",
        "MuiButton-colorSecondary",
      );
      expect(statusButton("Volver a borrador")).not.toBeInTheDocument();
      expect(screen.queryByText(lockNotice)).not.toBeInTheDocument();
    });

    it("offers Volver a borrador right after Guardar on a published workout, and no Publicar", async () => {
      // Arrange & Act
      await renderWorkout({ getWorkout: withStatus("PUBLISHED") }).rendered;
      await loaded();

      // Assert
      const buttons = within(propertiesSection())
        .getAllByRole("button")
        .map((button) => button.textContent);
      expect(buttons[buttons.indexOf("Guardar") + 1]).toBe("Volver a borrador");
      expect(statusButton("Volver a borrador")).toHaveClass(
        "MuiButton-outlined",
        "MuiButton-colorSecondary",
      );
      expect(statusButton("Publicar")).not.toBeInTheDocument();
      expect(screen.queryByText(lockNotice)).not.toBeInTheDocument();
    });

    it("offers Publicar to an admin too", async () => {
      // Arrange & Act
      await renderWorkout({ profile: { ...completeProfile, id: "admin-1", role: "ADMIN" } })
        .rendered;
      await loaded();

      // Assert
      expect(statusButton("Publicar")).toBeInTheDocument();
      expect(statusButton("Volver a borrador")).not.toBeInTheDocument();
    });

    it("asks before publishing and sends nothing", async () => {
      // Arrange
      const { mutations, rendered } = renderWorkout();
      await rendered;
      await loaded();
      const user = userEvent.setup();

      // Act
      await user.click(statusButton("Publicar")!);

      // Assert
      const dialog = await screen.findByRole("dialog", { name: "¿Publicar el entrenamiento?" });
      expect(within(dialog).getByText("El atleta podrá verlo y empezarlo.")).toBeInTheDocument();
      expect(within(dialog).getByRole("button", { name: "Cancelar" })).toBeEnabled();
      expect(within(dialog).getByRole("button", { name: "Publicar" })).toBeEnabled();
      expect(mutations.changeStatus).not.toHaveBeenCalled();
    });

    it("asks before moving back to draft and sends nothing", async () => {
      // Arrange
      const { mutations, rendered } = renderWorkout({ getWorkout: withStatus("PUBLISHED") });
      await rendered;
      await loaded();
      const user = userEvent.setup();

      // Act
      await user.click(statusButton("Volver a borrador")!);

      // Assert
      const dialog = await screen.findByRole("dialog", { name: "¿Volver a borrador?" });
      expect(
        within(dialog).getByText("El atleta dejará de verlo hasta que lo vuelvas a publicar."),
      ).toBeInTheDocument();
      expect(within(dialog).getByRole("button", { name: "Volver a borrador" })).toBeEnabled();
      expect(mutations.changeStatus).not.toHaveBeenCalled();
    });

    it("closes the dialog on Cancelar without a request, keeping the status", async () => {
      // Arrange
      const { mutations, rendered } = renderWorkout();
      await rendered;
      await loaded();
      const user = userEvent.setup();
      await user.click(statusButton("Publicar")!);
      const dialog = await screen.findByRole("dialog", { name: "¿Publicar el entrenamiento?" });

      // Act
      await user.click(within(dialog).getByRole("button", { name: "Cancelar" }));

      // Assert
      await waitFor(() => expect(publishDialog()).not.toBeInTheDocument());
      expect(mutations.changeStatus).not.toHaveBeenCalled();
      expect(statusButton("Publicar")).toBeInTheDocument();
    });

    it("publishes once on confirm and then offers Volver a borrador", async () => {
      // Arrange
      const { mutations, rendered } = renderWorkout();
      await rendered;
      await loaded();
      const user = userEvent.setup();
      await user.click(statusButton("Publicar")!);
      const dialog = await screen.findByRole("dialog", { name: "¿Publicar el entrenamiento?" });

      // Act
      await user.click(within(dialog).getByRole("button", { name: "Publicar" }));

      // Assert
      expect(await screen.findByText("Entrenamiento publicado.")).toBeInTheDocument();
      expect(mutations.changeStatus).toHaveBeenCalledTimes(1);
      expect(mutations.changeStatus).toHaveBeenCalledWith({
        workoutId: "w-1",
        status: "PUBLISHED",
      });
      await propertiesShown();
      expect(publishDialog()).not.toBeInTheDocument();
      expect(statusButton("Volver a borrador")).toBeInTheDocument();
      expect(statusButton("Publicar")).not.toBeInTheDocument();
    });

    it("moves a published workout back to draft once on confirm and then offers Publicar", async () => {
      // Arrange
      const { mutations, rendered } = renderWorkout({ getWorkout: withStatus("PUBLISHED") });
      await rendered;
      await loaded();
      const user = userEvent.setup();
      await user.click(statusButton("Volver a borrador")!);
      const dialog = await screen.findByRole("dialog", { name: "¿Volver a borrador?" });

      // Act
      await user.click(within(dialog).getByRole("button", { name: "Volver a borrador" }));

      // Assert
      expect(await screen.findByText("El entrenamiento ha vuelto a borrador.")).toBeInTheDocument();
      await propertiesShown();
      expect(mutations.changeStatus).toHaveBeenCalledTimes(1);
      expect(mutations.changeStatus).toHaveBeenCalledWith({ workoutId: "w-1", status: "DRAFT" });
      expect(statusButton("Publicar")).toBeInTheDocument();
      expect(statusButton("Volver a borrador")).not.toBeInTheDocument();
    });

    it("disables the dialog and Guardar while the change is in progress, and ignores Escape", async () => {
      // Arrange
      const { rendered } = renderWorkout({ changeStatus: () => new Promise(() => {}) });
      await rendered;
      await loaded();
      const user = userEvent.setup();
      await user.click(statusButton("Publicar")!);
      const dialog = await screen.findByRole("dialog", { name: "¿Publicar el entrenamiento?" });
      await user.click(within(dialog).getByRole("button", { name: "Publicar" }));

      // Act
      await user.keyboard("{Escape}");

      // Assert
      expect(publishDialog()).toBeInTheDocument();
      expect(within(dialog).getByRole("button", { name: "Cancelar" })).toBeDisabled();
      expect(within(dialog).getByRole("button", { name: "Publicar" })).toBeDisabled();
      expect(screen.getByRole("button", { name: "Guardar", hidden: true })).toBeDisabled();
    });

    it("disables Publicar while the workout is being saved", async () => {
      // Arrange
      const { rendered } = renderWorkout({ updateWorkout: () => new Promise(() => {}) });
      await rendered;
      await loaded();
      const user = userEvent.setup();

      // Act
      await user.click(screen.getByRole("button", { name: "Guardar" }));

      // Assert
      await waitFor(() => expect(statusButton("Publicar")).toBeDisabled());
    });

    it("shows the error and keeps Volver a borrador when the API refuses the change", async () => {
      // Arrange
      const { rendered } = renderWorkout({
        getWorkout: withStatus("PUBLISHED"),
        changeStatus: async () => fail(new DomainError("VALIDATION", "")),
      });
      await rendered;
      await loaded();
      const user = userEvent.setup();
      await user.click(statusButton("Volver a borrador")!);
      const dialog = await screen.findByRole("dialog", { name: "¿Volver a borrador?" });

      // Act
      await user.click(within(dialog).getByRole("button", { name: "Volver a borrador" }));

      // Assert
      expect(await screen.findByText("Revisa los datos introducidos.")).toBeInTheDocument();
      await propertiesShown();
      expect(backToDraftDialog()).not.toBeInTheDocument();
      expect(statusButton("Volver a borrador")).toBeInTheDocument();
      expect(screen.queryByText("El entrenamiento ha vuelto a borrador.")).not.toBeInTheDocument();
    });

    it("hides the confirmation when a field changes again", async () => {
      // Arrange
      await renderWorkout().rendered;
      await loaded();
      const user = userEvent.setup();
      await user.click(statusButton("Publicar")!);
      const dialog = await screen.findByRole("dialog", { name: "¿Publicar el entrenamiento?" });
      await user.click(within(dialog).getByRole("button", { name: "Publicar" }));
      await screen.findByText("Entrenamiento publicado.");
      await propertiesShown();

      // Act
      await user.type(within(propertiesSection()).getByRole("textbox", { name: "Nombre" }), " B");

      // Assert
      expect(screen.queryByText("Entrenamiento publicado.")).not.toBeInTheDocument();
    });
  });

  describe("review", () => {
    const luis = {
      id: "6a2d3f1e-0e6c-4b64-9d8f-1e7a8d2b3c4d",
      email: "luis@example.com",
      name: "Luis",
      surname: "Pérez",
      role: "TRAINER",
    };
    const admin: UserProfile = { ...completeProfile, id: "admin-1", role: "ADMIN" };
    const trainerProfile: UserProfile = { ...completeProfile, id: luis.id, role: "TRAINER" };
    const heading = () => screen.findByRole("heading", { name: "Revisión del entrenamiento" });

    function setRecord(
      id: string,
      values: Partial<WorkoutByIdExercise["setRecords"][number]> = {},
    ): WorkoutByIdExercise["setRecords"][number] {
      return {
        id,
        type: "Reps",
        reps: null,
        durationSeconds: null,
        weight: null,
        registeredAtUtc: "2026-11-10T10:00:00Z",
        ...values,
      };
    }

    function exerciseWithRecords(
      id: string,
      exerciseId: string,
      effort: string,
      sets: WorkoutExerciseDetail["sets"],
      setRecords: WorkoutByIdExercise["setRecords"],
      extra: Partial<WorkoutExerciseDetail> = {},
    ): WorkoutByIdExercise {
      return { ...workoutExercise(id, exerciseId, effort, sets, extra), setRecords };
    }

    /** Completed: 3 planned, 4 recorded (one discarded). */
    const squatDone = exerciseWithRecords(
      "we-1",
      squat.id,
      "MID",
      [{ count: 3, target: { type: "Reps", value: 10, maxValue: null } }],
      [
        setRecord("r1", { reps: 8, weight: 80 }),
        setRecord("r2", { reps: 8, weight: 85 }),
        setRecord("r3", { reps: 7, weight: 85 }),
        setRecord("r4", { reps: 5, weight: 100 }),
      ],
      { restMinutes: 2 },
    );
    /** In progress: 4 planned, 1 recorded without weight; no rest. */
    const benchStarted = exerciseWithRecords(
      "we-2",
      bench.id,
      "HIGH",
      [
        { count: 2, target: { type: "RepsRange", value: 8, maxValue: 12 } },
        { count: 2, target: { type: "MaxReps", value: null, maxValue: null } },
      ],
      [setRecord("r5", { reps: 10 })],
    );
    /** Pending, without effort. */
    const pressPending = exerciseWithRecords(
      "we-3",
      legPress.id,
      "",
      [{ count: 3, target: { type: "Reps", value: 12, maxValue: null } }],
      [],
    );
    /** In progress: a duration and a minutes target, one timed record. */
    const bikeStarted = exerciseWithRecords(
      "we-4",
      bike.id,
      "LOW",
      [
        { count: 1, target: { type: "Duration", value: 30, maxValue: null } },
        { count: 1, target: { type: "Minutes", value: null, maxValue: null, minutes: 1.5 } },
      ],
      [setRecord("r6", { type: "Duration", durationSeconds: 45 })],
    );
    const started: WorkoutById = {
      ...workout,
      status: "STARTED",
      trainerId: luis.id,
      trainer: luis,
      mesocycleId: "m-1",
      mesocycleWeekNumber: 2,
      mesocycle: {
        id: "m-1",
        name: "Hipertrofia",
        objective: "Ganar masa",
        startDate: "2026-10-26",
        durationInWeeks: 4,
        weeks: [],
      },
      resultsUpdatedAt: "2026-11-13T10:00:00Z",
      exercises: [squatDone, benchStarted, pressPending, bikeStarted],
    };
    const exercisesCatalog = async () => ok([...catalog, bike]);
    const withReview =
      (overrides: Partial<WorkoutById> = {}) =>
      async () =>
        ok({ ...started, ...overrides });
    const cardOf = (name: string) => screen.getByRole("heading", { name, level: 3 }).closest("li")!;
    const statValue = (label: string) => screen.getByText(label).nextElementSibling!.textContent;

    it.each(["STARTED", "COMPLETED", "REVIEWED"])(
      "opens the review of a %s workout",
      async (status) => {
        // Arrange & Act
        await renderWorkout({ getWorkout: withReview({ status }), getExercises: exercisesCatalog })
          .rendered;

        // Assert
        expect(await heading()).toBeInTheDocument();
        expect(
          screen.queryByRole("heading", { name: "Editar entrenamiento" }),
        ).not.toBeInTheDocument();
        expect(screen.queryByRole("button", { name: "Guardar" })).not.toBeInTheDocument();
        expect(screen.queryByRole("button", { name: "Añadir ejercicio" })).not.toBeInTheDocument();
      },
    );

    it.each(["DRAFT", "PUBLISHED", "ARCHIVED"])(
      "opens the editor of a %s workout",
      async (status) => {
        // Arrange & Act
        await renderWorkout({ getWorkout: async () => ok({ ...workout, status }) }).rendered;

        // Assert
        expect(
          await screen.findByRole("heading", { name: "Editar entrenamiento" }),
        ).toBeInTheDocument();
        expect(
          screen.queryByRole("heading", { name: "Revisión del entrenamiento" }),
        ).not.toBeInTheDocument();
      },
    );

    it("shows the review although the catalog fails", async () => {
      // Arrange & Act
      await renderWorkout({
        getWorkout: withReview(),
        getExercises: async () => fail(new DomainError("NETWORK", "")),
      }).rendered;

      // Assert
      expect(await heading()).toBeInTheDocument();
      expect(screen.queryByRole("alert")).not.toBeInTheDocument();
    });

    it("reads the workout once", async () => {
      // Arrange
      const { readWorkout, rendered } = renderWorkout({ getWorkout: withReview() });

      // Act
      await rendered;
      await heading();

      // Assert
      expect(readWorkout).toHaveBeenCalledTimes(1);
    });

    it("shows the title, the subtitle and the status of a placed workout", async () => {
      // Arrange & Act
      await renderWorkout({ getWorkout: withReview() }).rendered;

      // Assert
      await heading();
      expect(screen.getByText("Pierna · Hipertrofia · Semana 2")).toBeInTheDocument();
      expect(screen.getByRole("link", { name: "Volver" })).toHaveAttribute(
        "href",
        "/mesocycle?id=m-1",
      );
      expect(screen.getByText("En curso")).toBeInTheDocument();
    });

    it("shows only the name of a standalone workout", async () => {
      // Arrange & Act
      await renderWorkout({
        getWorkout: withReview({
          status: "COMPLETED",
          exercises: [pressPending],
          mesocycleId: null,
          mesocycleWeekNumber: null,
          mesocycle: null,
        }),
      }).rendered;

      // Assert
      await heading();
      expect(screen.getByText("Pierna")).toBeInTheDocument();
      expect(screen.getByRole("link", { name: "Volver" })).toHaveAttribute(
        "href",
        "/assigned-workouts",
      );
      expect(screen.getByText("Completado", { selector: ".MuiChip-label" })).toBeInTheDocument();
    });

    it("shows a started workout as an outlined red chip with the live icon", async () => {
      // Arrange & Act
      await renderWorkout({ getWorkout: withReview() }).rendered;

      // Assert
      await heading();
      const chip = screen.getByText("En curso").closest(".MuiChip-root")!;
      expect(chip).toHaveClass("MuiChip-outlined", "MuiChip-colorError");
      expect(within(chip as HTMLElement).getByTestId("SensorsOutlinedIcon")).toBeInTheDocument();
      expect(screen.queryByText("En directo")).not.toBeInTheDocument();
    });

    it("shows a completed workout as a plain status chip", async () => {
      // Arrange & Act
      await renderWorkout({
        getWorkout: withReview({ status: "COMPLETED", exercises: [pressPending] }),
      }).rendered;

      // Assert
      await heading();
      const chip = screen
        .getByText("Completado", { selector: ".MuiChip-label" })
        .closest(".MuiChip-root")!;
      expect(chip).not.toHaveClass("MuiChip-colorError");
      expect(
        within(chip as HTMLElement).queryByTestId("SensorsOutlinedIcon"),
      ).not.toBeInTheDocument();
    });

    it("shows the athlete and the start placeholder", async () => {
      // Arrange & Act
      await renderWorkout({ getWorkout: withReview({ startedAt: null }) }).rendered;

      // Assert
      await heading();
      const strip = screen.getByRole("region", { name: "Datos del entrenamiento" });
      expect(within(strip).getByText("AG")).toBeInTheDocument();
      expect(within(strip).getByText("Ana García")).toBeInTheDocument();
      expect(within(strip).getByText("Atleta")).toBeInTheDocument();
      expect(within(strip).getByText("Inicio: —")).toBeInTheDocument();
    });

    it("shows the athlete and the start time", async () => {
      // Arrange
      const startedAt = "2026-11-13T10:42:00Z";
      const expected = new Intl.DateTimeFormat("es", {
        dateStyle: "medium",
        timeStyle: "short",
      }).format(new Date(startedAt));

      // Act
      await renderWorkout({ getWorkout: withReview({ startedAt }) }).rendered;

      // Assert
      await heading();
      const strip = screen.getByRole("region", { name: "Datos del entrenamiento" });
      expect(within(strip).getByText("Ana García")).toBeInTheDocument();
      expect(within(strip).getByText(`Inicio: ${expected}`)).toBeInTheDocument();
    });

    it("shows when the results last changed", async () => {
      // Arrange
      vi.setSystemTime(new Date("2026-11-13T10:00:30Z"));

      // Act
      await renderWorkout({ getWorkout: withReview() }).rendered;

      // Assert
      await heading();
      expect(screen.getByText("Última actualización: hace 30 segundos")).toBeInTheDocument();
    });

    it("shows no last update before any result", async () => {
      // Arrange & Act
      await renderWorkout({ getWorkout: withReview({ resultsUpdatedAt: null }) }).rendered;

      // Assert
      await heading();
      expect(screen.queryByText(/Última actualización/)).not.toBeInTheDocument();
    });

    it("shows the trainer to an admin", async () => {
      // Arrange & Act
      await renderWorkout({ getWorkout: withReview(), profile: admin }).rendered;

      // Assert
      await heading();
      expect(screen.getByText("Luis Pérez")).toBeInTheDocument();
    });

    it("hides the trainer from a trainer", async () => {
      // Arrange & Act
      await renderWorkout({ getWorkout: withReview(), profile: trainerProfile }).rendered;

      // Assert
      await heading();
      expect(screen.queryByText("Luis Pérez")).not.toBeInTheDocument();
    });

    it("shows the session figures, counting only the planned sets", async () => {
      // Arrange & Act
      await renderWorkout({ getWorkout: withReview() }).rendered;

      // Assert
      await heading();
      expect(statValue("Series completadas")).toBe("5 / 12");
      expect(statValue("Ejercicios terminados")).toBe("1 / 4");
      expect(statValue("Volumen registrado")).toBe("1.915 kg");
    });

    describe("elapsed time", () => {
      const now = new Date("2026-11-13T10:30:00Z");

      beforeEach(() => {
        vi.setSystemTime(now);
      });

      afterEach(() => {
        vi.restoreAllMocks();
      });

      it("counts from the start to now for a started workout", async () => {
        // Arrange
        const startedAt = "2026-11-13T10:05:00Z";

        // Act
        await renderWorkout({ getWorkout: withReview({ status: "STARTED", startedAt }) }).rendered;

        // Assert
        await heading();
        expect(statValue("Tiempo transcurrido")).toBe("25 min");
      });

      it.each([
        ["COMPLETED", "2026-11-13T10:00:00Z", "2026-11-13T11:05:00Z", "1 h 05 min"],
        ["REVIEWED", "2026-11-13T10:00:00Z", "2026-11-13T10:40:00Z", "40 min"],
        ["COMPLETED", "2026-11-13T10:00:00Z", "2026-11-13T10:00:30Z", "< 1 min"],
      ])(
        "counts from the start to the completion for a %s workout (%s to %s)",
        async (status, startedAt, completedAt, expected) => {
          // Arrange & Act
          await renderWorkout({
            getWorkout: withReview({ status, startedAt, completedAt }),
          }).rendered;

          // Assert
          await heading();
          expect(statValue("Tiempo transcurrido")).toBe(expected);
        },
      );

      it.each([
        ["without a start", { status: "STARTED", startedAt: null }],
        [
          "without a completion",
          { status: "COMPLETED", startedAt: "2026-11-13T10:00:00Z", completedAt: null },
        ],
        [
          "with a completion before the start",
          {
            status: "COMPLETED",
            startedAt: "2026-11-13T10:00:00Z",
            completedAt: "2026-11-13T09:00:00Z",
          },
        ],
      ])("shows a dash %s", async (_name, overrides) => {
        // Arrange & Act
        await renderWorkout({ getWorkout: withReview(overrides) }).rendered;

        // Assert
        await heading();
        expect(statValue("Tiempo transcurrido")).toBe("—");
      });

      it("counts one more minute when a minute passes on a started workout", async () => {
        // Arrange
        const ticks: (() => void)[] = [];
        const realSetInterval = globalThis.setInterval;
        vi.spyOn(globalThis, "setInterval").mockImplementation(((
          handler: () => void,
          timeout?: number,
        ) => {
          if (timeout === 60_000) {
            ticks.push(handler);
          }
          return realSetInterval(handler, timeout);
        }) as typeof setInterval);
        const getWorkout = vi.fn(
          withReview({ status: "STARTED", startedAt: "2026-11-13T10:05:00Z" }),
        );
        await renderWorkout({ getWorkout }).rendered;
        await heading();

        // Act
        act(() => {
          vi.setSystemTime(new Date(now.getTime() + 60_000));
          ticks.forEach((tick) => tick());
        });

        // Assert
        expect(statValue("Tiempo transcurrido")).toBe("26 min");
        expect(getWorkout).toHaveBeenCalledTimes(1);
      });

      it("keeps a completed workout's time when a minute passes", async () => {
        // Arrange
        const intervals = vi.spyOn(globalThis, "setInterval");
        await renderWorkout({
          getWorkout: withReview({
            status: "COMPLETED",
            startedAt: "2026-11-13T10:00:00Z",
            completedAt: "2026-11-13T10:40:00Z",
          }),
        }).rendered;
        await heading();

        // Act
        act(() => {
          vi.setSystemTime(new Date(now.getTime() + 60_000));
        });

        // Assert
        expect(intervals).not.toHaveBeenCalledWith(expect.any(Function), 60_000);
        expect(statValue("Tiempo transcurrido")).toBe("40 min");
      });
    });

    it("shows the progress of the session", async () => {
      // Arrange & Act
      await renderWorkout({ getWorkout: withReview() }).rendered;

      // Assert
      await heading();
      expect(screen.getByText("42%")).toBeInTheDocument();
      expect(screen.getByText("5 series realizadas")).toBeInTheDocument();
      expect(screen.getByText("7 pendientes")).toBeInTheDocument();
      expect(screen.getByRole("progressbar", { name: "Progreso de la sesión" })).toHaveAttribute(
        "aria-valuenow",
        "42",
      );
    });

    it("shows 0% when nothing is planned", async () => {
      // Arrange & Act
      await renderWorkout({ getWorkout: withReview({ exercises: [] }) }).rendered;

      // Assert
      await heading();
      expect(screen.getByText("0%")).toBeInTheDocument();
    });

    it("selects the exercises tab first, without an expand-all button", async () => {
      // Arrange & Act
      await renderWorkout({ getWorkout: withReview() }).rendered;

      // Assert
      await heading();
      expect(screen.getByRole("tab", { name: "Ejercicios" })).toHaveAttribute(
        "aria-selected",
        "true",
      );
      expect(screen.getByRole("heading", { name: "Sentadilla", level: 3 })).toBeInTheDocument();
      expect(screen.queryByRole("button", { name: "Expandir todo" })).not.toBeInTheDocument();
    });

    it("shows an empty panel in Feedback", async () => {
      // Arrange
      await renderWorkout({ getWorkout: withReview() }).rendered;
      await heading();
      const user = userEvent.setup();

      // Act
      await user.click(screen.getByRole("tab", { name: "Feedback" }));

      // Assert
      expect(screen.getByRole("tab", { name: "Feedback" })).toHaveAttribute(
        "aria-selected",
        "true",
      );
      expect(
        screen.queryByRole("heading", { name: "Sentadilla", level: 3 }),
      ).not.toBeInTheDocument();
      expect(screen.getByRole("tabpanel")).toBeEmptyDOMElement();
    });

    it("lists a card per exercise in workout order, with its labels and state", async () => {
      // Arrange & Act
      await renderWorkout({ getWorkout: withReview() }).rendered;

      // Assert
      await heading();
      const names = screen.getAllByRole("heading", { level: 3 }).map((item) => item.textContent);
      expect(names).toEqual(["Sentadilla", "Press de banca", "Prensa de piernas", "Bicicleta"]);
      expect(within(cardOf("Sentadilla")).getByText("Piernas")).toBeInTheDocument();
      expect(within(cardOf("Sentadilla")).getByText("Cuádriceps")).toBeInTheDocument();
      expect(within(cardOf("Sentadilla")).getByText("Completado")).toBeInTheDocument();
      expect(within(cardOf("Press de banca")).getByText("En progreso")).toBeInTheDocument();
      expect(within(cardOf("Prensa de piernas")).getByText("Pendiente")).toBeInTheDocument();
    });

    it("expands the first exercise in progress and collapses the rest", async () => {
      // Arrange & Act
      await renderWorkout({ getWorkout: withReview() }).rendered;

      // Assert
      await heading();
      expect(screen.getAllByRole("table")).toHaveLength(1);
      expect(within(cardOf("Press de banca")).getByRole("table")).toBeInTheDocument();
    });

    it("expands the first exercise not completed when none is in progress", async () => {
      // Arrange & Act
      await renderWorkout({
        getWorkout: withReview({
          exercises: [squatDone, pressPending, { ...pressPending, id: "we-5" }],
        }),
      }).rendered;

      // Assert
      await heading();
      expect(screen.getAllByRole("table")).toHaveLength(1);
    });

    it("collapses every card when all exercises are completed", async () => {
      // Arrange & Act
      await renderWorkout({ getWorkout: withReview({ exercises: [squatDone] }) }).rendered;

      // Assert
      await heading();
      expect(screen.queryByRole("table")).not.toBeInTheDocument();
    });

    it("expands and collapses a card from its button", async () => {
      // Arrange
      await renderWorkout({ getWorkout: withReview() }).rendered;
      await heading();
      const user = userEvent.setup();

      // Act
      await user.click(screen.getByRole("button", { name: "Expandir Sentadilla" }));

      // Assert
      expect(within(cardOf("Sentadilla")).getByRole("table")).toBeInTheDocument();
      expect(screen.getByRole("button", { name: "Contraer Sentadilla" })).toHaveAttribute(
        "aria-expanded",
        "true",
      );
    });

    it("shows the effort of the exercise above its table", async () => {
      // Arrange
      await renderWorkout({ getWorkout: withReview() }).rendered;
      await heading();
      const user = userEvent.setup();

      // Act
      await user.click(screen.getByRole("button", { name: "Expandir Sentadilla" }));

      // Assert
      expect(within(cardOf("Sentadilla")).getByText("Esfuerzo: RIR 3")).toBeInTheDocument();
    });

    it("shows no effort line when the exercise has none", async () => {
      // Arrange
      await renderWorkout({ getWorkout: withReview() }).rendered;
      await heading();
      const user = userEvent.setup();

      // Act
      await user.click(screen.getByRole("button", { name: "Expandir Prensa de piernas" }));

      // Assert
      expect(within(cardOf("Prensa de piernas")).getByRole("table")).toBeInTheDocument();
      expect(within(cardOf("Prensa de piernas")).queryByText(/Esfuerzo/)).not.toBeInTheDocument();
    });

    it("pairs each planned set with its record and drops the discarded one", async () => {
      // Arrange
      await renderWorkout({ getWorkout: withReview() }).rendered;
      await heading();
      const user = userEvent.setup();

      // Act
      await user.click(screen.getByRole("button", { name: "Expandir Sentadilla" }));

      // Assert
      const table = within(cardOf("Sentadilla")).getByRole("table");
      expect(within(table).getAllByRole("row")).toHaveLength(4);
      expect(within(table).getAllByText("10 reps")).toHaveLength(3);
      expect(within(table).getByText("8 × 80 kg")).toBeInTheDocument();
      expect(within(table).getByText("8 × 85 kg")).toBeInTheDocument();
      expect(within(table).getByText("7 × 85 kg")).toBeInTheDocument();
      expect(within(table).queryByText("5 × 100 kg")).not.toBeInTheDocument();
    });

    it("shows the targets without weight, the records without weight and the pending sets", async () => {
      // Arrange & Act
      await renderWorkout({ getWorkout: withReview() }).rendered;

      // Assert
      await heading();
      const table = within(cardOf("Press de banca")).getByRole("table");
      expect(within(table).getAllByText("8–12 reps")).toHaveLength(2);
      expect(within(table).getAllByText("Máx. reps")).toHaveLength(2);
      expect(within(table).getByText("10 reps")).toBeInTheDocument();
      expect(within(table).getAllByText("Pendiente")).toHaveLength(3);
    });

    it("shows duration and minutes targets and a timed record", async () => {
      // Arrange
      await renderWorkout({ getWorkout: withReview(), getExercises: exercisesCatalog }).rendered;
      await heading();
      const user = userEvent.setup();

      // Act
      await user.click(screen.getByRole("button", { name: "Expandir Bicicleta" }));

      // Assert
      const table = within(cardOf("Bicicleta")).getByRole("table");
      expect(within(table).getByText("30 s")).toBeInTheDocument();
      expect(within(table).getByText("1:30 min")).toBeInTheDocument();
      expect(within(table).getByText("45 s")).toBeInTheDocument();
    });

    it("shows the rest of the exercise on every row", async () => {
      // Arrange
      await renderWorkout({ getWorkout: withReview() }).rendered;
      await heading();
      const user = userEvent.setup();

      // Act
      await user.click(screen.getByRole("button", { name: "Expandir Sentadilla" }));

      // Assert
      expect(within(cardOf("Sentadilla")).getAllByText("2:00")).toHaveLength(3);
      expect(within(cardOf("Press de banca")).getAllByText("—")).toHaveLength(4);
    });

    it("offers no action that changes the workout", async () => {
      // Arrange & Act
      await renderWorkout({ getWorkout: withReview({ status: "COMPLETED" }) }).rendered;

      // Assert
      await heading();
      for (const name of ["Guardar", "Publicar", "Volver a borrador", "Añadir ejercicio"]) {
        expect(screen.queryByRole("button", { name })).not.toBeInTheDocument();
      }
    });

    it("shows Guardar como plantilla enabled", async () => {
      // Arrange & Act
      await renderWorkout({ getWorkout: withReview({ status: "COMPLETED" }) }).rendered;

      // Assert
      await heading();
      expect(screen.getByRole("button", { name: "Guardar como plantilla" })).toBeEnabled();
    });

    it("creates one template from the workout without updating it", async () => {
      // Arrange
      const { mutations, rendered } = renderWorkout({ getWorkout: withReview() });
      await rendered;
      await heading();
      const user = userEvent.setup();

      // Act
      await user.click(screen.getByRole("button", { name: "Guardar como plantilla" }));

      // Assert
      expect(await screen.findByText("Plantilla creada.")).toBeInTheDocument();
      expect(mutations.saveAsTemplate).toHaveBeenCalledTimes(1);
      expect(mutations.saveAsTemplate).toHaveBeenCalledWith({ workoutId: "w-1" });
      expect(mutations.update).not.toHaveBeenCalled();
      expect(screen.getByRole("link", { name: "Ver plantilla" })).toHaveAttribute(
        "href",
        "/workout-template?id=wt-9",
      );
    });

    it("shows the error when the template is refused", async () => {
      // Arrange
      const { rendered } = renderWorkout({
        getWorkout: withReview(),
        saveAsTemplate: async () => fail(new DomainError("VALIDATION", "")),
      });
      await rendered;
      await heading();
      const user = userEvent.setup();

      // Act
      await user.click(screen.getByRole("button", { name: "Guardar como plantilla" }));

      // Assert
      expect(await screen.findByText("Revisa los datos introducidos.")).toBeInTheDocument();
      expect(screen.queryByText("Plantilla creada.")).not.toBeInTheDocument();
    });

    describe("metrics tab", () => {
      const curlPending = exerciseWithRecords(
        "we-5",
        curl.id,
        "MID",
        [{ count: 3, target: { type: "Reps", value: 12, maxValue: null } }],
        [],
      );
      const metricsWorkout: Partial<WorkoutById> = {
        status: "COMPLETED",
        startedAt: "2026-11-13T10:00:00Z",
        completedAt: "2026-11-13T11:12:00Z",
        exercises: [squatDone, benchStarted, pressPending, bikeStarted, curlPending],
      };
      const previousTopSet = { reps: 6, durationSeconds: null, weight: 120 };

      /** The summary the API answers: the squat, bench and press are weighted; the bike and the curl are not. */
      function summaryOf(exercises: WorkoutSummary["exercises"] = []): WorkoutSummary {
        const entry = (
          workoutExerciseId: string,
          exerciseId: string,
          volume: number | null,
          setsInTarget: { inTarget: number; evaluated: number },
          topSet: WorkoutSummary["exercises"][number]["topSet"],
          previous: WorkoutSummary["exercises"][number]["previous"],
        ) => ({ workoutExerciseId, exerciseId, volume, setsInTarget, topSet, previous });

        return {
          workoutId: "w-1",
          setsInTarget: { inTarget: 27, evaluated: 29 },
          volume: 16913.4,
          exercises: exercises.length
            ? exercises
            : [
                entry(
                  "we-1",
                  squat.id,
                  3190,
                  { inTarget: 3, evaluated: 3 },
                  { reps: 7, durationSeconds: null, weight: 130 },
                  { workoutId: "w-0", workoutDate: "2026-10-27", topSet: previousTopSet },
                ),
                entry(
                  "we-2",
                  bench.id,
                  1200,
                  { inTarget: 1, evaluated: 2 },
                  { reps: 10, durationSeconds: null, weight: 40 },
                  null,
                ),
                entry(
                  "we-3",
                  legPress.id,
                  2400,
                  { inTarget: 3, evaluated: 3 },
                  { reps: 12, durationSeconds: null, weight: 100 },
                  {
                    workoutId: "w-0",
                    workoutDate: "2026-10-27",
                    topSet: { reps: 12, durationSeconds: null, weight: 100 },
                  },
                ),
                entry("we-4", bike.id, null, { inTarget: 0, evaluated: 0 }, null, null),
                entry("we-5", curl.id, null, { inTarget: 0, evaluated: 0 }, null, null),
              ],
        };
      }
      const withSummary = async () => ok(summaryOf());

      const sessionOf = (
        workoutDate: string,
        maxWeight: number | null,
        volume: number | null,
      ): ExerciseHistorySession => ({
        workoutId: `w-${workoutDate}`,
        workoutDate,
        maxWeight,
        volume,
        estimatedOneRepMax: null,
      });
      /** Newest first, as the API answers; the oldest has no max weight. */
      const history = [
        sessionOf("2026-11-12", 130, 3190),
        sessionOf("2026-10-27", 120, 2880),
        sessionOf("2026-10-13", null, 2500),
      ];
      const withHistory = async () => ok(history);

      async function openMetrics(options: Parameters<typeof renderWorkout>[0] = {}) {
        const rendered = renderWorkout({
          getWorkout: withReview(metricsWorkout),
          getSummary: withSummary,
          getHistory: withHistory,
          ...options,
        });
        await rendered.rendered;
        await heading();
        const user = userEvent.setup();
        await user.click(screen.getByRole("tab", { name: "Resumen y métricas" }));
        return { ...rendered, user };
      }

      const panel = () => screen.getByRole("tabpanel");
      const resultsHeading = () =>
        screen.findByRole("heading", { name: "Resultados de la sesión" });
      const cardOfHeading = (name: string) =>
        within(panel()).getByRole("heading", { name }).closest(".MuiPaper-root") as HTMLElement;
      /** The value and the caption under a stat label of the results card. */
      const stat = (label: string) => {
        const value = within(cardOfHeading("Resultados de la sesión")).getByText(
          label,
        ).nextElementSibling!;
        return { value: value.textContent, caption: value.nextElementSibling?.textContent ?? null };
      };
      const tableRows = (card: HTMLElement) =>
        within(card)
          .getAllByRole("row")
          .slice(1)
          .map((row) =>
            within(row)
              .getAllByRole("cell")
              .map((cell) => cell.textContent),
          );
      const chart = (testId: string) => ({
        labels: screen.getByTestId(testId).getAttribute("data-labels"),
        values: screen.getByTestId(testId).getAttribute("data-values"),
      });

      it("does not read the summary while the tab is closed", async () => {
        // Arrange
        const { readSummary } = renderWorkout({
          getWorkout: withReview(metricsWorkout),
          getSummary: withSummary,
        });

        // Act
        await heading();

        // Assert
        expect(readSummary).not.toHaveBeenCalled();
      });

      it("reads the summary once on first opening and not when coming back", async () => {
        // Arrange
        const { readSummary, user } = await openMetrics();
        await resultsHeading();

        // Act
        await user.click(screen.getByRole("tab", { name: "Ejercicios" }));
        await user.click(screen.getByRole("tab", { name: "Resumen y métricas" }));

        // Assert
        expect(await resultsHeading()).toBeInTheDocument();
        expect(readSummary).toHaveBeenCalledTimes(1);
        expect(readSummary).toHaveBeenCalledWith({ workoutId: "w-1" });
      });

      it("shows a load error with retry, and the content after a retry that succeeds", async () => {
        // Arrange
        let calls = 0;
        const { user } = await openMetrics({
          getSummary: async () =>
            ++calls === 1 ? fail(new DomainError("NETWORK", "offline")) : ok(summaryOf()),
        });
        const retry = await screen.findByRole("button", { name: "Reintentar" });

        // Act
        await user.click(retry);

        // Assert
        expect(await resultsHeading()).toBeInTheDocument();
        expect(screen.queryByRole("button", { name: "Reintentar" })).not.toBeInTheDocument();
      });

      it("shows the five result cards with their percentages", async () => {
        // Arrange & Act
        await openMetrics();
        await resultsHeading();

        // Assert
        expect(
          screen.getByText("Cumplimiento, volumen e intensidad del entrenamiento"),
        ).toBeInTheDocument();
        expect(stat("Series completadas")).toEqual({ value: "5 / 15", caption: "33%" });
        expect(stat("Ejercicios completados")).toEqual({ value: "1 / 5", caption: "20%" });
        expect(stat("Series en rango objetivo")).toEqual({ value: "27 / 29", caption: "93%" });
        expect(stat("Volumen de fuerza")).toEqual({ value: "16.913,4 kg", caption: null });
        expect(stat("Duración")).toEqual({ value: "1 h 12 min", caption: null });
      });

      it("shows a dash for nothing evaluated, no volume and no start", async () => {
        // Arrange & Act
        await openMetrics({
          getWorkout: withReview({ ...metricsWorkout, startedAt: null }),
          getSummary: async () =>
            ok({
              ...summaryOf(),
              setsInTarget: { inTarget: 0, evaluated: 0 },
              volume: null,
            }),
        });
        await resultsHeading();

        // Assert
        expect(stat("Series en rango objetivo")).toEqual({ value: "—", caption: null });
        expect(stat("Volumen de fuerza")).toEqual({ value: "—", caption: null });
        expect(stat("Duración")).toEqual({ value: "—", caption: null });
      });

      it("lists the weighted exercises only, in workout order, against their previous top set", async () => {
        // Arrange & Act
        await openMetrics();
        await resultsHeading();

        // Assert
        expect(tableRows(cardOfHeading("Comparativa por ejercicio"))).toEqual([
          ["1Sentadilla", "6 × 120 kg", "7 × 130 kg", "▲ +8,3 %", "3.190 kg", "100%"],
          ["2Press de banca", "—", "10 × 40 kg", "—", "1.200 kg", "50%"],
          ["3Prensa de piernas", "12 × 100 kg", "12 × 100 kg", "=", "2.400 kg", "100%"],
        ]);
      });

      it("shows a fall in weight and no compliance when nothing was evaluated", async () => {
        // Arrange
        const [first] = summaryOf().exercises;
        const lighter = {
          ...first!,
          topSet: { reps: 7, durationSeconds: null, weight: 95 },
          previous: {
            workoutId: "w-0",
            workoutDate: "2026-10-27",
            topSet: { reps: 7, durationSeconds: null, weight: 100 },
          },
          setsInTarget: { inTarget: 0, evaluated: 0 },
        };

        // Act
        await openMetrics({ getSummary: async () => ok(summaryOf([lighter])) });
        await resultsHeading();

        // Assert
        expect(tableRows(cardOfHeading("Comparativa por ejercicio"))).toEqual([
          ["1Sentadilla", "7 × 100 kg", "7 × 95 kg", "▼ -5,0 %", "3.190 kg", "—"],
        ]);
      });

      it("shows the trained muscles only, most sets first", async () => {
        // Arrange & Act
        await openMetrics();
        await resultsHeading();

        // Assert
        const card = cardOfHeading("Distribución de series por grupo muscular");
        expect(within(card).getByText("Series efectivas de la sesión")).toBeInTheDocument();
        expect(
          within(card)
            .getAllByRole("progressbar")
            .map((bar) => bar.getAttribute("aria-label")),
        ).toEqual(["Cuádriceps: 4 series", "Pecho: 1 series"]);
        expect(within(card).queryByText("Bíceps")).not.toBeInTheDocument();
      });

      it("shows the three sample observations marked as a preview", async () => {
        // Arrange & Act
        await openMetrics();
        await resultsHeading();

        // Assert
        const card = cardOfHeading("Observaciones de la sesión");
        expect(within(card).getByText("Vista previa")).toBeInTheDocument();
        expect(
          within(card)
            .getAllByRole("heading", { level: 3 })
            .map((item) => item.textContent),
        ).toEqual(["Progresión destacada", "Buen cumplimiento", "Revisar"]);
      });

      it("charts the athlete's history of the first weighted exercise, oldest first", async () => {
        // Arrange & Act
        const { readHistory } = await openMetrics();
        await resultsHeading();

        // Assert
        await waitFor(() =>
          expect(chart("line-chart")).toEqual({ labels: "27 oct|12 nov", values: "120|130" }),
        );
        expect(chart("bar-chart")).toEqual({
          labels: "13 oct|27 oct|12 nov",
          values: "2500|2880|3190",
        });
        expect(readHistory).toHaveBeenCalledTimes(1);
        expect(readHistory).toHaveBeenCalledWith({
          userId: ana.id,
          exerciseId: squat.id,
          limit: 5,
        });
      });

      it("reads the history again when another exercise is selected, in both cards", async () => {
        // Arrange
        const { readHistory, user } = await openMetrics();
        await resultsHeading();
        await waitFor(() => expect(screen.getByTestId("line-chart")).toBeInTheDocument());
        const [loadSelector] = screen.getAllByRole("combobox", { name: "Ejercicio" });

        // Act
        await user.click(loadSelector!);
        await user.click(screen.getByRole("option", { name: "Press de banca" }));

        // Assert
        await waitFor(() =>
          expect(readHistory).toHaveBeenLastCalledWith({
            userId: ana.id,
            exerciseId: bench.id,
            limit: 5,
          }),
        );
        const selectors = screen.getAllByRole("combobox", { name: "Ejercicio" });
        expect(selectors.map((item) => item.textContent)).toEqual([
          "Press de banca",
          "Press de banca",
        ]);
      });

      it("shows that there is no data when the history is empty", async () => {
        // Arrange & Act
        await openMetrics({ getHistory: async () => ok([]) });
        await resultsHeading();

        // Assert
        await waitFor(() =>
          expect(screen.getAllByText("Sin datos de este ejercicio")).toHaveLength(2),
        );
        expect(screen.queryByTestId("line-chart")).not.toBeInTheDocument();
        expect(screen.queryByTestId("bar-chart")).not.toBeInTheDocument();
      });

      it("shows a load error with retry in both evolution cards while the rest stays", async () => {
        // Arrange
        let calls = 0;
        const { user } = await openMetrics({
          getHistory: async () =>
            ++calls === 1 ? fail(new DomainError("FORBIDDEN", "")) : ok(history),
        });
        await resultsHeading();
        const retries = await screen.findAllByRole("button", { name: "Reintentar" });

        // Act
        await user.click(retries[0]!);

        // Assert
        expect(retries).toHaveLength(2);
        expect(tableRows(cardOfHeading("Comparativa por ejercicio"))).toHaveLength(3);
        await waitFor(() => expect(screen.getByTestId("line-chart")).toBeInTheDocument());
        expect(screen.queryByRole("button", { name: "Reintentar" })).not.toBeInTheDocument();
      });

      it("reads no history when no exercise is weighted", async () => {
        // Arrange & Act
        const { readHistory } = await openMetrics({
          getSummary: async () =>
            ok({
              ...summaryOf(),
              exercises: summaryOf().exercises.map((entry) => ({ ...entry, volume: null })),
            }),
        });
        await resultsHeading();

        // Assert
        expect(screen.getAllByText("Sin datos de este ejercicio")).toHaveLength(2);
        expect(screen.getByText("Ningún ejercicio con peso en esta sesión")).toBeInTheDocument();
        expect(screen.queryByRole("combobox", { name: "Ejercicio" })).not.toBeInTheDocument();
        expect(readHistory).not.toHaveBeenCalled();
      });

      it.each([
        ["ADMIN", admin],
        ["TRAINER", trainerProfile],
      ])("shows the same tab to a %s", async (_role, profile) => {
        // Arrange & Act
        await openMetrics({ profile });
        await resultsHeading();

        // Assert
        expect(stat("Volumen de fuerza").value).toBe("16.913,4 kg");
        expect(tableRows(cardOfHeading("Comparativa por ejercicio"))).toHaveLength(3);
        expect(within(panel()).getByText("Vista previa")).toBeInTheDocument();
      });

      it("reads in English", async () => {
        // Arrange
        await openMetrics();
        await resultsHeading();

        // Act
        await act(() => i18n.changeLanguage("en"));

        // Assert
        expect(screen.getByRole("heading", { name: "Session results" })).toBeInTheDocument();
        expect(screen.getByRole("heading", { name: "Exercise comparison" })).toBeInTheDocument();
        expect(screen.getByRole("heading", { name: "Sets per muscle group" })).toBeInTheDocument();
        expect(
          within(cardOfHeading("Session results")).getByText("Strength volume").nextElementSibling,
        ).toHaveTextContent("16,913.4 kg");
        expect(
          within(cardOfHeading("Sets per muscle group"))
            .getAllByRole("progressbar")
            .map((bar) => bar.getAttribute("aria-label")),
        ).toEqual(["Quadriceps: 4 sets", "Chest: 1 sets"]);
      });
    });

    it("reads in English", async () => {
      // Arrange
      await renderWorkout({ getWorkout: withReview() }).rendered;
      await heading();

      // Act
      await act(() => i18n.changeLanguage("en"));

      // Assert
      expect(screen.getByRole("heading", { name: "Workout review" })).toBeInTheDocument();
      expect(screen.getByText("Sets completed")).toBeInTheDocument();
      expect(screen.getByText("Exercises finished")).toBeInTheDocument();
      expect(screen.getByText("Volume recorded")).toBeInTheDocument();
      expect(screen.getByText("Session progress")).toBeInTheDocument();
      expect(screen.getByRole("tab", { name: "Exercises" })).toBeInTheDocument();
      expect(screen.getByRole("tab", { name: "Summary and metrics" })).toBeInTheDocument();
      expect(screen.getByRole("tab", { name: "Feedback" })).toBeInTheDocument();
    });
  });
});
