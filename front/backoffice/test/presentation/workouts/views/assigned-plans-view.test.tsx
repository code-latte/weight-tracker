import { act, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterAll, afterEach, beforeAll, beforeEach, describe, expect, it, vi } from "vitest";
import { DomainError, fail, ok } from "@weight-tracker/core/domain";
import type { Result, UserProfile, Workout } from "@weight-tracker/core/domain";
import i18n from "@/infrastructure/i18n";
import AssignedPlansView from "@/presentation/workouts/views/assigned-plans-view";
import renderWithProviders, { completeProfile } from "../../../support/render-with-providers";

const replace = vi.fn();
const push = vi.fn();

vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace, push }),
  usePathname: () => "/assigned-plans",
}));

const adminProfile: UserProfile = { ...completeProfile, email: "admin@example.com", role: "ADMIN" };

const exercise = {
  id: "0a1b2c3d-4e5f-4a6b-8c7d-9e0f1a2b3c4d",
  exerciseId: "1f2e3d4c-5b6a-4978-8695-a4b3c2d1e0f9",
  translations: [],
  muscle: "QUADRICEPS",
  bodyPart: "LEGS",
  type: "STRENGTH",
  effort: "HIGH",
  perSide: false,
  restMinutes: null,
  sets: [],
  superSeriesId: null,
} satisfies Workout["exercises"][number];

const legs: Workout = {
  id: "7b0c1f7e-3a54-4d4e-9d0e-1a2b3c4d5e6f",
  name: "Pierna",
  description: null,
  workoutDate: "2026-10-27",
  userId: "u-1",
  trainerId: "t-1",
  status: "DRAFT",
  exercises: [exercise, { ...exercise, id: "e-2" }, { ...exercise, id: "e-3" }],
};

const pushDay: Workout = {
  ...legs,
  id: "8c1d2e3f-4a5b-4c6d-8e7f-9a0b1c2d3e4f",
  name: "Empuje",
  workoutDate: "2026-11-03",
  userId: "u-2",
  trainerId: null,
  exercises: [],
};

/** A workout dated `workoutDate`, for the tests about how a date reads as its week. */
function datedWorkout(id: string, workoutDate: string): Workout {
  return { ...pushDay, id, name: `W ${workoutDate}`, workoutDate };
}

function dateCellOf(name: string) {
  const row = bodyRows().find((candidate) => within(candidate).queryByText(name) !== null)!;
  return within(row).getAllByRole("cell")[1]!.textContent;
}

function renderAssignedPlans({
  profile = completeProfile,
  getWorkouts = async () => ok([legs, pushDay]),
}: {
  profile?: UserProfile;
  getWorkouts?: () => Promise<Result<Workout[], DomainError>>;
} = {}) {
  const execute = vi.fn(getWorkouts);

  return {
    execute,
    rendered: renderWithProviders(<AssignedPlansView />, {
      getSessionUseCase: () => ({
        execute: async () => ok({ accessToken: "a", refreshToken: "r" }),
      }),
      getMyProfileUseCase: () => ({ execute: async () => ok(profile) }),
      getWorkoutsUseCase: () => ({ execute }),
    }),
  };
}

function bodyRows() {
  const [, ...rows] = screen.getAllByRole("row");
  return rows;
}

