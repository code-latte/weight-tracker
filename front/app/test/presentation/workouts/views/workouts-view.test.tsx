import { act, fireEvent, screen } from "@testing-library/react-native";
import { DomainError, fail, ok } from "@weight-tracker/core/domain";
import type { Result, UserProfile, WorkoutListItem } from "@weight-tracker/core/domain";
import { MIN_REFRESH_MS } from "@/presentation/workouts/hooks/use-week-workouts";
import WorkoutsView from "@/presentation/workouts/views/workouts-view";
import renderWithProviders from "../../../support/render-with-providers";

const mockPush = jest.fn();

jest.mock("expo-router", () => ({
  router: { push: (href: unknown) => mockPush(href) },
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

function workout(
  id: string,
  name: string,
  workoutDate: string,
  status: string,
  prescribed = true,
  completedAt: string | null = null,
): WorkoutListItem {
  return {
    id,
    name,
    description: null,
    workoutDate,
    userId: athlete.id,
    trainerId: prescribed ? trainer.id : null,
    status,
    exercises: [],
    athlete,
    trainer: prescribed ? trainer : null,
    mesocycleId: null,
    mesocycleWeekNumber: null,
    completedAt,
    reviewedAt: null,
    reviewedBy: null,
    resultsUpdatedAt: null,
  };
}

/** The week of Monday 9 Nov 2026, out of order, between workouts of the weeks around it. */
const workouts: WorkoutListItem[] = [
  workout("w1", "Pierna", "2026-11-13", "PUBLISHED"),
  // Planned for Monday, finished on Tuesday evening.
  workout(
    "w2",
    "Empuje",
    "2026-11-09",
    "REVIEWED",
    true,
    new Date(2026, 10, 10, 19, 30).toISOString(),
  ),
  // Planned for Wednesday, finished on Friday morning.
  workout(
    "w3",
    "Tirón",
    "2026-11-11",
    "COMPLETED",
    true,
    new Date(2026, 10, 13, 8, 0).toISOString(),
  ),
  workout("w4", "Core", "2026-11-12", "STARTED"),
  workout("w5", "Carrera", "2026-11-15", "DRAFT", false),
  workout("w6", "Semana pasada", "2026-11-08", "COMPLETED"),
  workout("w7", "Semana próxima", "2026-11-16", "PUBLISHED"),
];

interface SetupOptions {
  workoutsResult?: () => Promise<Result<WorkoutListItem[], DomainError>>;
}

function setup({ workoutsResult = async () => ok(workouts) }: SetupOptions = {}) {
  const getWorkouts = jest.fn(workoutsResult);
  const dependencies = {
    getWorkoutsUseCase: () => ({ execute: getWorkouts }),
  };

  return { getWorkouts, dependencies };
}

/** Pulls the list down, as the platform's refresh control would. */
async function pullToRefresh() {
  const list = screen.getByTestId("workouts-list");
  await act(async () => {
    list.props.refreshControl.props.onRefresh();
  });
}

function isRefreshing(): boolean {
  return screen.getByTestId("workouts-list").props.refreshControl.props.refreshing;
}

describe("WorkoutsView", () => {
  beforeEach(() => {
    // Only the clock is faked: today is Monday 9 Nov 2026.
    jest.useFakeTimers({
      now: new Date(2026, 10, 9, 10, 0),
      doNotFake: [
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
        "setInterval",
        "clearInterval",
        "setTimeout",
        "clearTimeout",
      ],
    });
  });

  afterEach(() => {
    jest.useRealTimers();
  });

  it("shows a loading indicator while the workouts are read", async () => {
    // Arrange & Act
    const { dependencies } = setup({ workoutsResult: () => new Promise(() => {}) });
    await renderWithProviders(<WorkoutsView />, dependencies);

    // Assert
    expect(screen.getAllByRole("progressbar").length).toBeGreaterThan(0);
    expect(screen.queryByText("Empuje")).not.toBeOnTheScreen();
  });

  it("opens on the current week with its title, range and progress", async () => {
    // Arrange & Act
    const { dependencies } = setup();
    await renderWithProviders(<WorkoutsView />, dependencies);

    // Assert
    expect(await screen.findByText("Empuje")).toBeOnTheScreen();
    expect(screen.getByRole("heading", { name: "Esta semana" })).toBeOnTheScreen();
    expect(screen.getAllByText("Lun, 9 nov - Dom, 15 nov")).toHaveLength(2);
    expect(screen.getByText("2 / 5")).toBeOnTheScreen();
    expect(screen.getByRole("progressbar")).toHaveProp(
      "accessibilityValue",
      expect.objectContaining({ now: 40 }),
    );
  });

  it("lists only the week's workouts, in date order", async () => {
    // Arrange & Act
    const { dependencies } = setup();
    await renderWithProviders(<WorkoutsView />, dependencies);

    // Assert
    await screen.findByText("Empuje");
    const shown = screen
      .getAllByText(/^(Empuje|Tirón|Core|Pierna|Carrera|Semana pasada|Semana próxima)$/)
      .map((element) => element.props.children);
    expect(shown).toEqual(["Empuje", "Tirón", "Core", "Pierna", "Carrera"]);
  });

  it("labels each status icon, with drafts as pending", async () => {
    // Arrange & Act
    const { dependencies } = setup();
    await renderWithProviders(<WorkoutsView />, dependencies);

    // Assert
    await screen.findByText("Empuje");
    const labels = screen.getAllByRole("image").map((element) => element.props.accessibilityLabel);
    expect(labels).toEqual(["Revisado", "Completado", "En curso", "Pendiente", "Pendiente"]);
  });

  it("shows the trainer as author, or Creado por mí without the athlete's name", async () => {
    // Arrange & Act
    const { dependencies } = setup();
    await renderWithProviders(<WorkoutsView />, dependencies);

    // Assert
    await screen.findByText("Empuje");
    expect(screen.getAllByText("Carlos Ruiz")).toHaveLength(4);
    expect(screen.getByText("Creado por mí")).toBeOnTheScreen();
    expect(screen.queryByText("Ana López")).not.toBeOnTheScreen();
  });

  it("opens a workout when its card is tapped", async () => {
    // Arrange
    mockPush.mockClear();
    const { dependencies } = setup();
    await renderWithProviders(<WorkoutsView />, dependencies);
    await screen.findByText("Empuje");

    // Act
    await fireEvent.press(screen.getByText("Tirón"));

    // Assert
    expect(mockPush).toHaveBeenCalledWith({ pathname: "/workouts/[id]", params: { id: "w3" } });
  });

  it("shows the weekday a done workout was completed on, and -- for the others", async () => {
    // Arrange & Act
    const { dependencies } = setup();
    await renderWithProviders(<WorkoutsView />, dependencies);

    // Assert
    await screen.findByText("Empuje");
    expect(screen.getByText("mar")).toBeOnTheScreen();
    expect(screen.getByText("vie")).toBeOnTheScreen();
    expect(screen.queryByText("lun")).not.toBeOnTheScreen();
    expect(screen.queryByText("mié")).not.toBeOnTheScreen();
    expect(screen.getAllByText("--")).toHaveLength(3);
  });

  it("shows -- for a done workout without a completion time", async () => {
    // Arrange
    const { dependencies } = setup({
      workoutsResult: async () => ok([workout("w3", "Tirón", "2026-11-11", "COMPLETED")]),
    });

    // Act
    await renderWithProviders(<WorkoutsView />, dependencies);

    // Assert
    await screen.findByText("Tirón");
    expect(screen.getByText("--")).toBeOnTheScreen();
    expect(screen.queryByText("mié")).not.toBeOnTheScreen();
  });

  it("shows Próxima semana and its range after the right caret", async () => {
    // Arrange
    const { dependencies } = setup();
    await renderWithProviders(<WorkoutsView />, dependencies);
    await screen.findByText("Empuje");

    // Act
    await fireEvent.press(screen.getByRole("button", { name: "Semana siguiente" }));

    // Assert
    expect(screen.getByRole("heading", { name: "Próxima semana" })).toBeOnTheScreen();
    expect(screen.getAllByText("Lun, 16 nov - Dom, 22 nov")).toHaveLength(2);
    expect(screen.getByText("Semana próxima")).toBeOnTheScreen();
    expect(screen.getByText("0 / 1")).toBeOnTheScreen();
  });

  it("titles any other week with its range and its year", async () => {
    // Arrange
    const { dependencies } = setup();
    await renderWithProviders(<WorkoutsView />, dependencies);
    await screen.findByText("Empuje");

    // Act
    await fireEvent.press(screen.getByRole("button", { name: "Semana anterior" }));

    // Assert
    expect(screen.getByRole("heading", { name: "Lun, 2 nov - Dom, 8 nov" })).toBeOnTheScreen();
    expect(screen.getByText("2026")).toBeOnTheScreen();
    expect(screen.getAllByText("Lun, 2 nov - Dom, 8 nov")).toHaveLength(2);
    expect(screen.getByText("Semana pasada")).toBeOnTheScreen();
  });

  it("returns to Esta semana from next week with the left caret", async () => {
    // Arrange
    const { dependencies } = setup();
    await renderWithProviders(<WorkoutsView />, dependencies);
    await screen.findByText("Empuje");
    await fireEvent.press(screen.getByRole("button", { name: "Semana siguiente" }));

    // Act
    await fireEvent.press(screen.getByRole("button", { name: "Semana anterior" }));

    // Assert
    expect(screen.getByRole("heading", { name: "Esta semana" })).toBeOnTheScreen();
    expect(screen.getByText("Empuje")).toBeOnTheScreen();
  });

  it("shows the empty line and 0 / 0 for a week without workouts", async () => {
    // Arrange
    const { dependencies } = setup();
    await renderWithProviders(<WorkoutsView />, dependencies);
    await screen.findByText("Empuje");
    await fireEvent.press(screen.getByRole("button", { name: "Semana siguiente" }));

    // Act
    await fireEvent.press(screen.getByRole("button", { name: "Semana siguiente" }));

    // Assert
    expect(screen.getByText("No tienes entrenamientos esta semana")).toBeOnTheScreen();
    expect(screen.getByText("0 / 0")).toBeOnTheScreen();
    expect(screen.getByRole("heading", { name: "Lun, 23 nov - Dom, 29 nov" })).toBeOnTheScreen();
  });

  it("does not read the workouts again when the week changes", async () => {
    // Arrange
    const { getWorkouts, dependencies } = setup();
    await renderWithProviders(<WorkoutsView />, dependencies);
    await screen.findByText("Empuje");

    // Act
    await fireEvent.press(screen.getByRole("button", { name: "Semana siguiente" }));

    // Assert
    expect(getWorkouts).toHaveBeenCalledTimes(1);
  });

  it("shows the error and retry when the workouts cannot be read", async () => {
    // Arrange & Act
    const { dependencies } = setup({
      workoutsResult: async () => fail(new DomainError("NETWORK", "")),
    });
    await renderWithProviders(<WorkoutsView />, dependencies);

    // Assert
    expect(await screen.findByText("No hay conexión con el servidor.")).toBeOnTheScreen();
    expect(screen.getByRole("button", { name: "Reintentar" })).toBeOnTheScreen();
    expect(screen.queryByText("Empuje")).not.toBeOnTheScreen();
  });

  it("reads the workouts again on retry and shows the week", async () => {
    // Arrange
    const results = [fail(new DomainError("NETWORK", "")), ok(workouts)];
    const { getWorkouts, dependencies } = setup({
      workoutsResult: async () => results.shift() ?? ok(workouts),
    });
    await renderWithProviders(<WorkoutsView />, dependencies);
    await screen.findByText("No hay conexión con el servidor.");

    // Act
    await fireEvent.press(screen.getByRole("button", { name: "Reintentar" }));

    // Assert
    expect(await screen.findByText("Empuje")).toBeOnTheScreen();
    expect(getWorkouts).toHaveBeenCalledTimes(2);
  });

  describe("pull to refresh", () => {
    beforeEach(() => {
      // Timers are faked too, so the minimum refresh delay can be stepped through.
      jest.useFakeTimers({ now: new Date(2026, 10, 9, 10, 0) });
    });

    it("reads the workouts again and shows what changed, keeping the viewed week", async () => {
      // Arrange
      const refreshed = [workout("w8", "Semana próxima nueva", "2026-11-17", "COMPLETED")];
      const results = [ok(workouts), ok(refreshed)];
      const { getWorkouts, dependencies } = setup({
        workoutsResult: async () => results.shift() ?? ok(refreshed),
      });
      await renderWithProviders(<WorkoutsView />, dependencies);
      await screen.findByText("Empuje");
      await fireEvent.press(screen.getByRole("button", { name: "Semana siguiente" }));

      // Act
      await pullToRefresh();
      await act(async () => {
        jest.advanceTimersByTime(MIN_REFRESH_MS);
      });

      // Assert
      expect(getWorkouts).toHaveBeenCalledTimes(2);
      expect(screen.getByRole("heading", { name: "Próxima semana" })).toBeOnTheScreen();
      expect(screen.getByText("Semana próxima nueva")).toBeOnTheScreen();
      expect(screen.queryByText("Semana próxima")).not.toBeOnTheScreen();
      expect(screen.getByText("1 / 1")).toBeOnTheScreen();
      expect(isRefreshing()).toBe(false);
    });

    it("keeps the spinner and the current list until the minimum delay has passed", async () => {
      // Arrange
      const { dependencies } = setup();
      await renderWithProviders(<WorkoutsView />, dependencies);
      await screen.findByText("Empuje");

      // Act
      await pullToRefresh();
      await act(async () => {
        jest.advanceTimersByTime(MIN_REFRESH_MS - 1);
      });

      // Assert
      expect(isRefreshing()).toBe(true);
      expect(screen.getByText("Empuje")).toBeOnTheScreen();
    });

    it("shows the error and retry when the refresh fails", async () => {
      // Arrange
      const results = [ok(workouts), fail(new DomainError("NETWORK", ""))];
      const { dependencies } = setup({
        workoutsResult: async () => results.shift() ?? ok(workouts),
      });
      await renderWithProviders(<WorkoutsView />, dependencies);
      await screen.findByText("Empuje");

      // Act
      await pullToRefresh();
      await act(async () => {
        jest.advanceTimersByTime(MIN_REFRESH_MS);
      });

      // Assert
      expect(screen.getByText("No hay conexión con el servidor.")).toBeOnTheScreen();
      expect(screen.getByRole("button", { name: "Reintentar" })).toBeOnTheScreen();
    });
  });
});
