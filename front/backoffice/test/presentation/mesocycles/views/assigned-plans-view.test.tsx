import { act, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterAll, afterEach, beforeAll, beforeEach, describe, expect, it, vi } from "vitest";
import { DomainError, fail, ok } from "@weight-tracker/core/domain";
import type { Mesocycle, Result, UserProfile } from "@weight-tracker/core/domain";
import i18n from "@/infrastructure/i18n";
import AssignedPlansView from "@/presentation/mesocycles/views/assigned-plans-view";
import renderWithProviders, { completeProfile } from "../../../support/render-with-providers";

const replace = vi.fn();
const push = vi.fn();

vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace, push }),
  usePathname: () => "/assigned-plans",
}));

const adminProfile: UserProfile = { ...completeProfile, email: "admin@example.com", role: "ADMIN" };

const ana = { id: "u-1", email: "ana@example.com", name: "Ana", surname: "García", role: "USER" };
const luis = {
  id: "t-1",
  email: "luis@example.com",
  name: "Luis",
  surname: "Pérez",
  role: "TRAINER",
};

/** An athlete with neither name nor surname, who is shown by email. */
const nameless = { id: "u-2", email: "new@example.com", name: "", surname: "", role: "USER" };

const hypertrophy: Mesocycle = {
  id: "7b0c1f7e-3a54-4d4e-9d0e-1a2b3c4d5e6f",
  trainerId: "t-1",
  athleteId: "u-1",
  name: "Hipertrofia",
  objective: "Ganar masa muscular",
  startDate: "2026-11-03",
  durationInWeeks: 4,
  weeks: [
    { weekNumber: 1, trainingWeekType: "ADAPTATION", objective: "Cargas iniciales" },
    { weekNumber: 2, trainingWeekType: "ACCUMULATION", objective: "Subir volumen" },
  ],
  trainer: luis,
  athlete: ana,
};

const strength: Mesocycle = {
  ...hypertrophy,
  id: "8c1d2e3f-4a5b-4c6d-8e7f-9a0b1c2d3e4f",
  athleteId: "u-2",
  name: "Fuerza",
  startDate: "2026-12-01",
  durationInWeeks: 1,
  weeks: [],
  athlete: nameless,
};

function renderAssignedPlans({
  profile = completeProfile,
  getMesocycles = async () => ok([hypertrophy, strength]),
}: {
  profile?: UserProfile;
  getMesocycles?: () => Promise<Result<Mesocycle[], DomainError>>;
} = {}) {
  const execute = vi.fn(getMesocycles);
  const getWorkouts = vi.fn(async () => ok([]));

  return {
    execute,
    getWorkouts,
    rendered: renderWithProviders(<AssignedPlansView />, {
      getSessionUseCase: () => ({
        execute: async () => ok({ accessToken: "a", refreshToken: "r" }),
      }),
      getMyProfileUseCase: () => ({ execute: async () => ok(profile) }),
      getMesocyclesUseCase: () => ({ execute }),
      getWorkoutsUseCase: () => ({ execute: getWorkouts }),
    }),
  };
}

function bodyRows() {
  const [, ...rows] = screen.getAllByRole("row");
  return rows;
}

function cellsOf(row: HTMLElement) {
  return within(row)
    .getAllByRole("cell")
    .map((cell) => cell.textContent);
}

