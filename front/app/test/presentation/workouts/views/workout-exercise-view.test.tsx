import { act, fireEvent, screen, waitFor, within } from "@testing-library/react-native";
import { AppState, Linking } from "react-native";
import { DomainError, fail, ok } from "@weight-tracker/core/domain";
import type {
  AddSetRecordInput,
  Result,
  SetRecord,
  Exercise,
  ExerciseLastPerformance,
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
  lastPerformanceResult?: () => Promise<Result<ExerciseLastPerformance | null, DomainError>>;
}

function setup({
  workout,
  catalogOf = catalog,
  workoutResult,
  addResult,
  updateResult,
  lastPerformanceResult,
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
  const getLastPerformance = jest.fn<
    Promise<Result<ExerciseLastPerformance | null, DomainError>>,
    [string]
  >(lastPerformanceResult ?? (async () => ok(null)));
  const dependencies = {
    getMyExerciseLastPerformanceUseCase: () => ({ execute: getLastPerformance }),
    getWorkoutByIdUseCase: () => ({ execute: getWorkout }),
    addSetRecordUseCase: () => ({ execute: addSetRecord }),
    updateSetRecordUseCase: () => ({ execute: updateSetRecord }),
    getExercisesUseCase: () => ({ execute: async () => ok(catalogOf) }),
  };

  return { getWorkout, addSetRecord, updateSetRecord, getLastPerformance, dependencies };
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

  it("renews the session and reads the workout again when the read is unauthorized", async () => {
    // Arrange
    const workout = workoutOf([exerciseOf("we1")]);
    const workoutResult = jest
      .fn<Promise<Result<WorkoutById, DomainError>>, []>()
      .mockResolvedValueOnce(fail(new DomainError("UNAUTHORIZED", "")))
      .mockResolvedValueOnce(ok(workout));
    const refresh = jest.fn(async () => ok({} as never));
    const { dependencies, getWorkout } = setup({ workoutResult });

    // Act
    await renderWithProviders(<WorkoutExerciseView />, {
      ...dependencies,
      refreshSessionUseCase: () => ({ execute: refresh }),
    });

    // Assert
    expect(await screen.findByRole("heading", { name: "Sentadilla" })).toBeOnTheScreen();
    expect(refresh).toHaveBeenCalledTimes(1);
    expect(getWorkout).toHaveBeenCalledTimes(2);
  });

  it("renews the session and saves the set again when the save is unauthorized", async () => {
    // Arrange
    const refresh = jest.fn(async () => ok({} as never));
    const current = workoutOf([exerciseOf("we1")]);
    const addResult = jest
      .fn<Promise<Result<WorkoutDetail, DomainError>>, []>()
      .mockResolvedValueOnce(fail(new DomainError("UNAUTHORIZED", "")))
      .mockResolvedValueOnce(ok(current as unknown as WorkoutDetail));
    const { dependencies, addSetRecord } = setup({ addResult });
    await renderWithProviders(<WorkoutExerciseView />, {
      ...dependencies,
      refreshSessionUseCase: () => ({ execute: refresh }),
    });
    await screen.findByTestId("exercise-sets");

    // Act
    await fireEvent.press(screen.getAllByRole("button", { name: "Guardar" })[0]!);

    // Assert
    expect(refresh).toHaveBeenCalledTimes(1);
    expect(addSetRecord).toHaveBeenCalledTimes(2);
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

describe("WorkoutExerciseView last performance", () => {
  const lastPerformanceOf = (
    overrides: Partial<ExerciseLastPerformance> = {},
  ): ExerciseLastPerformance => ({
    workoutDate: "2026-11-10",
    effort: "HIGH",
    sets: [{ reps: 10, durationSeconds: null, weight: 20 }],
    ...overrides,
  });

  async function renderLastPerformance(options: SetupOptions = {}) {
    mockBack.mockClear();
    mockSetParams.mockClear();
    mockParams = { id: "wk1", exerciseId: "we1" };
    const fakes = await renderExercise({
      lastPerformanceResult: async () => ok(lastPerformanceOf()),
      ...options,
    });
    await screen.findByTestId("exercise-sets");

    return fakes;
  }

  async function openDialog() {
    await fireEvent.press(await screen.findByRole("link", { name: "Marcas anteriores" }));
    await screen.findByText("Última sesión");
  }

  it("shows the previous sets link and keeps the video link in the header", async () => {
    // Arrange & Act
    await renderLastPerformance({
      workout: workoutOf([exerciseOf("we1", { videoUrl: "https://v.example/1" })]),
    });

    // Assert
    expect(screen.getByRole("link", { name: "Ver vídeo" })).toBeOnTheScreen();
    expect(await screen.findByRole("link", { name: "Marcas anteriores" })).toBeOnTheScreen();
  });

  it("shows the previous sets link for an exercise without a video", async () => {
    // Arrange & Act
    await renderLastPerformance();

    // Assert
    expect(screen.queryByRole("link", { name: "Ver vídeo" })).not.toBeOnTheScreen();
    expect(await screen.findByRole("link", { name: "Marcas anteriores" })).toBeOnTheScreen();
  });

  it("reads the last performance of the catalog exercise when the exercise is shown", async () => {
    // Arrange & Act
    const { getLastPerformance } = await renderLastPerformance();

    // Assert
    await waitFor(() => expect(getLastPerformance).toHaveBeenCalledWith("ex-we1"));
    expect(getLastPerformance).toHaveBeenCalledTimes(1);
    expect(screen.queryByText("Última sesión")).not.toBeOnTheScreen();
  });

  it("reads the last performance of another exercise when it is chosen", async () => {
    // Arrange
    const { getLastPerformance } = await renderLastPerformance({
      workout: workoutOf(threeExercises()),
    });
    await waitFor(() => expect(getLastPerformance).toHaveBeenCalledWith("ex-we1"));

    // Act
    await fireEvent.press(within(screen.getByLabelText("Ejercicios")).getByText("Remo"));

    // Assert
    await waitFor(() => expect(getLastPerformance).toHaveBeenCalledWith("ex-we3"));
    expect(getLastPerformance).toHaveBeenCalledTimes(2);
  });

  it("shows the link when only one of three sets has a weight", async () => {
    // Arrange & Act
    await renderLastPerformance({
      lastPerformanceResult: async () =>
        ok(
          lastPerformanceOf({
            sets: [
              { reps: 12, durationSeconds: null, weight: null },
              { reps: 12, durationSeconds: null, weight: 40 },
              { reps: 12, durationSeconds: null, weight: null },
            ],
          }),
        ),
    });

    // Assert
    expect(await screen.findByRole("link", { name: "Marcas anteriores" })).toBeOnTheScreen();
  });

  it("hides the link when no set has a weight", async () => {
    // Arrange
    const { getLastPerformance } = await renderLastPerformance({
      lastPerformanceResult: async () =>
        ok(
          lastPerformanceOf({
            sets: [
              { reps: 12, durationSeconds: null, weight: null },
              { reps: 12, durationSeconds: null, weight: null },
            ],
          }),
        ),
    });

    // Act
    await waitFor(() => expect(getLastPerformance).toHaveBeenCalled());
    await act(async () => {});

    // Assert
    expect(screen.queryByRole("link", { name: "Marcas anteriores" })).not.toBeOnTheScreen();
  });

  it("hides the link when the athlete never did the exercise", async () => {
    // Arrange
    const { getLastPerformance } = await renderLastPerformance({
      lastPerformanceResult: async () => ok(null),
    });

    // Act
    await waitFor(() => expect(getLastPerformance).toHaveBeenCalled());
    await act(async () => {});

    // Assert
    expect(screen.queryByRole("link", { name: "Marcas anteriores" })).not.toBeOnTheScreen();
  });

  it("hides the link when the last performance cannot be read", async () => {
    // Arrange
    const { getLastPerformance } = await renderLastPerformance({
      lastPerformanceResult: async () => fail(new DomainError("NOT_FOUND", "Missing")),
    });

    // Act
    await waitFor(() => expect(getLastPerformance).toHaveBeenCalled());
    await act(async () => {});

    // Assert
    expect(screen.queryByRole("link", { name: "Marcas anteriores" })).not.toBeOnTheScreen();
  });

  it("shows a skeleton instead of the sets and the link while the last performance is read", async () => {
    // Arrange & Act
    const { getLastPerformance } = await renderExercise({
      lastPerformanceResult: () => new Promise(() => {}),
    });
    await waitFor(() => expect(getLastPerformance).toHaveBeenCalled());

    // Assert
    expect(
      await screen.findByTestId("exercise-skeleton", { includeHiddenElements: true }),
    ).toBeOnTheScreen();
    expect(screen.queryByTestId("exercise-sets")).not.toBeOnTheScreen();
    expect(screen.queryByRole("link", { name: "Marcas anteriores" })).not.toBeOnTheScreen();
    expect(screen.getByRole("heading", { name: "Sentadilla" })).toBeOnTheScreen();
    expect(screen.getByText("Descanso: 1:30")).toBeOnTheScreen();
  });

  it("replaces the skeleton with the sets and the link together once the last performance is read", async () => {
    // Arrange & Act
    await renderLastPerformance();

    // Assert
    expect(await screen.findByRole("link", { name: "Marcas anteriores" })).toBeOnTheScreen();
    expect(screen.getByTestId("exercise-sets")).toBeOnTheScreen();
    expect(
      screen.queryByTestId("exercise-skeleton", { includeHiddenElements: true }),
    ).not.toBeOnTheScreen();
  });

  it("shows the sets without the link and without the skeleton when the read fails", async () => {
    // Arrange
    const { getLastPerformance } = await renderLastPerformance({
      lastPerformanceResult: async () => fail(new DomainError("NETWORK", "Offline")),
    });

    // Act
    await waitFor(() => expect(getLastPerformance).toHaveBeenCalled());
    await act(async () => {});

    // Assert
    expect(screen.getByTestId("exercise-sets")).toBeOnTheScreen();
    expect(
      screen.queryByTestId("exercise-skeleton", { includeHiddenElements: true }),
    ).not.toBeOnTheScreen();
    expect(screen.queryByRole("link", { name: "Marcas anteriores" })).not.toBeOnTheScreen();
  });

  it("shows the skeleton again while the next exercise's last performance is read", async () => {
    // Arrange
    let reads = 0;
    const { getLastPerformance } = await renderLastPerformance({
      workout: workoutOf(threeExercises()),
      lastPerformanceResult: () =>
        ++reads === 1 ? Promise.resolve(ok(lastPerformanceOf())) : new Promise(() => {}),
    });
    await screen.findByRole("link", { name: "Marcas anteriores" });

    // Act
    await fireEvent.press(within(screen.getByLabelText("Ejercicios")).getByText("Remo"));

    // Assert
    await waitFor(() => expect(getLastPerformance).toHaveBeenCalledWith("ex-we3"));
    expect(
      await screen.findByTestId("exercise-skeleton", { includeHiddenElements: true }),
    ).toBeOnTheScreen();
    expect(screen.queryByTestId("exercise-sets")).not.toBeOnTheScreen();
    expect(screen.queryByRole("link", { name: "Marcas anteriores" })).not.toBeOnTheScreen();
  });

  it("opens the dialog with what was read, without reading again", async () => {
    // Arrange
    const { getLastPerformance } = await renderLastPerformance();

    // Act
    await openDialog();

    // Assert
    expect(await screen.findByTestId("last-performance-content")).toBeOnTheScreen();
    expect(getLastPerformance).toHaveBeenCalledTimes(1);
  });

  it("shows no previous sets link while the workout is read", async () => {
    // Arrange & Act
    await renderExercise({ workoutResult: () => new Promise(() => {}) });

    // Assert
    expect(screen.getAllByRole("progressbar").length).toBeGreaterThan(0);
    expect(screen.queryByRole("link", { name: "Marcas anteriores" })).not.toBeOnTheScreen();
  });

  it.each(["PUBLISHED", "STARTED", "COMPLETED", "REVIEWED"])(
    "shows the previous sets link when the workout is %s",
    async (status) => {
      // Arrange & Act
      await renderLastPerformance({ workout: workoutOf([exerciseOf("we1")], { status } as never) });

      // Assert
      expect(await screen.findByRole("link", { name: "Marcas anteriores" })).toBeOnTheScreen();
    },
  );

  it("shows the date of the session, short, in the active language", async () => {
    // Arrange
    await renderLastPerformance({ lastPerformanceResult: async () => ok(lastPerformanceOf()) });

    // Act
    await openDialog();

    // Assert
    expect(await screen.findByText("10 nov 2026")).toBeOnTheScreen();
  });

  it.each([
    ["STRENGTH", "HIGH", "RIR 0"],
    ["CARDIO", "MID", "RPE 5-6"],
    ["STRETCHING", "LOW", "Muy suave"],
    ["OTHER", "MIDLOW", "Medio-bajo"],
  ])("shows the effort of a %s exercise in its own scale", async (type, effort, expected) => {
    // Arrange
    await renderLastPerformance({
      workout: workoutOf([exerciseOf("we1", { type } as never)]),
      lastPerformanceResult: async () => ok(lastPerformanceOf({ effort })),
    });

    // Act
    await openDialog();

    // Assert
    expect(await screen.findByLabelText(`Esfuerzo: ${expected}`)).toBeOnTheScreen();
    expect(screen.getByText(expected)).toBeOnTheScreen();
  });

  it("shows an effort code it does not know as it comes", async () => {
    // Arrange
    await renderLastPerformance({
      lastPerformanceResult: async () => ok(lastPerformanceOf({ effort: "EXTREME" })),
    });

    // Act
    await openDialog();

    // Assert
    expect(await screen.findByLabelText("Esfuerzo: EXTREME")).toBeOnTheScreen();
  });

  it("hides the effort line when the session has no effort", async () => {
    // Arrange
    await renderLastPerformance({
      lastPerformanceResult: async () => ok(lastPerformanceOf({ effort: "" })),
    });

    // Act
    await openDialog();

    // Assert
    await screen.findByTestId("last-performance-content");
    expect(screen.getByLabelText("Series realizadas: 1")).toBeOnTheScreen();
    expect(screen.queryByText("Esfuerzo")).not.toBeOnTheScreen();
  });

  it("shows how many sets were done and the effort in two cards", async () => {
    // Arrange
    await renderLastPerformance({
      lastPerformanceResult: async () =>
        ok(
          lastPerformanceOf({
            sets: [
              { reps: 12, durationSeconds: null, weight: 20 },
              { reps: 12, durationSeconds: null, weight: 20 },
              { reps: 12, durationSeconds: null, weight: 20 },
            ],
          }),
        ),
    });

    // Act
    await openDialog();

    // Assert
    expect(await screen.findByLabelText("Series realizadas: 3")).toBeOnTheScreen();
    expect(screen.getByLabelText("Esfuerzo: RIR 0")).toBeOnTheScreen();
  });

  it("lists the sets in order with their reps or seconds and their weight", async () => {
    // Arrange
    await renderLastPerformance({
      lastPerformanceResult: async () =>
        ok(
          lastPerformanceOf({
            sets: [
              { reps: 12, durationSeconds: null, weight: 20 },
              { reps: 14, durationSeconds: null, weight: null },
              { reps: null, durationSeconds: 45, weight: null },
              { reps: 9, durationSeconds: null, weight: 0 },
              { reps: null, durationSeconds: null, weight: 62.5 },
            ],
          }),
        ),
    });

    // Act
    await openDialog();

    // Assert
    const table = within(await screen.findByTestId("last-performance-sets"));
    expect(table.getAllByText(/^\d$/).map((cell) => cell.props.children)).toEqual([1, 2, 3, 4, 5]);
    expect(screen.getByText("Series registradas")).toBeOnTheScreen();
    expect(screen.getByText("12 reps")).toBeOnTheScreen();
    expect(screen.getByText("20 kg")).toBeOnTheScreen();
    expect(screen.getByText("14 reps")).toBeOnTheScreen();
    expect(screen.getByText("45 s")).toBeOnTheScreen();
    expect(screen.getAllByText("sin peso")).toHaveLength(2);
    expect(screen.getByText("9 reps")).toBeOnTheScreen();
    expect(screen.getByText("0 kg")).toBeOnTheScreen();
    expect(screen.getByText("62.5 kg")).toBeOnTheScreen();
    expect(screen.queryByText(/null/)).not.toBeOnTheScreen();
  });

  describe("highest weight", () => {
    const setsOf = (weights: (number | null)[]) =>
      weights.map((weight) => ({ reps: 12, durationSeconds: null, weight }));

    async function openWith(weights: (number | null)[]) {
      await renderLastPerformance({
        lastPerformanceResult: async () => ok(lastPerformanceOf({ sets: setsOf(weights) })),
      });
      await openDialog();
      await screen.findByTestId("last-performance-sets");
    }

    const starIn = (row: number) =>
      within(screen.getByTestId(`last-performance-set-${row}`)).queryByText("Mayor carga");

    it("marks the highest weight only, with a star and the text under it", async () => {
      // Arrange & Act
      await openWith([20, 30, 25]);

      // Assert
      expect(starIn(1)).not.toBeOnTheScreen();
      expect(starIn(2)).toBeOnTheScreen();
      expect(starIn(3)).not.toBeOnTheScreen();
      expect(screen.getAllByText("Mayor carga")).toHaveLength(1);
    });

    it("marks only the last set when the highest weight is repeated", async () => {
      // Arrange & Act
      await openWith([30, 20, 30]);

      // Assert
      expect(starIn(1)).not.toBeOnTheScreen();
      expect(starIn(2)).not.toBeOnTheScreen();
      expect(starIn(3)).toBeOnTheScreen();
    });

    it("ignores sets without weight when looking for the highest", async () => {
      // Arrange & Act
      await openWith([null, 15, null]);

      // Assert
      expect(starIn(2)).toBeOnTheScreen();
      expect(screen.getAllByText("Mayor carga")).toHaveLength(1);
    });

    it("marks nothing when the highest weight is 0", async () => {
      // Arrange & Act
      await openWith([0, null, 0]);

      // Assert
      expect(screen.queryByText("Mayor carga")).not.toBeOnTheScreen();
    });
  });

  it("closes the dialog and keeps the screen as it was", async () => {
    // Arrange
    await renderLastPerformance({ lastPerformanceResult: async () => ok(lastPerformanceOf()) });
    await openDialog();
    await screen.findByTestId("last-performance-content");

    // Act
    await fireEvent.press(screen.getByRole("button", { name: "Cerrar" }));

    // Assert
    await waitFor(() => expect(screen.queryByText("Última sesión")).not.toBeOnTheScreen());
    expect(screen.getByRole("heading", { name: "Sentadilla" })).toBeOnTheScreen();
    expect(screen.getByRole("link", { name: "Marcas anteriores" })).toBeOnTheScreen();
    expect(mockBack).not.toHaveBeenCalled();
  });

  it("closes the dialog when another exercise is chosen", async () => {
    // Arrange
    await renderLastPerformance({
      workout: workoutOf(threeExercises()),
      lastPerformanceResult: async () => ok(lastPerformanceOf()),
    });
    await openDialog();
    await screen.findByTestId("last-performance-content");

    // Act
    await fireEvent.press(within(screen.getByLabelText("Ejercicios")).getByText("Remo"));

    // Assert
    await waitFor(() => expect(screen.queryByText("Última sesión")).not.toBeOnTheScreen());
    expect(mockSetParams).toHaveBeenCalledWith({ exerciseId: "we3" });
  });

  it("follows the active language", async () => {
    // Arrange
    await renderLastPerformance({
      lastPerformanceResult: async () =>
        ok(
          lastPerformanceOf({
            sets: [
              { reps: 12, durationSeconds: null, weight: 20 },
              { reps: 14, durationSeconds: null, weight: null },
            ],
          }),
        ),
    });
    await act(async () => {
      await i18n.changeLanguage("en");
    });

    // Act
    await fireEvent.press(await screen.findByRole("link", { name: "Previous sets" }));

    // Assert
    expect(await screen.findByText("Last session")).toBeOnTheScreen();
    expect(await screen.findByText("Nov 10, 2026")).toBeOnTheScreen();
    expect(screen.getByLabelText("Effort: RIR 0")).toBeOnTheScreen();
    expect(screen.getByLabelText("Sets done: 2")).toBeOnTheScreen();
    expect(screen.getByText("Recorded sets")).toBeOnTheScreen();
    expect(screen.getByText("12 reps")).toBeOnTheScreen();
    expect(screen.getByText("no weight")).toBeOnTheScreen();
    expect(screen.getByRole("button", { name: "Close" })).toBeOnTheScreen();
  });
});
