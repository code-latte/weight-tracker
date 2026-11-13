import { act, fireEvent, screen, waitFor, within } from "@testing-library/react-native";
import { AppState, Linking } from "react-native";
import { DomainError, fail, ok } from "@weight-tracker/core/domain";
import type {
  AddSetRecordInput,
  Result,
  SetRecord,
  Exercise,
  UpdateSetRecordInput,
  UserProfile,
  WorkoutById,
  WorkoutByIdExercise,
  WorkoutDetail,
} from "@weight-tracker/core/domain";
import i18n from "@/infrastructure/i18n";
import WorkoutExerciseView from "@/presentation/workouts/views/workout-exercise-view";
import renderWithProviders from "../../../support/render-with-providers";

const mockBack = jest.fn();
const mockSetParams = jest.fn();
let mockParams = { id: "wk1", exerciseId: "we1" };
let mockUpdateRoute: ((params: Partial<typeof mockParams>) => void) | null = null;
let mockSetKeyboardVisible: ((visible: boolean) => void) | null = null;

// The route keeps its own copy of the params, so `setParams` switches the exercise the view shows.
jest.mock("expo-router", () => ({
  router: {
    back: () => mockBack(),
    setParams: (params: Partial<typeof mockParams>) => {
      mockSetParams(params);
      mockUpdateRoute?.(params);
    },
  },
  useLocalSearchParams: () => {
    const [params, setParams] = jest.requireActual("react").useState(mockParams);
    mockUpdateRoute = (next) =>
      setParams((current: typeof mockParams) => ({ ...current, ...next }));

    return params;
  },
}));

// The keyboard is not on screen under Jest: a test shows and hides it through `mockSetKeyboardVisible`.
jest.mock("@/presentation/common/hooks/use-keyboard-visible", () => ({
  __esModule: true,
  default: () => {
    const [visible, setVisible] = jest.requireActual("react").useState(false);
    mockSetKeyboardVisible = setVisible;

    return visible;
  },
}));

// Paper's icons load their font through expo-font, which has no assets under Jest.
jest.mock("expo-font", () => ({
  ...jest.requireActual("expo-font"),
  isLoaded: () => true,
  loadAsync: async () => {},
}));

const athlete: UserProfile = {
  id: "2c3d4e5f-6a7b-4c8d-9e0f-1a2b3c4d5e6f",
  email: "ana@example.com",
  name: "Ana",
  surname: "López",
  role: "USER",
};

const PROGRESS_LABELS = /^(Completado|En curso|Sin empezar)$/;

const target = (
  type: string,
  value: number | null,
  maxValue: number | null,
  minutes: number | null,
) => ({
  type,
  value,
  maxValue,
  minutes,
});
const reps = (value: number) => target("Reps", value, null, null);

function recordOf(id: string, overrides: Partial<SetRecord> = {}): SetRecord {
  return {
    id,
    type: "Reps",
    reps: 10,
    durationSeconds: null,
    weight: 60,
    registeredAtUtc: "2026-11-10T10:00:00Z",
    ...overrides,
  };
}

function exerciseOf(id: string, overrides: Partial<WorkoutByIdExercise> = {}): WorkoutByIdExercise {
  return {
    id,
    exerciseId: `ex-${id}`,
    translations: [
      { languageCode: "es", name: "Sentadilla" },
      { languageCode: "en", name: "Squat" },
    ],
    videoUrl: null,
    muscle: "QUADS",
    bodyPart: "LEGS",
    type: "STRENGTH",
    effort: "MID",
    perSide: false,
    restMinutes: 1.5,
    sets: [
      { count: 3, target: reps(10) },
      { count: 1, target: reps(8) },
    ],
    superSeriesId: null,
    setRecords: [],
    ...overrides,
  } as WorkoutByIdExercise;
}

function workoutOf(
  exercises: WorkoutByIdExercise[],
  overrides: Partial<WorkoutById> = {},
): WorkoutById {
  return {
    id: "wk1",
    name: "Pierna A",
    description: null,
    workoutDate: "2026-11-10",
    userId: athlete.id,
    trainerId: null,
    status: "STARTED",
    exercises,
    mesocycleId: null,
    mesocycleWeekNumber: null,
    completedAt: null,
    reviewedAt: null,
    reviewedBy: null,
    resultsUpdatedAt: null,
    athlete,
    trainer: null,
    mesocycle: null,
    ...overrides,
  };
}