describe("AssignedPlansView", () => {
  // A zone behind UTC, where reading "YYYY-MM-DD" as UTC midnight would show the previous day.
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
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  it("shows the title, the button and one row per mesocycle in the API's order", async () => {
    // Arrange
    const { getWorkouts, rendered } = renderAssignedPlans();

    // Act
    await rendered;

    // Assert
    expect(
      await screen.findByRole("heading", { level: 1, name: "Mesociclos" }),
    ).toBeInTheDocument();
    await screen.findByText("Hipertrofia");
    const rows = bodyRows();
    expect(rows).toHaveLength(2);
    expect(within(rows[0]).getByText("Hipertrofia")).toBeInTheDocument();
    expect(within(rows[1]).getByText("Fuerza")).toBeInTheDocument();
    expect(getWorkouts).not.toHaveBeenCalled();
  });

  it("shows the name, the athlete, the start date, the duration and the defined weeks", async () => {
    // Arrange & Act
    await renderAssignedPlans().rendered;

    // Assert
    await screen.findByText("Hipertrofia");
    const [hypertrophyRow, strengthRow] = bodyRows();
    expect(cellsOf(hypertrophyRow)).toEqual([
      "Hipertrofia",
      "Ana García",
      "3 nov 2026",
      "4 semanas",
      "2 / 4",
      "",
    ]);
    expect(within(strengthRow).getAllByRole("cell")[3]).toHaveTextContent("1 semana");
  });

  it("shows the email of an athlete without a name and no athlete id", async () => {
    // Arrange & Act
    await renderAssignedPlans().rendered;

    // Assert
    await screen.findByText("Hipertrofia");
    expect(within(bodyRows()[1]).getAllByRole("cell")[1]).toHaveTextContent("new@example.com");
    expect(screen.queryByText("u-1")).not.toBeInTheDocument();
    expect(screen.queryByText("u-2")).not.toBeInTheDocument();
  });

  it("shows the trainer column to an admin with the trainer's name", async () => {
    // Arrange & Act
    await renderAssignedPlans({ profile: adminProfile }).rendered;

    // Assert
    expect(await screen.findByRole("columnheader", { name: "Entrenador" })).toBeInTheDocument();
    expect(cellsOf(bodyRows()[0])).toEqual([
      "Hipertrofia",
      "Ana García",
      "Luis Pérez",
      "3 nov 2026",
      "4 semanas",
      "2 / 4",
      "",
    ]);
    expect(screen.queryByText("t-1")).not.toBeInTheDocument();
  });

  it("hides the trainer column and every trainer name from a trainer", async () => {
    // Arrange & Act
    await renderAssignedPlans({ profile: completeProfile }).rendered;

    // Assert
    await screen.findByText("Hipertrofia");
    expect(screen.queryByRole("columnheader", { name: "Entrenador" })).not.toBeInTheDocument();
    expect(screen.queryByText("Luis Pérez")).not.toBeInTheDocument();
    expect(within(bodyRows()[0]).getAllByRole("cell")).toHaveLength(6);
  });

  it("shows the empty message and no rows when there are no mesocycles", async () => {
    // Arrange & Act
    await renderAssignedPlans({ getMesocycles: async () => ok([]) }).rendered;

    // Assert
    expect(await screen.findByText("Todavía no hay mesociclos.")).toBeInTheDocument();
    expect(screen.queryByRole("table")).not.toBeInTheDocument();
  });

  it("shows the skeleton and no table while the mesocycles load", async () => {
    // Arrange & Act
    await renderAssignedPlans({ getMesocycles: () => new Promise(() => {}) }).rendered;

    // Assert
    expect(await screen.findByLabelText("Cargando")).toBeInTheDocument();
    expect(screen.queryByRole("table")).not.toBeInTheDocument();
  });

  it("shows the error and reads the mesocycles again when Reintentar is pressed", async () => {
    // Arrange
    const getMesocycles = vi
      .fn<() => Promise<Result<Mesocycle[], DomainError>>>()
      .mockResolvedValueOnce(fail(new DomainError("NETWORK", "")))
      .mockResolvedValueOnce(ok([hypertrophy]));
    const { execute, rendered } = renderAssignedPlans({ getMesocycles });
    await rendered;
    const user = userEvent.setup();
    await screen.findByText("No hay conexión con el servidor.");

    // Act
    await user.click(screen.getByRole("button", { name: "Reintentar" }));

    // Assert
    expect(await screen.findByText("Hipertrofia")).toBeInTheDocument();
    expect(screen.queryByText("No hay conexión con el servidor.")).not.toBeInTheDocument();
    expect(execute).toHaveBeenCalledTimes(2);
  });

  it("links Nuevo mesociclo to the create screen", async () => {
    // Arrange & Act
    await renderAssignedPlans().rendered;

    // Assert
    expect(await screen.findByRole("link", { name: "Nuevo mesociclo" })).toHaveAttribute(
      "href",
      "/mesocycle",
    );
  });

  it("links each row's edit action to that mesocycle's screen", async () => {
    // Arrange & Act
    await renderAssignedPlans().rendered;

    // Assert
    expect(await screen.findByRole("link", { name: "Editar Hipertrofia" })).toHaveAttribute(
      "href",
      `/mesocycle?id=${hypertrophy.id}`,
    );
    expect(screen.getByRole("link", { name: "Editar Fuerza" })).toHaveAttribute(
      "href",
      `/mesocycle?id=${strength.id}`,
    );
  });

  it("follows the active language", async () => {
    // Arrange
    await renderAssignedPlans({ profile: adminProfile }).rendered;
    await screen.findByText("Hipertrofia");

    // Act
    await act(() => i18n.changeLanguage("en"));

    // Assert
    expect(screen.getByRole("heading", { level: 1, name: "Mesocycles" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "New mesocycle" })).toBeInTheDocument();
    for (const header of [
      "Name",
      "Athlete",
      "Trainer",
      "Start date",
      "Duration",
      "Defined weeks",
    ]) {
      expect(screen.getByRole("columnheader", { name: header })).toBeInTheDocument();
    }
    expect(screen.getByText("Nov 3, 2026")).toBeInTheDocument();
    expect(screen.getByText("4 weeks")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Edit Hipertrofia" })).toBeInTheDocument();
  });
});
