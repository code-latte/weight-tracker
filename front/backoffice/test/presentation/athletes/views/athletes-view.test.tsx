import { act, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { DomainError, fail, ok } from "@weight-tracker/core/domain";
import type {
  Result,
  Session,
  TrainerAthlete,
  TrainerAthleteRelationship,
  UserProfile,
} from "@weight-tracker/core/domain";
import i18n from "@/infrastructure/i18n";
import AthletesView from "@/presentation/athletes/views/athletes-view";
import renderWithProviders, { completeProfile } from "../../../support/render-with-providers";

const replace = vi.fn();
const push = vi.fn();

vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace, push }),
  usePathname: () => "/athletes",
}));

const adminProfile: UserProfile = { ...completeProfile, email: "admin@example.com", role: "ADMIN" };

const athlete = (overrides: Partial<TrainerAthlete>): TrainerAthlete => ({
  id: "1b2c3d4e-5f6a-4b7c-8d9e-0f1a2b3c4d5e",
  athleteId: "9a8b7c6d-5e4f-4a3b-8c2d-1e0f9a8b7c6d",
  email: "athlete@example.com",
  name: "",
  surname: "",
  trainerName: "Ana",
  trainerSurname: "García",
  startedAtUtc: "2026-10-03T08:00:00Z",
  ...overrides,
});

const luis = athlete({ id: "a1", email: "luis@example.com", name: "Luis", surname: "Pérez" });
const marta = athlete({ id: "a2", email: "marta@example.com", name: "Marta", surname: "Anaya" });
const nameless = athlete({ id: "a3", email: "nameless@example.com" });

const endedRelationship: TrainerAthleteRelationship = {
  id: luis.id,
  trainerId: completeProfile.id,
  trainerName: "Ana",
  trainerSurname: "García",
  athleteId: luis.athleteId,
  athleteName: "Luis",
  athleteSurname: "Pérez",
  status: "ENDED",
  startedAtUtc: luis.startedAtUtc,
  finishedAtUtc: "2026-11-02T09:00:00Z",
};

function renderAthletes({
  profile = completeProfile,
  getAthletes = async () => ok([luis, marta, nameless]),
  session = async () => ok({ accessToken: "a", refreshToken: "r" }),
  end = async () => ok(endedRelationship),
}: {
  profile?: UserProfile;
  getAthletes?: () => Promise<Result<TrainerAthlete[], DomainError>>;
  session?: () => Promise<Result<Session | null, DomainError>>;
  end?: () => Promise<Result<TrainerAthleteRelationship, DomainError>>;
} = {}) {
  const execute = vi.fn(getAthletes);
  const endExecute = vi.fn(end);

  const rendered = renderWithProviders(<AthletesView />, {
    getSessionUseCase: () => ({ execute: session }),
    getMyProfileUseCase: () => ({ execute: async () => ok(profile) }),
    getMyAthletesUseCase: () => ({ execute }),
    endTrainerAthleteUseCase: () => ({ execute: endExecute }),
  });
  return { execute, endExecute, rendered };
}

/** Opens the ⋮ menu of the athlete's row and picks Finalizar relación. */
async function chooseEnd(user: ReturnType<typeof userEvent.setup>, name: string) {
  await user.click(await screen.findByRole("button", { name: `Acciones de ${name}` }));
  await user.click(await screen.findByRole("menuitem", { name: "Finalizar relación" }));
}

function bodyRows() {
  const [, ...rows] = screen.getAllByRole("row");
  return rows;
}

const emailsShown = () => bodyRows().map((row) => within(row).getAllByRole("cell")[1]!.textContent);