/** The workout's three exercises as the navigator shows them: done, in progress, not started. */
const threeExercises = () => [
  exerciseOf("we1", {
    translations: [{ languageCode: "es", name: "Press inclinado" }],
    sets: [{ count: 1, target: reps(10) }],
    setRecords: [recordOf("r1")],
  }),
  exerciseOf("we2", {
    translations: [{ languageCode: "es", name: "Jalón al pecho" }],
    sets: [{ count: 2, target: reps(10) }],
    setRecords: [recordOf("r2")],
  }),
  exerciseOf("we3", { translations: [{ languageCode: "es", name: "Remo" }] }),
];

/** The catalog: LEGS has two muscles, so an exercise of it shows its muscle too. */
const catalog = [
  { bodyPart: "LEGS", muscle: "QUADS" },
  { bodyPart: "LEGS", muscle: "HAMSTRINGS" },
] as Exercise[];

interface SetupOptions {
  workout?: WorkoutById;
  catalogOf?: Exercise[];
  workoutResult?: () => Promise<Result<WorkoutById, DomainError>>;
  addResult?: () => Promise<Result<WorkoutDetail, DomainError>>;
  updateResult?: () => Promise<Result<WorkoutDetail, DomainError>>;
}

function setup({
  workout,
  catalogOf = catalog,
  workoutResult,
  addResult,
  updateResult,
}: SetupOptions = {}) {
  const current = workout ?? workoutOf([exerciseOf("we1")]);
  const getWorkout = jest.fn(workoutResult ?? (async () => ok(current)));
  const addSetRecord = jest.fn<Promise<Result<WorkoutDetail, DomainError>>, [AddSetRecordInput]>(
    addResult ?? (async () => ok(current as unknown as WorkoutDetail)),
  );
  const updateSetRecord = jest.fn<
    Promise<Result<WorkoutDetail, DomainError>>,
    [UpdateSetRecordInput]
  >(updateResult ?? (async () => ok(current as unknown as WorkoutDetail)));
  const dependencies = {
    getWorkoutByIdUseCase: () => ({ execute: getWorkout }),
    addSetRecordUseCase: () => ({ execute: addSetRecord }),
    updateSetRecordUseCase: () => ({ execute: updateSetRecord }),
    getExercisesUseCase: () => ({ execute: async () => ok(catalogOf) }),
  };

  return { getWorkout, addSetRecord, updateSetRecord, dependencies };
}

async function renderExercise(options: SetupOptions = {}) {
  const fakes = setup(options);
  await renderWithProviders(<WorkoutExerciseView />, fakes.dependencies);

  return fakes;
}