describe("AssignedPlansView", () => {
  // A zone behind UTC, where reading "YYYY-MM-DD" as UTC midnight would show the previous day, and so the week before
  // for a Monday.
  const originalTimeZone = process.env.TZ;
  beforeAll(() => {
    process.env.TZ = "America/New_York";
  });
  afterAll(() => {
    process.env.TZ = originalTimeZone;
  });

  beforeEach(() => {
    replace.mockClear();
    push.mockClear();
    // Only `Date` is faked, so user events and promises keep running; the current year is 2026.
    vi.useFakeTimers({ toFake: ["Date"] });
    vi.setSystemTime(new Date(2026, 9, 29, 12));
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it("shows the title, the button and one row per workout in the API's order", async () => {
    // Arrange & Act
    await renderAssignedPlans().rendered;

    // Assert
    expect(
      await screen.findByRole("heading", { level: 1, name: "Planes asignados" }),
    ).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Nuevo entrenamiento" })).toBeInTheDocument();
    await screen.findByText("Pierna");
    const rows = bodyRows();
    expect(rows).toHaveLength(2);
    expect(within(rows[0]).getByText("Pierna")).toBeInTheDocument();
    expect(within(rows[1]).getByText("Empuje")).toBeInTheDocument();
  });

  it("shows the name, the week, the athlete and the exercise count of each workout", async () => {
    // Arrange & Act
    await renderAssignedPlans().rendered;

    // Assert
    await screen.findByText("Pierna");
    const [legsRow, pushRow] = bodyRows();
    expect(
      within(legsRow)
        .getAllByRole("cell")
        .map((cell) => cell.textContent),
    ).toEqual(["Pierna", "Semana del 26 oct – 1 nov", "u-1", "3", ""]);
    expect(within(pushRow).getAllByRole("cell")[3]).toHaveTextContent("0");
  });

  it("shows the trainer column to an admin, empty when the workout has no trainer", async () => {
    // Arrange & Act
    await renderAssignedPlans({ profile: adminProfile }).rendered;

    // Assert
    expect(await screen.findByRole("columnheader", { name: "Entrenador" })).toBeInTheDocument();
    const [legsRow, pushRow] = bodyRows();
    expect(
      within(legsRow)
        .getAllByRole("cell")
        .map((cell) => cell.textContent),
    ).toEqual(["Pierna", "Semana del 26 oct – 1 nov", "u-1", "t-1", "3", ""]);
    expect(within(pushRow).getAllByRole("cell")[3]).toBeEmptyDOMElement();
  });

  it("hides the trainer column and every trainer id from a trainer", async () => {
    // Arrange & Act
    await renderAssignedPlans({ profile: completeProfile }).rendered;

    // Assert
    await screen.findByText("Pierna");
    expect(screen.queryByRole("columnheader", { name: "Entrenador" })).not.toBeInTheDocument();
    expect(screen.queryByText("t-1")).not.toBeInTheDocument();
    expect(within(bodyRows()[0]).getAllByRole("cell")).toHaveLength(5);
  });

  it("shows the empty message and no rows when there are no workouts", async () => {
    // Arrange & Act
    await renderAssignedPlans({ getWorkouts: async () => ok([]) }).rendered;

    // Assert
    expect(await screen.findByText("Todavía no hay entrenamientos asignados.")).toBeInTheDocument();
    expect(screen.queryByRole("table")).not.toBeInTheDocument();
  });

  it("shows the skeleton and no table while the workouts load", async () => {
    // Arrange & Act
    await renderAssignedPlans({ getWorkouts: () => new Promise(() => {}) }).rendered;

    // Assert
    expect(await screen.findByLabelText("Cargando")).toBeInTheDocument();
    expect(screen.queryByRole("table")).not.toBeInTheDocument();
  });

  it("shows the error and reads the workouts again when Reintentar is pressed", async () => {
    // Arrange
    const getWorkouts = vi
      .fn<() => Promise<Result<Workout[], DomainError>>>()
      .mockResolvedValueOnce(fail(new DomainError("NETWORK", "")))
      .mockResolvedValueOnce(ok([legs]));
    const { execute, rendered } = renderAssignedPlans({ getWorkouts });
    await rendered;
    const user = userEvent.setup();
    await screen.findByText("No hay conexión con el servidor.");

    // Act
    await user.click(screen.getByRole("button", { name: "Reintentar" }));

    // Assert
    expect(await screen.findByText("Pierna")).toBeInTheDocument();
    expect(screen.queryByText("No hay conexión con el servidor.")).not.toBeInTheDocument();
    expect(execute).toHaveBeenCalledTimes(2);
  });

  it("opens the create dialog and sends nothing when Nuevo entrenamiento is pressed", async () => {
    // Arrange
    const { execute, rendered } = renderAssignedPlans();
    await rendered;
    const user = userEvent.setup();
    await screen.findByText("Pierna");

    // Act
    await user.click(screen.getByRole("button", { name: "Nuevo entrenamiento" }));

    // Assert
    expect(await screen.findByRole("dialog", { name: "Nuevo entrenamiento" })).toBeInTheDocument();
    expect(push).not.toHaveBeenCalled();
    expect(replace).not.toHaveBeenCalled();
    expect(execute).toHaveBeenCalledTimes(1);
  });

  it("links each row's edit action to that workout's screen", async () => {
    // Arrange & Act
    await renderAssignedPlans().rendered;

    // Assert
    expect(await screen.findByRole("link", { name: "Editar Pierna" })).toHaveAttribute(
      "href",
      `/workout?id=${legs.id}`,
    );
    expect(screen.getByRole("link", { name: "Editar Empuje" })).toHaveAttribute(
      "href",
      `/workout?id=${pushDay.id}`,
    );
  });

  it("shows the Semana column header", async () => {
    // Arrange & Act
    await renderAssignedPlans().rendered;

    // Assert
    expect(await screen.findByRole("columnheader", { name: "Semana" })).toBeInTheDocument();
    expect(screen.queryByRole("columnheader", { name: "Fecha" })).not.toBeInTheDocument();
  });

  it("shows the same week for a Monday, a Wednesday and a Sunday, behind UTC", async () => {
    // Arrange
    const workouts = ["2026-10-26", "2026-10-28", "2026-11-01"].map((date, index) =>
      datedWorkout(`w-${index}`, date),
    );

    // Act
    await renderAssignedPlans({ getWorkouts: async () => ok(workouts) }).rendered;

    // Assert
    await screen.findByText("W 2026-10-26");
    for (const date of ["2026-10-26", "2026-10-28", "2026-11-01"]) {
      expect(dateCellOf(`W ${date}`)).toBe("Semana del 26 oct – 1 nov");
    }
  });

  it("shows the year of a week in another year", async () => {
    // Arrange
    const workouts = [datedWorkout("w-old", "2025-10-29")];

    // Act
    await renderAssignedPlans({ getWorkouts: async () => ok(workouts) }).rendered;

    // Assert
    await screen.findByText("W 2025-10-29");
    expect(dateCellOf("W 2025-10-29")).toBe("Semana del 27 oct 2025 – 2 nov 2025");
  });

  it("follows the active language", async () => {
    // Arrange
    await renderAssignedPlans({ profile: adminProfile }).rendered;
    await screen.findByText("Pierna");

    // Act
    await act(() => i18n.changeLanguage("en"));

    // Assert
    expect(screen.getByRole("heading", { level: 1, name: "Assigned plans" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "New workout" })).toBeInTheDocument();
    for (const header of ["Name", "Week", "Athlete", "Trainer", "Exercises"]) {
      expect(screen.getByRole("columnheader", { name: header })).toBeInTheDocument();
    }
    expect(screen.getByText("Week of Oct 26 – Nov 1")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Edit Pierna" })).toBeInTheDocument();
  });
});
