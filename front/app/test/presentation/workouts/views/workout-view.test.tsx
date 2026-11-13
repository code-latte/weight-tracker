import { act, fireEvent, screen, waitFor } from "@testing-library/react-native";
import { DomainError, fail, ok } from "@weight-tracker/core/domain";
import type {
  Result,
  SetRecord,
  UserProfile,
  Exercise,
  WorkoutById,
  WorkoutByIdExercise,
  WorkoutDetail,
  WorkoutMesocycle,
} from "@weight-tracker/core/domain";
import i18n from "@/infrastructure/i18n";
import WorkoutView from "@/presentation/workouts/views/workout-view";
import renderWithProviders from "../../../support/render-with-providers";

const mockBack = jest.fn();
const mockPush = jest.fn();
const mockFocus: { current: (() => void) | null } = { current: null };

jest.mock("expo-router", () => ({
  router: { back: () => mockBack(), push: (href: unknown) => mockPush(href) },
  // Runs the effect on mount, as the router does on the first focus, and keeps it so a test can focus again.
  useFocusEffect: (effect: () => void) => {
    // eslint-disable-next-line @typescript-eslint/no-require-imports
    require("react").useEffect(() => {
      mockFocus.current = effect;
      effect();
    }, [effect]);
  },
  useLocalSearchParams: () => ({ id: "wk1" }),
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

const trainer: UserProfile = {
  id: "7a2b3c4d-1e2f-4a5b-9c8d-0e1f2a3b4c5d",
  email: "carlos@example.com",
  name: "Carlos",
  surname: "Ruiz",
  role: "TRAINER",
};

const PROGRESS_LABELS = /^(Completado|En curso|Sin empezar)$/;
const TYPE_LABELS = /^(Fuerza|Cardio|Estiramiento)$/;

function records(count: number): SetRecord[] {
  return Array.from({ length: count }, (_, index) => ({
    id: `r${index}`,
    type: "Reps",
    reps: 10,
    durationSeconds: null,
    weight: 20,
    registeredAtUtc: "2026-11-10T10:00:00Z",
  }));
}

/**
 * A workout exercise, with its exercise's names, type and video as the workout carries them, prescribed one
 * `count × 10 reps` line per count, with that many set records.
 */
function workoutExercise(
  id: string,
  type: string,
  names: Record<string, string>,
  counts: number[],
  recordCount: number,
  videoUrl: string | null = null,
): WorkoutByIdExercise {
  return {
    id,
    exerciseId: `exercise-${id}`,
    translations: Object.entries(names).map(([languageCode, name]) => ({ languageCode, name })),
    videoUrl,
    muscle: "CHEST",
    bodyPart: "CHEST",
    type,
    effort: "MID",
    perSide: false,
    restMinutes: null,
    sets: counts.map((count) => ({ count, target: { type: "Reps", value: 10, maxValue: null } })),
    superSeriesId: null,
    setRecords: records(recordCount),
  };
}

/** Six exercises, three done: one in progress across two prescriptions, one with no names and an unknown type. */
const exercises: WorkoutByIdExercise[] = [
  workoutExercise(
    "we1",
    "STRENGTH",
    { es: "Press inclinado", en: "Incline press" },
    [4],
    4,
    "https://v.example/1",
  ),
  // More records than sets is still done.
  workoutExercise("we2", "STRENGTH", { es: "Jalón al pecho", en: "Lat pulldown" }, [3], 4),
  // 3 + 1 sets with 3 records is still in progress.
  workoutExercise("we3", "CARDIO", { es: "Remo en máquina", en: "Rowing machine" }, [3, 1], 3),
  workoutExercise(
    "we4",
    "STRETCHING",
    { es: "Estiramiento de isquios", en: "Hamstring stretch" },
    [4],
    0,
  ),
  workoutExercise("we5", "UNKNOWN", {}, [], 0),
  workoutExercise("we6", "STRENGTH", { en: "Squat" }, [2], 2),
];

const mesocycle: WorkoutMesocycle = {
  id: "m1",
  name: "Hipertrofia otoño",
  objective: "Hipertrofia",
  startDate: "2026-10-26",
  durationInWeeks: 6,
  weeks: [],
};

function workout(overrides: Partial<WorkoutById> = {}): WorkoutById {
  return {
    id: "wk1",
    name: "Tren superior A",
    description: null,
    workoutDate: "2026-11-10",
    userId: athlete.id,
    trainerId: trainer.id,
    status: "STARTED",
    exercises,
    mesocycleId: "m1",
    mesocycleWeekNumber: 3,
    startedAt: null,
    completedAt: null,
    reviewedAt: null,
    reviewedBy: null,
    resultsUpdatedAt: null,
    athlete,
    trainer,
    mesocycle,
    ...overrides,
  };
}

/** The catalog: CHEST has one muscle (so it shows alone), LEGS has two. */
const catalog = [
  { bodyPart: "CHEST", muscle: "CHEST" },
  { bodyPart: "LEGS", muscle: "QUADRICEPS" },
  { bodyPart: "LEGS", muscle: "HAMSTRINGS" },
] as Exercise[];

interface SetupOptions {
  catalogResult?: () => Promise<Result<Exercise[], DomainError>>;
  workoutResult?: () => Promise<Result<WorkoutById, DomainError>>;
  statusResult?: () => Promise<Result<WorkoutDetail, DomainError>>;
}

function setup({
  catalogResult = async () => ok(catalog),
  workoutResult = async () => ok(workout()),
  statusResult = async () => ok(workout() as unknown as WorkoutDetail),
}: SetupOptions = {}) {
  const getWorkout = jest.fn(workoutResult);
  const changeStatus = jest.fn(statusResult);
  const dependencies = {
    getWorkoutByIdUseCase: () => ({ execute: getWorkout }),
    changeWorkoutStatusUseCase: () => ({ execute: changeStatus }),
    getExercisesUseCase: () => ({ execute: jest.fn(catalogResult) }),
  };

  return { getWorkout, changeStatus, dependencies };
}

/** The accessible labels of the icons whose label matches, in screen order. */
function iconLabels(pattern: RegExp): string[] {
  return screen
    .getAllByRole("image")
    .map((element) => element.props.accessibilityLabel as string)
    .filter((label) => pattern.test(label));
}

describe("WorkoutView", () => {
  beforeEach(() => {
    mockBack.mockClear();
    mockPush.mockClear();
  });

  it("shows a loading indicator and the back button while the workout is read", async () => {
    // Arrange & Act
    const { dependencies } = setup({ workoutResult: () => new Promise(() => {}) });
    await renderWithProviders(<WorkoutView />, dependencies);

    // Assert
    expect(screen.getAllByRole("progressbar").length).toBeGreaterThan(0);
    expect(screen.getByRole("button", { name: "Volver" })).toBeOnTheScreen();
    expect(screen.queryByText("Tren superior A")).not.toBeOnTheScreen();
  });

  it("shows the name, the mesocycle week and the exercise progress in the panel", async () => {
    // Arrange & Act
    const { getWorkout, dependencies } = setup();
    await renderWithProviders(<WorkoutView />, dependencies);

    // Assert
    expect(await screen.findByRole("heading", { name: "Tren superior A" })).toBeOnTheScreen();
    expect(screen.getByText("Hipertrofia otoño · Semana 3")).toBeOnTheScreen();
    expect(screen.getByText("3 / 6 ejercicios")).toBeOnTheScreen();
    expect(screen.getByRole("progressbar")).toHaveProp(
      "accessibilityValue",
      expect.objectContaining({ now: 50 }),
    );
    expect(getWorkout).toHaveBeenCalledTimes(1);
    expect(getWorkout).toHaveBeenCalledWith({ id: "wk1" });
  });

  it("shows no subtitle for a standalone workout", async () => {
    // Arrange & Act
    const { dependencies } = setup({
      workoutResult: async () =>
        ok(workout({ mesocycleId: null, mesocycleWeekNumber: null, mesocycle: null })),
    });
    await renderWithProviders(<WorkoutView />, dependencies);

    // Assert
    await screen.findByRole("heading", { name: "Tren superior A" });
    expect(screen.queryByText(/Semana/)).not.toBeOnTheScreen();
  });

  it("lists the exercises in the API order, with their names in Spanish, their first translation or Ejercicio", async () => {
    // Arrange & Act
    const { dependencies } = setup();
    await renderWithProviders(<WorkoutView />, dependencies);

    // Assert
    await screen.findByText("Press inclinado");
    const names = [
      "Press inclinado",
      "Jalón al pecho",
      "Remo en máquina",
      "Estiramiento de isquios",
      "Ejercicio",
      "Squat",
    ];
    const shown = screen
      .getAllByText(new RegExp(`^(${names.join("|")})$`))
      .map((element) => element.props.children);
    expect(shown).toEqual(names);
  });

  it("labels each exercise's progress from its set records against its sets", async () => {
    // Arrange & Act
    const { dependencies } = setup();
    await renderWithProviders(<WorkoutView />, dependencies);

    // Assert
    await screen.findByText("Press inclinado");
    expect(iconLabels(PROGRESS_LABELS)).toEqual([
      "Completado",
      "Completado",
      "En curso",
      "Sin empezar",
      "Sin empezar",
      "Completado",
    ]);
  });

  it("shows each exercise's type icon, and none for an unknown type", async () => {
    // Arrange & Act
    const { dependencies } = setup();
    await renderWithProviders(<WorkoutView />, dependencies);

    // Assert
    await screen.findByText("Press inclinado");
    expect(iconLabels(TYPE_LABELS)).toEqual([
      "Fuerza",
      "Fuerza",
      "Cardio",
      "Estiramiento",
      "Fuerza",
    ]);
  });

  it("shows a details caret on every exercise", async () => {
    // Arrange & Act
    const { dependencies } = setup();
    await renderWithProviders(<WorkoutView />, dependencies);

    // Assert
    await screen.findByText("Press inclinado");
    expect(iconLabels(/^Ver detalle$/)).toHaveLength(6);
  });

  it("shows 0 / 0 ejercicios and an empty bar for a workout without exercises", async () => {
    // Arrange & Act
    const { dependencies } = setup({ workoutResult: async () => ok(workout({ exercises: [] })) });
    await renderWithProviders(<WorkoutView />, dependencies);

    // Assert
    expect(await screen.findByText("0 / 0 ejercicios")).toBeOnTheScreen();
    expect(screen.getByRole("progressbar")).toHaveProp(
      "accessibilityValue",
      expect.objectContaining({ now: 0 }),
    );
  });

  it("goes back to the list when the back button is pressed", async () => {
    // Arrange
    const { dependencies } = setup();
    await renderWithProviders(<WorkoutView />, dependencies);
    await screen.findByText("Press inclinado");

    // Act
    await fireEvent.press(screen.getByRole("button", { name: "Volver" }));

    // Assert
    expect(mockBack).toHaveBeenCalledTimes(1);
  });

  it("shows the error, retry and back when the workout cannot be read", async () => {
    // Arrange & Act
    const { dependencies } = setup({
      workoutResult: async () => fail(new DomainError("NETWORK", "")),
    });
    await renderWithProviders(<WorkoutView />, dependencies);

    // Assert
    expect(await screen.findByRole("button", { name: "Reintentar" })).toBeOnTheScreen();
    expect(screen.getByRole("button", { name: "Volver" })).toBeOnTheScreen();
    expect(screen.queryByText("Tren superior A")).not.toBeOnTheScreen();
  });

  it("reads the workout again on retry and shows it", async () => {
    // Arrange
    const results = [fail(new DomainError("NETWORK", "")), ok(workout())];
    const { getWorkout, dependencies } = setup({
      workoutResult: async () => results.shift() ?? ok(workout()),
    });
    await renderWithProviders(<WorkoutView />, dependencies);
    await screen.findByRole("button", { name: "Reintentar" });

    // Act
    await fireEvent.press(screen.getByRole("button", { name: "Reintentar" }));

    // Assert
    expect(await screen.findByRole("heading", { name: "Tren superior A" })).toBeOnTheScreen();
    expect(getWorkout).toHaveBeenCalledTimes(2);
  });

  it("shows the subtitle and the progress in English", async () => {
    // Arrange
    const { dependencies } = setup();
    await renderWithProviders(<WorkoutView />, dependencies);
    await screen.findByText("Press inclinado");

    // Act
    await act(async () => {
      await i18n.changeLanguage("en");
    });

    // Assert
    expect(screen.getByText("Hipertrofia otoño · Week 3")).toBeOnTheScreen();
    expect(screen.getByText("3 / 6 exercises")).toBeOnTheScreen();
    expect(screen.getByText("Incline press")).toBeOnTheScreen();
  });
  it("opens the exercise screen when a row is pressed", async () => {
    // Arrange
    const { dependencies } = setup();
    await renderWithProviders(<WorkoutView />, dependencies);
    await screen.findByText("Press inclinado");

    // Act
    await fireEvent.press(screen.getByText("Press inclinado"));

    // Assert
    expect(mockPush).toHaveBeenCalledTimes(1);
    expect(mockPush).toHaveBeenCalledWith({
      pathname: "/workouts/[id]/exercises/[exerciseId]",
      params: { id: "wk1", exerciseId: "we1" },
    });
  });

  it("starts a published workout, reads it again and then offers Finalizar", async () => {
    // Arrange
    const workoutResult = jest
      .fn<Promise<Result<WorkoutById, DomainError>>, []>()
      .mockResolvedValueOnce(ok(workout({ status: "PUBLISHED" })))
      .mockResolvedValueOnce(ok(workout({ status: "STARTED" })));
    const { getWorkout, changeStatus, dependencies } = setup({ workoutResult });
    await renderWithProviders(<WorkoutView />, dependencies);
    await screen.findByRole("heading", { name: "Tren superior A" });

    // Act
    await fireEvent.press(screen.getByRole("button", { name: "Iniciar entrenamiento" }));

    // Assert
    expect(changeStatus).toHaveBeenCalledTimes(1);
    expect(changeStatus).toHaveBeenCalledWith({ workoutId: "wk1", status: "STARTED" });
    expect(await screen.findByRole("button", { name: "Finalizar entrenamiento" })).toBeEnabled();
    expect(screen.queryByRole("button", { name: "Iniciar entrenamiento" })).not.toBeOnTheScreen();
    expect(getWorkout).toHaveBeenCalledTimes(2);
  });

  it.each(["COMPLETED", "REVIEWED"])(
    "shows a disabled Finalizado for a %s workout",
    async (status) => {
      // Arrange
      const { dependencies } = setup({
        workoutResult: async () => ok(workout({ status } as never)),
      });

      // Act
      await renderWithProviders(<WorkoutView />, dependencies);

      // Assert
      expect(await screen.findByRole("button", { name: "Finalizado" })).toBeDisabled();
      expect(screen.queryByRole("button", { name: "Iniciar entrenamiento" })).not.toBeOnTheScreen();
      expect(
        screen.queryByRole("button", { name: "Finalizar entrenamiento" }),
      ).not.toBeOnTheScreen();
    },
  );

  it("offers Finalizar, and not Iniciar, for a started workout", async () => {
    // Arrange
    const { dependencies } = setup();

    // Act
    await renderWithProviders(<WorkoutView />, dependencies);

    // Assert
    expect(await screen.findByRole("button", { name: "Finalizar entrenamiento" })).toBeEnabled();
    expect(screen.queryByRole("button", { name: "Iniciar entrenamiento" })).not.toBeOnTheScreen();
  });

  it("asks before finishing and sends nothing until it is confirmed", async () => {
    // Arrange
    const { changeStatus, dependencies } = setup();
    await renderWithProviders(<WorkoutView />, dependencies);

    // Act
    await fireEvent.press(await screen.findByRole("button", { name: "Finalizar entrenamiento" }));

    // Assert
    expect(await screen.findByText("¿Finalizar el entrenamiento?")).toBeOnTheScreen();
    expect(
      screen.getByText(
        "Se enviará a tu entrenador para su revisión. Podrás cambiar los registros hasta que lo revise.",
      ),
    ).toBeOnTheScreen();
    expect(changeStatus).not.toHaveBeenCalled();
  });

  it("closes the question without finishing when it is cancelled", async () => {
    // Arrange
    const { changeStatus, dependencies } = setup();
    await renderWithProviders(<WorkoutView />, dependencies);
    await fireEvent.press(await screen.findByRole("button", { name: "Finalizar entrenamiento" }));
    await screen.findByText("¿Finalizar el entrenamiento?");

    // Act
    await fireEvent.press(screen.getByRole("button", { name: "Cancelar" }));

    // Assert
    expect(changeStatus).not.toHaveBeenCalled();
    await waitFor(() =>
      expect(screen.queryByText("¿Finalizar el entrenamiento?")).not.toBeOnTheScreen(),
    );
  });

  it("finishes the workout once confirmed and reads it again", async () => {
    // Arrange
    const workoutResult = jest
      .fn<Promise<Result<WorkoutById, DomainError>>, []>()
      .mockResolvedValueOnce(ok(workout({ status: "STARTED" })))
      .mockResolvedValueOnce(ok(workout({ status: "COMPLETED" })));
    const { getWorkout, changeStatus, dependencies } = setup({ workoutResult });
    await renderWithProviders(<WorkoutView />, dependencies);
    await fireEvent.press(await screen.findByRole("button", { name: "Finalizar entrenamiento" }));
    await screen.findByText("¿Finalizar el entrenamiento?");

    // Act
    await fireEvent.press(screen.getByRole("button", { name: "Finalizar" }));

    // Assert
    expect(changeStatus).toHaveBeenCalledTimes(1);
    expect(changeStatus).toHaveBeenCalledWith({ workoutId: "wk1", status: "COMPLETED" });
    expect(await screen.findByRole("button", { name: "Finalizado" })).toBeDisabled();
    expect(getWorkout).toHaveBeenCalledTimes(2);
  });

  it("closes the question and shows the error when finishing fails", async () => {
    // Arrange
    const { getWorkout, dependencies } = setup({
      statusResult: async () => fail(new DomainError("CONFLICT", "Not allowed")),
    });
    await renderWithProviders(<WorkoutView />, dependencies);
    await fireEvent.press(await screen.findByRole("button", { name: "Finalizar entrenamiento" }));
    await screen.findByText("¿Finalizar el entrenamiento?");

    // Act
    await fireEvent.press(screen.getByRole("button", { name: "Finalizar" }));

    // Assert
    expect(await screen.findByText(i18n.t("errors:CONFLICT"))).toBeOnTheScreen();
    await waitFor(() =>
      expect(screen.queryByText("¿Finalizar el entrenamiento?")).not.toBeOnTheScreen(),
    );
    expect(getWorkout).toHaveBeenCalledTimes(1);
  });

  it("words the question for a workout without a trainer", async () => {
    // Arrange
    const { dependencies } = setup({
      workoutResult: async () => ok(workout({ trainerId: null, trainer: null })),
    });
    await renderWithProviders(<WorkoutView />, dependencies);

    // Act
    await fireEvent.press(await screen.findByRole("button", { name: "Finalizar entrenamiento" }));

    // Assert
    expect(
      await screen.findByText("Quedará completado. Podrás seguir cambiando los registros."),
    ).toBeOnTheScreen();
  });

  it("shows no start button for a draft workout", async () => {
    // Arrange
    const { dependencies } = setup({ workoutResult: async () => ok(workout({ status: "DRAFT" })) });

    // Act
    await renderWithProviders(<WorkoutView />, dependencies);

    // Assert
    await screen.findByRole("heading", { name: "Tren superior A" });
    expect(screen.queryByRole("button", { name: "Iniciar entrenamiento" })).not.toBeOnTheScreen();
    expect(screen.queryByRole("button", { name: "Finalizar entrenamiento" })).not.toBeOnTheScreen();
    expect(screen.queryByRole("button", { name: "Finalizado" })).not.toBeOnTheScreen();
  });

  it("shows the error and keeps Iniciar when starting fails", async () => {
    // Arrange
    const { getWorkout, dependencies } = setup({
      workoutResult: async () => ok(workout({ status: "PUBLISHED" })),
      statusResult: async () => fail(new DomainError("CONFLICT", "Not allowed")),
    });
    await renderWithProviders(<WorkoutView />, dependencies);
    await screen.findByRole("heading", { name: "Tren superior A" });

    // Act
    await fireEvent.press(screen.getByRole("button", { name: "Iniciar entrenamiento" }));

    // Assert
    expect(await screen.findByText(i18n.t("errors:CONFLICT"))).toBeOnTheScreen();
    expect(screen.getByRole("button", { name: "Iniciar entrenamiento" })).toBeEnabled();
    expect(getWorkout).toHaveBeenCalledTimes(1);
  });

  it("shows the start button in English", async () => {
    // Arrange
    const { dependencies } = setup({
      workoutResult: async () => ok(workout({ status: "PUBLISHED" })),
    });
    await renderWithProviders(<WorkoutView />, dependencies);
    await screen.findByRole("heading", { name: "Tren superior A" });

    // Act
    await act(async () => {
      await i18n.changeLanguage("en");
    });

    // Assert
    expect(screen.getByRole("button", { name: "Start workout" })).toBeOnTheScreen();
  });
  it("reads the workout again, keeping what is shown, when the screen is focused again", async () => {
    // Arrange
    // The fourth exercise has no records; after the first one is recorded it is in progress.
    const started = exercises.map((exercise, index) =>
      index === 3 ? { ...exercise, setRecords: records(1) } : exercise,
    );
    const workoutResult = jest
      .fn<Promise<Result<WorkoutById, DomainError>>, []>()
      .mockResolvedValueOnce(ok(workout()))
      .mockResolvedValueOnce(ok(workout({ exercises: started })));
    const { getWorkout, dependencies } = setup({ workoutResult });
    await renderWithProviders(<WorkoutView />, dependencies);
    await screen.findByText("Estiramiento de isquios");
    expect(getWorkout).toHaveBeenCalledTimes(1);
    const before = iconLabels(PROGRESS_LABELS);

    // Act
    await act(async () => {
      mockFocus.current?.();
    });

    // Assert
    expect(getWorkout).toHaveBeenCalledTimes(2);
    expect(iconLabels(PROGRESS_LABELS)).not.toEqual(before);
  });
  it("shows only the body part when it has a single muscle in the catalog", async () => {
    // Arrange
    const { dependencies } = setup();

    // Act
    await renderWithProviders(<WorkoutView />, dependencies);

    // Assert
    await screen.findByText("Press inclinado");
    await waitFor(() => expect(screen.getAllByText("Pecho")).toHaveLength(6));
  });

  it("shows the body part and the muscle when the body part has several", async () => {
    // Arrange
    const legs = {
      ...workoutExercise("we1", "STRENGTH", { es: "Sentadilla" }, [3], 0),
      bodyPart: "LEGS",
      muscle: "QUADRICEPS",
    };
    const { dependencies } = setup({
      workoutResult: async () => ok(workout({ exercises: [legs] })),
    });

    // Act
    await renderWithProviders(<WorkoutView />, dependencies);

    // Assert
    expect(await screen.findByText("Piernas")).toBeOnTheScreen();
    expect(screen.getByText("Cuádriceps")).toBeOnTheScreen();
  });

  it("shows both chips when the catalog cannot be read", async () => {
    // Arrange
    const { dependencies } = setup({
      catalogResult: async () => fail(new DomainError("NETWORK", "")),
    });

    // Act
    await renderWithProviders(<WorkoutView />, dependencies);

    // Assert
    await screen.findByText("Press inclinado");
    expect(screen.getAllByText("Pecho")).toHaveLength(12);
  });

  it("shows the chips in English", async () => {
    // Arrange
    const legs = {
      ...workoutExercise("we1", "STRENGTH", { es: "Sentadilla" }, [3], 0),
      bodyPart: "LEGS",
      muscle: "QUADRICEPS",
    };
    const { dependencies } = setup({
      workoutResult: async () => ok(workout({ exercises: [legs] })),
    });
    await renderWithProviders(<WorkoutView />, dependencies);
    await screen.findByText("Piernas");

    // Act
    await act(async () => {
      await i18n.changeLanguage("en");
    });

    // Assert
    expect(screen.getByText("Legs")).toBeOnTheScreen();
    expect(screen.getByText("Quadriceps")).toBeOnTheScreen();
  });
});