describe("WorkoutExerciseView", () => {
  beforeEach(() => {
    mockBack.mockClear();
    mockSetParams.mockClear();
    mockParams = { id: "wk1", exerciseId: "we1" };
  });

  it("reads the workout once and shows the exercise of the route", async () => {
    // Arrange & Act
    const { getWorkout } = await renderExercise();

    // Assert
    expect(await screen.findByRole("heading", { name: "Sentadilla" })).toBeOnTheScreen();
    expect(getWorkout).toHaveBeenCalledTimes(1);
    expect(getWorkout).toHaveBeenCalledWith({ id: "wk1" });
  });

  it("shows a loading indicator and the back button while the workout is read", async () => {
    // Arrange & Act
    await renderExercise({ workoutResult: () => new Promise(() => {}) });

    // Assert
    expect(screen.getAllByRole("progressbar").length).toBeGreaterThan(0);
    expect(screen.getByRole("button", { name: "Volver" })).toBeOnTheScreen();
    expect(screen.queryByTestId("exercise-sets")).not.toBeOnTheScreen();
  });

  it("shows the workout name, the exercise name and its English name below it", async () => {
    // Arrange & Act
    await renderExercise();

    // Assert
    expect(await screen.findByRole("heading", { name: "Sentadilla" })).toBeOnTheScreen();
    expect(screen.getByText("Pierna A")).toBeOnTheScreen();
    expect(screen.getByText("Squat")).toBeOnTheScreen();
  });

  it("shows the body part and the muscle chips in the header, below the subtitle", async () => {
    // Arrange & Act
    await renderExercise({
      workout: workoutOf([exerciseOf("we1", { bodyPart: "LEGS", muscle: "QUADRICEPS" } as never)]),
    });

    // Assert
    await screen.findByRole("heading", { name: "Sentadilla" });
    expect(await screen.findByText("Piernas")).toBeOnTheScreen();
    expect(screen.getByText("Cuádriceps")).toBeOnTheScreen();
  });

  it("shows only the body part when it has a single muscle in the catalog", async () => {
    // Arrange & Act
    await renderExercise({
      workout: workoutOf([exerciseOf("we1", { bodyPart: "CHEST", muscle: "CHEST" } as never)]),
      catalogOf: [{ bodyPart: "CHEST", muscle: "CHEST" }] as Exercise[],
    });

    // Assert
    await screen.findByRole("heading", { name: "Sentadilla" });
    await waitFor(() => expect(screen.getAllByText("Pecho")).toHaveLength(1));
  });

  it("shows no subtitle when the exercise has no English name", async () => {
    // Arrange & Act
    await renderExercise({
      workout: workoutOf([
        exerciseOf("we1", { translations: [{ languageCode: "es", name: "Sentadilla" }] }),
      ]),
    });

    // Assert
    await screen.findByRole("heading", { name: "Sentadilla" });
    expect(screen.queryByText("Squat")).not.toBeOnTheScreen();
  });

  it("shows the English name once, as the title, when English is active", async () => {
    // Arrange
    await renderExercise();
    await screen.findByRole("heading", { name: "Sentadilla" });

    // Act
    await act(async () => {
      await i18n.changeLanguage("en");
    });

    // Assert
    expect(screen.getByRole("heading", { name: "Squat" })).toBeOnTheScreen();
    // The title and the navigator's item: no subtitle on top of them.
    expect(screen.getAllByText("Squat")).toHaveLength(2);
  });

  it("opens the video when its link is pressed, and shows no link without one", async () => {
    // Arrange
    const openURL = jest.spyOn(Linking, "openURL").mockResolvedValue(undefined);
    await renderExercise({
      workout: workoutOf([exerciseOf("we1", { videoUrl: "https://v.example/1" })]),
    });
    await screen.findByRole("heading", { name: "Sentadilla" });

    // Act
    await fireEvent.press(screen.getByRole("link", { name: "Ver vídeo" }));

    // Assert
    expect(openURL).toHaveBeenCalledWith("https://v.example/1");
    openURL.mockRestore();
  });

  it("shows no video link for an exercise without a video", async () => {
    // Arrange & Act
    await renderExercise();

    // Assert
    await screen.findByRole("heading", { name: "Sentadilla" });
    expect(screen.queryByRole("link", { name: "Ver vídeo" })).not.toBeOnTheScreen();
  });

  it("goes back when the back button is pressed", async () => {
    // Arrange
    await renderExercise();
    await screen.findByRole("heading", { name: "Sentadilla" });

    // Act
    await fireEvent.press(screen.getByRole("button", { name: "Volver" }));

    // Assert
    expect(mockBack).toHaveBeenCalledTimes(1);
  });

  it("lists every exercise in the navigator, in order, with its progress, and marks the current one", async () => {
    // Arrange
    mockParams = { id: "wk1", exerciseId: "we2" };

    // Act
    await renderExercise({ workout: workoutOf(threeExercises()) });

    // Assert
    await screen.findByRole("heading", { name: "Jalón al pecho" });
    const labels = screen
      .getAllByRole("image")
      .map((element) => element.props.accessibilityLabel as string)
      .filter((label) => PROGRESS_LABELS.test(label));
    expect(labels).toEqual(["Completado", "En curso", "Sin empezar"]);
    expect(
      screen.getByRole("button", { name: /Jalón al pecho/, selected: true }),
    ).toBeOnTheScreen();
    expect(
      screen.getByRole("button", { name: /Press inclinado/, selected: false }),
    ).toBeOnTheScreen();
    expect(screen.getByRole("button", { name: /Remo/, selected: false })).toBeOnTheScreen();
  });

  it("swaps the screen's exercise for the one whose item is pressed, without navigating", async () => {
    // Arrange
    await renderExercise({ workout: workoutOf(threeExercises()) });
    await screen.findByRole("heading", { name: "Press inclinado" });

    // Act
    await fireEvent.press(screen.getByRole("button", { name: /Remo/ }));

    // Assert
    expect(mockSetParams).toHaveBeenCalledTimes(1);
    expect(mockSetParams).toHaveBeenCalledWith({ exerciseId: "we3" });
    expect(mockBack).not.toHaveBeenCalled();
  });

  it("does nothing when the item of the exercise on screen is pressed", async () => {
    // Arrange
    await renderExercise({ workout: workoutOf(threeExercises()) });
    await screen.findByRole("heading", { name: "Press inclinado" });

    // Act
    await fireEvent.press(screen.getByRole("button", { name: /Press inclinado/ }));

    // Assert
    expect(mockSetParams).not.toHaveBeenCalled();
  });

  it("expands the prescriptions into numbered rows", async () => {
    // Arrange & Act
    await renderExercise();

    // Assert
    await screen.findByTestId("exercise-sets");
    for (const number of ["1", "2", "3", "4"]) {
      expect(screen.getByText(number)).toBeOnTheScreen();
    }
    expect(screen.getAllByText("10 reps")).toHaveLength(3);
    expect(screen.getAllByText("8 reps")).toHaveLength(1);
    expect(screen.queryByText("5")).not.toBeOnTheScreen();
  });

  it("shows each target as text", async () => {
    // Arrange
    const sets = [
      { count: 1, target: reps(10) },
      { count: 1, target: target("RepsRange", 8, 12, null) },
      { count: 1, target: target("MaxReps", null, null, null) },
      { count: 1, target: target("Duration", 45, null, null) },
      { count: 1, target: target("Minutes", null, null, 2) },
    ];

    // Act
    await renderExercise({ workout: workoutOf([exerciseOf("we1", { sets })]) });

    // Assert
    await screen.findByTestId("exercise-sets");
    for (const text of ["10 reps", "8–12 reps", "máx. reps", "45 s", "2 min"]) {
      expect(screen.getByText(text)).toBeOnTheScreen();
    }
  });

  it("offers the inputs each target asks for, and a save button on every row", async () => {
    // Arrange
    const sets = [
      { count: 1, target: reps(10) },
      { count: 1, target: target("RepsRange", 8, 12, null) },
      { count: 1, target: target("Duration", 45, null, null) },
    ];

    // Act
    await renderExercise({ workout: workoutOf([exerciseOf("we1", { sets })]) });

    // Assert
    await screen.findByTestId("exercise-sets");
    expect(screen.getAllByLabelText("Peso (kg)")).toHaveLength(2);
    expect(screen.getAllByLabelText("Reps")).toHaveLength(1);
    expect(screen.getAllByRole("button", { name: "Guardar" })).toHaveLength(3);
  });

  it("shows a recorded row as saved, with its values in the inputs", async () => {
    // Arrange
    const sets = [{ count: 2, target: target("RepsRange", 8, 12, null) }];
    const setRecords = [recordOf("r1"), recordOf("r2", { reps: 11, weight: 40 })];

    // Act
    await renderExercise({ workout: workoutOf([exerciseOf("we1", { sets, setRecords })]) });

    // Assert
    await screen.findByTestId("exercise-sets");
    expect(screen.getAllByLabelText("Guardada")).toHaveLength(2);
    expect(screen.queryByRole("button", { name: "Guardar" })).not.toBeOnTheScreen();
    expect(screen.getByDisplayValue("11")).toBeOnTheScreen();
    expect(screen.getByDisplayValue("40")).toBeOnTheScreen();
  });

  it("swaps the tick for a save button when a saved value changes, and back when it is restored", async () => {
    // Arrange
    const sets = [{ count: 1, target: reps(10) }];
    await renderExercise({
      workout: workoutOf([exerciseOf("we1", { sets, setRecords: [recordOf("r1")] })]),
    });
    await screen.findByTestId("exercise-sets");
    expect(screen.getByLabelText("Guardada")).toBeOnTheScreen();

    // Act
    await fireEvent.changeText(screen.getByLabelText("Peso (kg)"), "62,5");

    // Assert
    expect(screen.getByRole("button", { name: "Guardar" })).toBeOnTheScreen();
    expect(screen.queryByLabelText("Guardada")).not.toBeOnTheScreen();
    await fireEvent.changeText(screen.getByLabelText("Peso (kg)"), "60,00");
    expect(screen.getByLabelText("Guardada")).toBeOnTheScreen();
    expect(screen.queryByRole("button", { name: "Guardar" })).not.toBeOnTheScreen();
  });

  it("records a reps set with the prescribed reps and the typed weight, then reads the workout again", async () => {
    // Arrange
    const { addSetRecord, getWorkout } = await renderExercise();
    await screen.findByTestId("exercise-sets");
    await fireEvent.changeText(screen.getAllByLabelText("Peso (kg)")[0]!, "60");

    // Act
    await fireEvent.press(screen.getAllByRole("button", { name: "Guardar" })[0]!);

    // Assert
    expect(addSetRecord).toHaveBeenCalledTimes(1);
    expect(addSetRecord).toHaveBeenCalledWith({
      workoutId: "wk1",
      exerciseId: "ex-we1",
      reps: 10,
      weight: 60,
    });
    expect(getWorkout).toHaveBeenCalledTimes(2);
  });

  it("records a range set with the typed reps and no weight", async () => {
    // Arrange
    const sets = [{ count: 1, target: target("RepsRange", 8, 12, null) }];
    const { addSetRecord } = await renderExercise({
      workout: workoutOf([exerciseOf("we1", { sets })]),
    });
    await screen.findByTestId("exercise-sets");
    await fireEvent.changeText(screen.getByLabelText("Reps"), "11");

    // Act
    await fireEvent.press(screen.getByRole("button", { name: "Guardar" }));

    // Assert
    expect(addSetRecord).toHaveBeenCalledWith({
      workoutId: "wk1",
      exerciseId: "ex-we1",
      reps: 11,
    });
  });

  it("records a timed set with its seconds", async () => {
    // Arrange
    const sets = [{ count: 1, target: target("Minutes", null, null, 2) }];
    const { addSetRecord } = await renderExercise({
      workout: workoutOf([exerciseOf("we1", { sets })]),
    });
    await screen.findByText("2 min");

    // Act
    await fireEvent.press(screen.getByRole("button", { name: "Guardar" }));

    // Assert
    expect(addSetRecord).toHaveBeenCalledWith({
      workoutId: "wk1",
      exerciseId: "ex-we1",
      durationSeconds: 120,
    });
  });

  it("corrects the record of a row that already has one instead of adding another", async () => {
    // Arrange
    const sets = [{ count: 1, target: reps(10) }];
    const { addSetRecord, updateSetRecord } = await renderExercise({
      workout: workoutOf([exerciseOf("we1", { sets, setRecords: [recordOf("r1")] })]),
    });
    await screen.findByTestId("exercise-sets");
    await fireEvent.changeText(screen.getByDisplayValue("60"), "62,5");

    // Act
    await fireEvent.press(screen.getByRole("button", { name: "Guardar" }));

    // Assert
    expect(updateSetRecord).toHaveBeenCalledTimes(1);
    expect(updateSetRecord).toHaveBeenCalledWith({
      workoutId: "wk1",
      exerciseId: "ex-we1",
      setRecordId: "r1",
      reps: 10,
      weight: 62.5,
    });
    expect(addSetRecord).not.toHaveBeenCalled();
  });

  it("asks for the reps of a range row and sends nothing without them", async () => {
    // Arrange
    const sets = [{ count: 1, target: target("MaxReps", null, null, null) }];
    const { addSetRecord } = await renderExercise({
      workout: workoutOf([exerciseOf("we1", { sets })]),
    });
    await screen.findByText("máx. reps");

    // Act
    await fireEvent.press(screen.getByRole("button", { name: "Guardar" }));

    // Assert
    expect(screen.getByText("Indica las repeticiones.")).toBeOnTheScreen();
    expect(addSetRecord).not.toHaveBeenCalled();
  });

  it("rejects a weight that is not a number and sends nothing", async () => {
    // Arrange
    const { addSetRecord } = await renderExercise();
    await screen.findByTestId("exercise-sets");
    await fireEvent.changeText(screen.getAllByLabelText("Peso (kg)")[0]!, "abc");

    // Act
    await fireEvent.press(screen.getAllByRole("button", { name: "Guardar" })[0]!);

    // Assert
    expect(screen.getByText("Peso no válido.")).toBeOnTheScreen();
    expect(addSetRecord).not.toHaveBeenCalled();
  });

  it("keeps the typed values and shows the error on the row when saving fails with a conflict", async () => {
    // Arrange
    const { getWorkout } = await renderExercise({
      addResult: async () => fail(new DomainError("CONFLICT", "Locked")),
    });
    await screen.findByTestId("exercise-sets");
    await fireEvent.changeText(screen.getAllByLabelText("Peso (kg)")[0]!, "60");

    // Act
    await fireEvent.press(screen.getAllByRole("button", { name: "Guardar" })[0]!);

    // Assert
    expect(await screen.findByText("No se puede registrar esta serie.")).toBeOnTheScreen();
    expect(screen.getByDisplayValue("60")).toBeOnTheScreen();
    expect(getWorkout).toHaveBeenCalledTimes(1);
  });

  it.each(["PUBLISHED", "REVIEWED"])(
    "makes the rows read-only and hides the save button while the workout is %s",
    async (status) => {
      // Arrange
      const sets = [{ count: 1, target: reps(10) }];
      const setRecords = [recordOf("r1")];

      // Act
      await renderExercise({
        workout: workoutOf([exerciseOf("we1", { sets, setRecords })], { status } as never),
      });

      // Assert
      await screen.findByTestId("exercise-sets");
      expect(screen.queryByRole("button", { name: "Guardar" })).not.toBeOnTheScreen();
      expect(screen.getByLabelText("Peso (kg)")).toBeDisabled();
      expect(screen.getByLabelText("Guardada")).toBeOnTheScreen();
      expect(screen.getByDisplayValue("60")).toBeOnTheScreen();
    },
  );

  it.each(["STARTED", "COMPLETED"])(
    "offers to save every row while the workout is %s",
    async (status) => {
      // Arrange & Act
      await renderExercise({ workout: workoutOf([exerciseOf("we1")], { status } as never) });

      // Assert
      await screen.findByTestId("exercise-sets");
      expect(screen.getAllByRole("button", { name: "Guardar" })).toHaveLength(4);
      expect(screen.getAllByLabelText("Peso (kg)")[0]).not.toBeDisabled();
    },
  );

  it("shows the rest time in a card", async () => {
    // Arrange & Act
    await renderExercise();

    // Assert
    expect(await screen.findByText("Descanso: 1:30")).toBeOnTheScreen();
  });

  it("says there is no rest when the exercise has none", async () => {
    // Arrange & Act
    await renderExercise({ workout: workoutOf([exerciseOf("we1", { restMinutes: null })]) });

    // Assert
    expect(await screen.findByText("Sin descanso")).toBeOnTheScreen();
  });

  it("shows the error and reads the workout again on retry", async () => {
    // Arrange
    const workout = workoutOf([exerciseOf("we1")]);
    const workoutResult = jest
      .fn<Promise<Result<WorkoutById, DomainError>>, []>()
      .mockResolvedValueOnce(fail(new DomainError("NETWORK", "")))
      .mockResolvedValueOnce(ok(workout));
    const { getWorkout } = await renderExercise({ workoutResult });
    await screen.findByText("No hay conexión con el servidor.");

    // Act
    await fireEvent.press(screen.getByRole("button", { name: "Reintentar" }));

    // Assert
    expect(await screen.findByRole("heading", { name: "Sentadilla" })).toBeOnTheScreen();
    expect(getWorkout).toHaveBeenCalledTimes(2);
  });

  it("shows a not found error when the exercise is not in the workout", async () => {
    // Arrange
    mockParams = { id: "wk1", exerciseId: "missing" };

    // Act
    await renderExercise();

    // Assert
    expect(await screen.findByText("No hemos encontrado lo que buscas.")).toBeOnTheScreen();
    expect(screen.getByRole("button", { name: "Volver" })).toBeOnTheScreen();
    expect(screen.queryByTestId("exercise-sets")).not.toBeOnTheScreen();
  });

  it("follows the active language", async () => {
    // Arrange
    const sets = [{ count: 1, target: target("MaxReps", null, null, null) }];
    await renderExercise({ workout: workoutOf([exerciseOf("we1", { sets, restMinutes: 1 })]) });
    await screen.findByText("máx. reps");

    // Act
    await act(async () => {
      await i18n.changeLanguage("en");
    });

    // Assert
    expect(screen.getByText("max reps")).toBeOnTheScreen();
    expect(screen.getByRole("button", { name: "Save" })).toBeOnTheScreen();
    expect(screen.getByLabelText("Weight (kg)")).toBeOnTheScreen();
    expect(screen.getByText("Rest: 1:00")).toBeOnTheScreen();
    expect(screen.getByRole("button", { name: "Back" })).toBeOnTheScreen();
    expect(within(screen.getByLabelText("Exercises")).getByText("Squat")).toBeOnTheScreen();
  });
});