describe("AthletesView", () => {
  beforeEach(() => {
    replace.mockClear();
    push.mockClear();
  });

  it("shows the title and one row per athlete in the API's order", async () => {
    // Arrange & Act
    const { execute } = renderAthletes();

    // Assert
    expect(
      await screen.findByRole("heading", { level: 1, name: "Todos los atletas" }),
    ).toBeInTheDocument();
    await screen.findByText("luis@example.com");
    expect(emailsShown()).toEqual([
      "luis@example.com",
      "marta@example.com",
      "nameless@example.com",
    ]);
    expect(execute).toHaveBeenCalledTimes(1);
  });

  it("sends a visitor without a session to login and reads no athletes", async () => {
    // Arrange & Act
    const { execute, rendered } = renderAthletes({ session: async () => ok(null) });
    await rendered;

    // Assert
    await waitFor(() => expect(replace).toHaveBeenCalledWith("/login"));
    expect(execute).not.toHaveBeenCalled();
  });

  it("shows the full name, the email and the start date of each athlete", async () => {
    // Arrange & Act
    renderAthletes();

    // Assert
    await screen.findByText("luis@example.com");
    for (const header of ["Nombre completo", "Email", "Atleta desde"]) {
      expect(screen.getByRole("columnheader", { name: header })).toBeInTheDocument();
    }
    const cells = within(bodyRows()[0]!).getAllByRole("cell").slice(0, 3);
    expect(cells.map((cell) => cell.textContent)).toEqual([
      "Luis Pérez",
      "luis@example.com",
      "03/10/2026",
    ]);
  });

  it("shows a dash for an athlete without name nor surname", async () => {
    // Arrange & Act
    renderAthletes({ getAthletes: async () => ok([nameless]) });

    // Assert
    await screen.findByText("nameless@example.com");
    expect(within(bodyRows()[0]!).getAllByRole("cell")[0]).toHaveTextContent("—");
  });

  it.each([
    ["the full name", "luis perez", ["luis@example.com"]],
    ["the email", "marta@", ["marta@example.com"]],
  ])("filters by %s", async (_field, text, emails) => {
    // Arrange
    renderAthletes();
    await screen.findByText("luis@example.com");
    const user = userEvent.setup();

    // Act
    await user.type(screen.getByRole("searchbox", { name: "Buscar por nombre o email" }), text);

    // Assert
    expect(emailsShown()).toEqual(emails);
  });

  it("shows every athlete again when the search is cleared", async () => {
    // Arrange
    renderAthletes();
    await screen.findByText("luis@example.com");
    const user = userEvent.setup();
    const search = screen.getByRole("searchbox", { name: "Buscar por nombre o email" });
    await user.type(search, "marta");

    // Act
    await user.clear(search);

    // Assert
    expect(emailsShown()).toHaveLength(3);
  });

  it("shows the empty message and no table when the trainer has no athletes", async () => {
    // Arrange & Act
    renderAthletes({ getAthletes: async () => ok([]) });

    // Assert
    expect(await screen.findByText("Todavía no tienes atletas.")).toBeInTheDocument();
    expect(screen.queryByRole("table")).not.toBeInTheDocument();
  });

  it("shows the no-match message when the search matches nobody", async () => {
    // Arrange
    renderAthletes();
    await screen.findByText("luis@example.com");
    const user = userEvent.setup();

    // Act
    await user.type(screen.getByRole("searchbox", { name: "Buscar por nombre o email" }), "zzz");

    // Assert
    expect(screen.getByText("Ningún atleta coincide con la búsqueda.")).toBeInTheDocument();
    expect(screen.queryByRole("table")).not.toBeInTheDocument();
  });

  it("shows the skeleton and no table while the athletes load", async () => {
    // Arrange & Act
    renderAthletes({ getAthletes: () => new Promise(() => {}) });

    // Assert
    expect(await screen.findByLabelText("Cargando")).toBeInTheDocument();
    expect(screen.queryByRole("table")).not.toBeInTheDocument();
  });

  it("shows the error and reads the athletes again when Reintentar is pressed", async () => {
    // Arrange
    const getAthletes = vi
      .fn<() => Promise<Result<TrainerAthlete[], DomainError>>>()
      .mockResolvedValueOnce(fail(new DomainError("NETWORK", "")))
      .mockResolvedValueOnce(ok([luis]));
    const { execute } = renderAthletes({ getAthletes });
    const user = userEvent.setup();
    await screen.findByText("No hay conexión con el servidor.");

    // Act
    await user.click(screen.getByRole("button", { name: "Reintentar" }));

    // Assert
    expect(await screen.findByText("luis@example.com")).toBeInTheDocument();
    expect(execute).toHaveBeenCalledTimes(2);
  });

  it("shows the forbidden error to an admin who opens the URL directly", async () => {
    // Arrange & Act
    renderAthletes({
      profile: adminProfile,
      getAthletes: async () => fail(new DomainError("FORBIDDEN", "Trainers only")),
    });

    // Assert
    expect(
      await screen.findByText("No tienes permiso para realizar esta acción."),
    ).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Reintentar" })).toBeInTheDocument();
  });

  it("offers Finalizar relación on each athlete's row", async () => {
    // Arrange
    renderAthletes();
    await screen.findByText("luis@example.com");
    const user = userEvent.setup();

    // Act
    await user.click(screen.getByRole("button", { name: "Acciones de Luis Pérez" }));

    // Assert
    expect(await screen.findByRole("menuitem", { name: "Finalizar relación" })).toBeInTheDocument();
    expect(screen.getAllByRole("button", { name: /^Acciones de /, hidden: true })).toHaveLength(3);
  });

  it("ends the relationship for the signed-in trainer, shows the message and reloads", async () => {
    // Arrange
    const { execute, endExecute } = renderAthletes();
    await screen.findByText("luis@example.com");
    const user = userEvent.setup();
    await chooseEnd(user, "Luis Pérez");
    await screen.findByRole("dialog", { name: "¿Finalizar la relación con Luis Pérez?" });

    // Act
    await user.click(screen.getByRole("button", { name: "Finalizar" }));

    // Assert
    expect(await screen.findByText("Relación finalizada.")).toBeInTheDocument();
    expect(endExecute).toHaveBeenCalledTimes(1);
    expect(endExecute).toHaveBeenCalledWith({
      trainerId: completeProfile.id,
      athleteId: luis.athleteId,
    });
    await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
    expect(execute).toHaveBeenCalledTimes(2);
  });

  it("speaks to the trainer in the confirmation", async () => {
    // Arrange
    renderAthletes();
    await screen.findByText("luis@example.com");
    const user = userEvent.setup();

    // Act
    await chooseEnd(user, "Luis Pérez");

    // Assert
    expect(
      await screen.findByText("Dejarás de entrenar a este atleta y ya no lo verás en tu lista."),
    ).toBeInTheDocument();
    expect(
      screen.queryByText("El atleta dejará de estar asignado a este entrenador."),
    ).not.toBeInTheDocument();
  });

  it("keeps the dialog open when the relationship is no longer active", async () => {
    // Arrange
    const { execute } = renderAthletes({
      end: async () => fail(new DomainError("NOT_FOUND", "No active relationship")),
    });
    await screen.findByText("luis@example.com");
    const user = userEvent.setup();
    await chooseEnd(user, "Luis Pérez");
    await screen.findByRole("dialog");

    // Act
    await user.click(screen.getByRole("button", { name: "Finalizar" }));

    // Assert
    expect(
      await screen.findByText("Esta relación ya no está activa. Recarga la lista."),
    ).toBeInTheDocument();
    expect(screen.getByRole("dialog")).toBeInTheDocument();
    expect(screen.queryByText("Relación finalizada.")).not.toBeInTheDocument();
    expect(execute).toHaveBeenCalledTimes(1);
  });

  it("reads the athletes again when the dialog is closed after a stale failure", async () => {
    // Arrange
    const { execute } = renderAthletes({
      end: async () => fail(new DomainError("CONFLICT", "Already ended")),
    });
    await screen.findByText("luis@example.com");
    const user = userEvent.setup();
    await chooseEnd(user, "Luis Pérez");
    await user.click(await screen.findByRole("button", { name: "Finalizar" }));
    await screen.findByRole("alert");

    // Act
    await user.click(screen.getByRole("button", { name: "Cancelar" }));

    // Assert
    await waitFor(() => expect(execute).toHaveBeenCalledTimes(2));
  });

  it("names the end relationship flow in English", async () => {
    // Arrange
    renderAthletes();
    await screen.findByText("luis@example.com");
    await act(() => i18n.changeLanguage("en"));
    const user = userEvent.setup();

    // Act
    await user.click(screen.getByRole("button", { name: "Actions for Luis Pérez" }));
    await user.click(await screen.findByRole("menuitem", { name: "End relationship" }));

    // Assert
    expect(
      await screen.findByRole("dialog", { name: "End the relationship with Luis Pérez?" }),
    ).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "End" })).toBeInTheDocument();
  });

  it("follows the active language", async () => {
    // Arrange
    renderAthletes();
    await screen.findByText("luis@example.com");

    // Act
    await act(() => i18n.changeLanguage("en"));

    // Assert
    expect(screen.getByRole("heading", { level: 1, name: "All athletes" })).toBeInTheDocument();
    expect(screen.getByRole("searchbox", { name: "Search by name or email" })).toBeInTheDocument();
    for (const header of ["Full name", "Email", "Athlete since"]) {
      expect(screen.getByRole("columnheader", { name: header })).toBeInTheDocument();
    }
  });
});