describe("WorkoutExerciseView rest countdown", () => {
  // Only the interval is faked, so the screen is read and pressed with real timers. The clock is the test's own,
  // moved by `elapse` / `lateTick` / `returnFromBackground` alongside the interval.
  let clock = 0;
  let nowSpy: jest.SpyInstance;
  let appStateHandler: ((state: string) => void) | null = null;
  let appStateSpy: jest.SpyInstance;

  const oneMinute = () => workoutOf([exerciseOf("we1", { restMinutes: 1 })]);

  async function renderRest(options: SetupOptions = {}) {
    const fakes = await renderExercise({ workout: oneMinute(), ...options });
    await screen.findByTestId("exercise-sets");
    jest.useFakeTimers({
      doNotFake: [
        "Date",
        "hrtime",
        "nextTick",
        "performance",
        "queueMicrotask",
        "requestAnimationFrame",
        "cancelAnimationFrame",
        "requestIdleCallback",
        "cancelIdleCallback",
        "setImmediate",
        "clearImmediate",
        "setTimeout",
        "clearTimeout",
      ],
    });
    clock = new Date(2026, 10, 9, 10, 0).getTime();
    nowSpy = jest.spyOn(Date, "now").mockImplementation(() => clock);

    return fakes;
  }

  const elapse = async (ms: number) => {
    clock += ms;
    await act(async () => {
      jest.advanceTimersByTime(ms);
    });
  };

  async function pressAndElapse(name: string, ms: number) {
    await fireEvent.press(screen.getByRole("button", { name }));
    await elapse(ms);
  }

  /** The clock moves on while the interval is held back, then one tick runs. */
  const lateTick = async (ms: number) => {
    clock += ms;
    await act(async () => {
      jest.advanceTimersByTime(250);
    });
  };

  /** The clock moves on while the app is in the background, then the app becomes active. */
  const returnFromBackground = async (ms: number) => {
    clock += ms;
    await act(async () => {
      appStateHandler?.("active");
    });
  };

  beforeEach(() => {
    mockBack.mockClear();
    mockSetParams.mockClear();
    mockParams = { id: "wk1", exerciseId: "we1" };
    appStateHandler = null;
    appStateSpy = jest.spyOn(AppState, "addEventListener").mockImplementation((_type, handler) => {
      appStateHandler = handler as (state: string) => void;

      return { remove: () => {} } as never;
    });
  });

  afterEach(async () => {
    jest.useRealTimers();
    nowSpy?.mockRestore();
    appStateSpy.mockRestore();
    await act(async () => {
      await i18n.changeLanguage("es");
    });
  });

  it("offers play and a disabled reset when the exercise has rest", async () => {
    // Arrange & Act
    await renderRest();

    // Assert
    expect(screen.getByText("Descanso: 1:00")).toBeOnTheScreen();
    expect(screen.getByRole("button", { name: "Iniciar descanso" })).toBeEnabled();
    expect(screen.getByRole("button", { name: "Reiniciar descanso" })).toBeDisabled();
  });

  it("offers no controls when the exercise has no rest", async () => {
    // Arrange & Act
    await renderRest({ workout: workoutOf([exerciseOf("we1", { restMinutes: null })]) });

    // Assert
    expect(screen.getByText("Sin descanso")).toBeOnTheScreen();
    expect(screen.queryByRole("button", { name: "Iniciar descanso" })).not.toBeOnTheScreen();
    expect(screen.queryByRole("button", { name: "Pausar descanso" })).not.toBeOnTheScreen();
    expect(screen.queryByRole("button", { name: "Reiniciar descanso" })).not.toBeOnTheScreen();
  });

  it("offers play on a workout that is read-only", async () => {
    // Arrange & Act
    await renderRest({
      workout: workoutOf([exerciseOf("we1", { restMinutes: 1 })], { status: "PUBLISHED" } as never),
    });

    // Assert
    expect(screen.getByRole("button", { name: "Iniciar descanso" })).toBeEnabled();
  });

  it("counts down from the full rest once started and offers pause and reset", async () => {
    // Arrange
    await renderRest();

    // Act
    await pressAndElapse("Iniciar descanso", 5000);

    // Assert
    expect(screen.getByText("Descanso: 0:55")).toBeOnTheScreen();
    expect(screen.getByRole("button", { name: "Pausar descanso" })).toBeEnabled();
    expect(screen.getByRole("button", { name: "Reiniciar descanso" })).toBeEnabled();
  });

  it("shows the remaining time in the band", async () => {
    // Arrange
    await renderRest({ workout: workoutOf([exerciseOf("we1", { restMinutes: 1.5 })]) });

    // Act
    await pressAndElapse("Iniciar descanso", 3000);

    // Assert
    expect(screen.getByText("Descanso: 1:27")).toBeOnTheScreen();
  });

  it("freezes the remaining time on pause", async () => {
    // Arrange
    await renderRest();
    await pressAndElapse("Iniciar descanso", 5000);

    // Act
    await pressAndElapse("Pausar descanso", 10000);

    // Assert
    expect(screen.getByText("Descanso: 0:55")).toBeOnTheScreen();
    expect(screen.getByRole("button", { name: "Iniciar descanso" })).toBeEnabled();
  });

  it("resumes from the remaining time", async () => {
    // Arrange
    await renderRest();
    await pressAndElapse("Iniciar descanso", 5000);
    await pressAndElapse("Pausar descanso", 10000);

    // Act
    await pressAndElapse("Iniciar descanso", 5000);

    // Assert
    expect(screen.getByText("Descanso: 0:50")).toBeOnTheScreen();
  });

  it("restores the full rest when reset while running", async () => {
    // Arrange
    await renderRest();
    await pressAndElapse("Iniciar descanso", 20000);

    // Act
    await pressAndElapse("Reiniciar descanso", 0);

    // Assert
    expect(screen.getByText("Descanso: 1:00")).toBeOnTheScreen();
    expect(screen.getByRole("button", { name: "Iniciar descanso" })).toBeEnabled();
    expect(screen.getByRole("button", { name: "Reiniciar descanso" })).toBeDisabled();
  });

  it("restores the full rest when reset while paused, and stays there", async () => {
    // Arrange
    await renderRest();
    await pressAndElapse("Iniciar descanso", 20000);
    await pressAndElapse("Pausar descanso", 0);

    // Act
    await pressAndElapse("Reiniciar descanso", 10000);

    // Assert
    expect(screen.getByText("Descanso: 1:00")).toBeOnTheScreen();
  });

  it("stops at zero and says the rest finished", async () => {
    // Arrange
    await renderRest();
    await fireEvent.press(screen.getByRole("button", { name: "Iniciar descanso" }));

    // Act
    await elapse(60000);

    // Assert
    expect(screen.getByText("Descanso terminado")).toBeOnTheScreen();
    expect(screen.getByRole("button", { name: "Iniciar descanso" })).toBeDisabled();
    expect(screen.getByRole("button", { name: "Reiniciar descanso" })).toBeEnabled();
  });

  it("restores the full rest when reset after finishing", async () => {
    // Arrange
    await renderRest();
    await pressAndElapse("Iniciar descanso", 60000);

    // Act
    await pressAndElapse("Reiniciar descanso", 0);

    // Assert
    expect(screen.getByText("Descanso: 1:00")).toBeOnTheScreen();
    expect(screen.getByRole("button", { name: "Iniciar descanso" })).toBeEnabled();
  });

  it("shows the right time after a late tick", async () => {
    // Arrange
    await renderRest();
    await fireEvent.press(screen.getByRole("button", { name: "Iniciar descanso" }));

    // Act
    await lateTick(20000);

    // Assert
    expect(screen.getByText("Descanso: 0:40")).toBeOnTheScreen();
  });

  it("shows the right time when the app comes back from the background", async () => {
    // Arrange
    await renderRest();
    await fireEvent.press(screen.getByRole("button", { name: "Iniciar descanso" }));

    // Act
    await returnFromBackground(30000);

    // Assert
    expect(screen.getByText("Descanso: 0:30")).toBeOnTheScreen();
  });

  it("says the rest finished when it ended in the background", async () => {
    // Arrange
    await renderRest();
    await fireEvent.press(screen.getByRole("button", { name: "Iniciar descanso" }));

    // Act
    await returnFromBackground(90000);

    // Assert
    expect(screen.getByText("Descanso terminado")).toBeOnTheScreen();
  });

  it("keeps counting while the keyboard hides the card", async () => {
    // Arrange
    await renderRest();
    await pressAndElapse("Iniciar descanso", 0);
    await act(async () => mockSetKeyboardVisible?.(true));
    await elapse(10000);

    // Act
    await act(async () => mockSetKeyboardVisible?.(false));

    // Assert
    expect(screen.getByText("Descanso: 0:50")).toBeOnTheScreen();
  });

  it("keeps counting after a set is saved", async () => {
    // Arrange
    const { addSetRecord } = await renderRest();
    await pressAndElapse("Iniciar descanso", 10000);

    // Act
    await fireEvent.press(screen.getAllByRole("button", { name: "Guardar" })[0]!);

    // Assert
    expect(addSetRecord).toHaveBeenCalledTimes(1);
    expect(screen.getByText("Descanso: 0:50")).toBeOnTheScreen();
    expect(screen.getByRole("button", { name: "Pausar descanso" })).toBeEnabled();
  });

  it("goes back to idle with the rest of the exercise the athlete switches to", async () => {
    // Arrange
    const workout = workoutOf([
      exerciseOf("we1", { restMinutes: 1 }),
      exerciseOf("we2", { translations: [{ languageCode: "es", name: "Remo" }], restMinutes: 2 }),
    ]);
    await renderRest({ workout });
    await pressAndElapse("Iniciar descanso", 5000);

    // Act
    await fireEvent.press(within(screen.getByLabelText("Ejercicios")).getByText("Remo"));

    // Assert
    expect(screen.getByText("Descanso: 2:00")).toBeOnTheScreen();
    expect(screen.getByRole("button", { name: "Iniciar descanso" })).toBeEnabled();
  });

  it("follows the active language", async () => {
    // Arrange
    await renderRest();

    // Act
    await act(async () => {
      await i18n.changeLanguage("en");
    });

    // Assert
    expect(screen.getByRole("button", { name: "Start rest" })).toBeOnTheScreen();
    expect(screen.getByRole("button", { name: "Reset rest" })).toBeOnTheScreen();
    await fireEvent.press(screen.getByRole("button", { name: "Start rest" }));
    expect(screen.getByRole("button", { name: "Pause rest" })).toBeOnTheScreen();
  });
});
