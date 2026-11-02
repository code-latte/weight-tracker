import { act, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { DomainError, fail, ok } from "@weight-tracker/core/domain";
import type {
  Result,
  Session,
  TrainerAthleteRelationship,
  UserProfile,
  UserSummary,
} from "@weight-tracker/core/domain";
import i18n from "@/infrastructure/i18n";
import TrainerAssignmentsView from "@/presentation/trainer-assignments/views/trainer-assignments-view";
import renderWithProviders, { completeProfile } from "../../../support/render-with-providers";

const replace = vi.fn();
const push = vi.fn();

vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace, push }),
  usePathname: () => "/trainer-assignments",
}));

const adminProfile: UserProfile = { ...completeProfile, email: "admin@example.com", role: "ADMIN" };

const user = (overrides: Partial<UserSummary>): UserSummary => ({
  id: "1b2c3d4e-5f6a-4b7c-8d9e-0f1a2b3c4d5e",
  email: "someone@example.com",
  name: "",
  surname: "",
  role: "USER",
  isActive: true,
  deactivatedAtUtc: null,
  trainer: null,
  ...overrides,
});

const ana = user({
  id: "trainer-1",
  email: "ana@example.com",
  name: "Ana",
  surname: "García",
  role: "TRAINER",
});
const retired = user({
  id: "trainer-2",
  email: "retired@example.com",
  name: "Pablo",
  surname: "Ruiz",
  role: "TRAINER",
  isActive: false,
  deactivatedAtUtc: "2026-10-20T09:30:00Z",
});
const admin = user({ id: "admin-1", email: "admin@example.com", name: "Eva", role: "ADMIN" });
const anaAsTrainer = {
  id: ana.id,
  email: ana.email,
  name: ana.name,
  surname: ana.surname,
  role: "TRAINER",
};
const luis = user({
  id: "athlete-1",
  email: "luis@example.com",
  name: "Luis",
  surname: "Pérez",
  trainer: anaAsTrainer,
});
const marta = user({
  id: "athlete-2",
  email: "marta@example.com",
  name: "Marta",
  surname: "Anaya",
});
const nameless = user({ id: "athlete-3", email: "nameless@example.com" });

const relationship: TrainerAthleteRelationship = {
  id: "9d5a6c4b-3b9f-4e97-8a0b-4b0d1a5e6f70",
  trainerId: ana.id,
  trainerName: "Ana",
  trainerSurname: "García",
  athleteId: luis.id,
  athleteName: "Luis",
  athleteSurname: "Pérez",
  status: "ACTIVE",
  startedAtUtc: "2026-10-01T09:00:00Z",
  finishedAtUtc: null,
};

type UsersAnswer = () => Promise<Result<UserSummary[], DomainError>>;
type RelationshipAnswer = () => Promise<Result<TrainerAthleteRelationship, DomainError>>;

function renderAssignments({
  profile = adminProfile,
  getUsers = async () => ok([admin, luis, ana, marta, retired, nameless]),
  session = async () => ok({ accessToken: "a", refreshToken: "r" }),
  link = async () => ok(relationship),
  end = async () => ok({ ...relationship, status: "ENDED" }),
}: {
  profile?: UserProfile;
  getUsers?: UsersAnswer;
  session?: () => Promise<Result<Session | null, DomainError>>;
  link?: RelationshipAnswer;
  end?: RelationshipAnswer;
} = {}) {
  const execute = vi.fn(getUsers);
  const linkExecute = vi.fn(link);
  const endExecute = vi.fn(end);

  const rendered = renderWithProviders(<TrainerAssignmentsView />, {
    getSessionUseCase: () => ({ execute: session }),
    getMyProfileUseCase: () => ({ execute: async () => ok(profile) }),
    getUsersUseCase: () => ({ execute }),
    linkAthleteToTrainerUseCase: () => ({ execute: linkExecute }),
    endTrainerAthleteUseCase: () => ({ execute: endExecute }),
  });
  return { execute, linkExecute, endExecute, rendered };
}

function bodyRows() {
  const [, ...rows] = screen.getAllByRole("row");
  return rows;
}

const emailsShown = () => bodyRows().map((row) => within(row).getAllByRole("cell")[1]!.textContent);

const searchField = () =>
  screen.getByRole("searchbox", { name: "Buscar por atleta, email o entrenador" });

/** Opens the ⋮ menu of the athlete's row. */
async function openActions(actor: ReturnType<typeof userEvent.setup>, name: string) {
  await actor.click(await screen.findByRole("button", { name: `Acciones de ${name}` }));
}

describe("TrainerAssignmentsView", () => {
  beforeEach(() => {
    replace.mockClear();
    push.mockClear();
  });

  it("shows the title and only the athletes, in the API's order", async () => {
    // Arrange & Act
    const { execute } = renderAssignments();

    // Assert
    expect(
      await screen.findByRole("heading", { level: 1, name: "Asignación de entrenadores" }),
    ).toBeInTheDocument();
    await screen.findByText("luis@example.com");
    expect(emailsShown()).toEqual([
      "luis@example.com",
      "marta@example.com",
      "nameless@example.com",
    ]);
    expect(screen.queryByText("admin@example.com")).not.toBeInTheDocument();
    expect(screen.queryByText("ana@example.com")).not.toBeInTheDocument();
    expect(execute).toHaveBeenCalledTimes(1);
  });

  it("sends a visitor without a session to login and reads no users", async () => {
    // Arrange & Act
    const { execute, rendered } = renderAssignments({ session: async () => ok(null) });
    await rendered;

    // Assert
    await waitFor(() => expect(replace).toHaveBeenCalledWith("/login"));
    expect(execute).not.toHaveBeenCalled();
  });

  it("shows each athlete's full name, email and trainer, or Sin entrenador", async () => {
    // Arrange & Act
    renderAssignments();

    // Assert
    await screen.findByText("luis@example.com");
    for (const header of ["Nombre completo", "Email", "Entrenador"]) {
      expect(screen.getByRole("columnheader", { name: header })).toBeInTheDocument();
    }
    const texts = bodyRows().map((row) =>
      within(row)
        .getAllByRole("cell")
        .slice(0, 3)
        .map((cell) => cell.textContent),
    );
    expect(texts).toEqual([
      ["Luis Pérez", "luis@example.com", "Ana García"],
      ["Marta Anaya", "marta@example.com", "Sin entrenador"],
      ["—", "nameless@example.com", "Sin entrenador"],
    ]);
  });

  it("offers Finalizar relación and not Asignar entrenador on a row with a trainer", async () => {
    // Arrange
    renderAssignments();
    await screen.findByText("luis@example.com");
    const actor = userEvent.setup();

    // Act
    await openActions(actor, "Luis Pérez");

    // Assert
    expect(await screen.findByRole("menuitem", { name: "Finalizar relación" })).toBeInTheDocument();
    expect(screen.queryByRole("menuitem", { name: "Asignar entrenador" })).not.toBeInTheDocument();
  });

  it("offers Asignar entrenador and not Finalizar relación on a row without a trainer", async () => {
    // Arrange
    renderAssignments();
    await screen.findByText("marta@example.com");
    const actor = userEvent.setup();

    // Act
    await openActions(actor, "Marta Anaya");

    // Assert
    expect(await screen.findByRole("menuitem", { name: "Asignar entrenador" })).toBeInTheDocument();
    expect(screen.queryByRole("menuitem", { name: "Finalizar relación" })).not.toBeInTheDocument();
  });

  it.each([
    ["the athlete's full name", "luis perez", ["luis@example.com"]],
    ["the athlete's email", "marta@", ["marta@example.com"]],
    ["the trainer's full name", "garcia", ["luis@example.com"]],
  ])("filters by %s", async (_field, text, emails) => {
    // Arrange
    renderAssignments();
    await screen.findByText("luis@example.com");
    const actor = userEvent.setup();

    // Act
    await actor.type(searchField(), text);

    // Assert
    expect(emailsShown()).toEqual(emails);
  });

  it.each([
    ["Con entrenador", ["luis@example.com"]],
    ["Sin entrenador", ["marta@example.com", "nameless@example.com"]],
  ])("keeps only the athletes of the %s pill", async (pill, emails) => {
    // Arrange
    renderAssignments();
    await screen.findByText("luis@example.com");
    const actor = userEvent.setup();

    // Act
    await actor.click(screen.getByRole("button", { name: pill }));

    // Assert
    expect(emailsShown()).toEqual(emails);
    expect(screen.getByRole("button", { name: pill })).toHaveAttribute("aria-pressed", "true");
  });

  it("combines the pill and the search, and clears the pill when pressed again", async () => {
    // Arrange
    renderAssignments();
    await screen.findByText("luis@example.com");
    const actor = userEvent.setup();
    await actor.click(screen.getByRole("button", { name: "Sin entrenador" }));
    await actor.type(searchField(), "marta");
    const combined = emailsShown();

    // Act
    await actor.click(screen.getByRole("button", { name: "Sin entrenador" }));

    // Assert
    expect(combined).toEqual(["marta@example.com"]);
    expect(emailsShown()).toEqual(["marta@example.com"]);
    expect(screen.getByRole("button", { name: "Sin entrenador" })).toHaveAttribute(
      "aria-pressed",
      "false",
    );
  });

  it("offers only the active trainers when assigning", async () => {
    // Arrange
    renderAssignments();
    await screen.findByText("marta@example.com");
    const actor = userEvent.setup();
    await openActions(actor, "Marta Anaya");
    await actor.click(await screen.findByRole("menuitem", { name: "Asignar entrenador" }));
    await screen.findByRole("dialog", { name: "Asignar entrenador a Marta Anaya" });

    // Act
    await actor.click(screen.getByRole("combobox", { name: "Entrenador" }));

    // Assert
    const options = await screen.findAllByRole("option");
    expect(options.map((option) => option.textContent)).toEqual(["Ana García · ana@example.com"]);
  });

  it("assigns a trainer, shows the message and reads the users again", async () => {
    // Arrange
    const { execute, linkExecute } = renderAssignments();
    await screen.findByText("marta@example.com");
    const actor = userEvent.setup();
    await openActions(actor, "Marta Anaya");
    await actor.click(await screen.findByRole("menuitem", { name: "Asignar entrenador" }));
    await actor.click(await screen.findByRole("combobox", { name: "Entrenador" }));
    await actor.click(await screen.findByRole("option", { name: "Ana García · ana@example.com" }));

    // Act
    await actor.click(screen.getByRole("button", { name: "Asignar" }));

    // Assert
    expect(await screen.findByText("Entrenador asignado.")).toBeInTheDocument();
    expect(linkExecute).toHaveBeenCalledWith({ trainerId: ana.id, athleteId: marta.id });
    await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
    expect(execute).toHaveBeenCalledTimes(2);
  });

  it("ends the relationship with the row's trainer, shows the message and reads the users again", async () => {
    // Arrange
    const { execute, endExecute } = renderAssignments();
    await screen.findByText("luis@example.com");
    const actor = userEvent.setup();
    await openActions(actor, "Luis Pérez");
    await actor.click(await screen.findByRole("menuitem", { name: "Finalizar relación" }));
    await screen.findByRole("dialog", { name: "¿Finalizar la relación con Luis Pérez?" });

    // Act
    await actor.click(screen.getByRole("button", { name: "Finalizar" }));

    // Assert
    expect(await screen.findByText("Relación finalizada.")).toBeInTheDocument();
    expect(endExecute).toHaveBeenCalledTimes(1);
    expect(endExecute).toHaveBeenCalledWith({ trainerId: ana.id, athleteId: luis.id });
    expect(execute).toHaveBeenCalledTimes(2);
  });

  it("reads the users again when the assign dialog is closed after a conflict", async () => {
    // Arrange
    const { execute } = renderAssignments({
      link: async () => fail(new DomainError("CONFLICT", "Already has a trainer")),
    });
    await screen.findByText("marta@example.com");
    const actor = userEvent.setup();
    await openActions(actor, "Marta Anaya");
    await actor.click(await screen.findByRole("menuitem", { name: "Asignar entrenador" }));
    await actor.click(await screen.findByRole("combobox", { name: "Entrenador" }));
    await actor.click(await screen.findByRole("option", { name: "Ana García · ana@example.com" }));
    await actor.click(screen.getByRole("button", { name: "Asignar" }));
    await screen.findByRole("alert");

    // Act
    await actor.click(screen.getByRole("button", { name: "Cancelar" }));

    // Assert
    await waitFor(() => expect(execute).toHaveBeenCalledTimes(2));
    expect(screen.queryByText("Entrenador asignado.")).not.toBeInTheDocument();
  });

  it("shows the empty message when the API returns no athletes", async () => {
    // Arrange & Act
    renderAssignments({ getUsers: async () => ok([admin, ana]) });

    // Assert
    expect(await screen.findByText("Todavía no hay atletas.")).toBeInTheDocument();
    expect(screen.queryByRole("table")).not.toBeInTheDocument();
  });

  it("shows the no-match message when the search matches nobody", async () => {
    // Arrange
    renderAssignments();
    await screen.findByText("luis@example.com");
    const actor = userEvent.setup();

    // Act
    await actor.type(searchField(), "zzz");

    // Assert
    expect(screen.getByText("Ningún atleta coincide con la búsqueda.")).toBeInTheDocument();
    expect(screen.queryByRole("table")).not.toBeInTheDocument();
  });

  it("shows the skeleton and no table while the users load", async () => {
    // Arrange & Act
    renderAssignments({ getUsers: () => new Promise(() => {}) });

    // Assert
    expect(await screen.findByLabelText("Cargando")).toBeInTheDocument();
    expect(screen.queryByRole("table")).not.toBeInTheDocument();
  });

  it("shows the error and reads the users again when Reintentar is pressed", async () => {
    // Arrange
    const getUsers = vi
      .fn<UsersAnswer>()
      .mockResolvedValueOnce(fail(new DomainError("NETWORK", "")))
      .mockResolvedValueOnce(ok([luis]));
    const { execute } = renderAssignments({ getUsers });
    const actor = userEvent.setup();
    await screen.findByText("No hay conexión con el servidor.");

    // Act
    await actor.click(screen.getByRole("button", { name: "Reintentar" }));

    // Assert
    expect(await screen.findByText("luis@example.com")).toBeInTheDocument();
    expect(execute).toHaveBeenCalledTimes(2);
  });

  it("shows the forbidden error and no action to a trainer who opens the URL directly", async () => {
    // Arrange & Act
    renderAssignments({
      profile: completeProfile,
      getUsers: async () => fail(new DomainError("FORBIDDEN", "Admins only")),
    });

    // Assert
    expect(
      await screen.findByText("No tienes permiso para realizar esta acción."),
    ).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Reintentar" })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /^Acciones de / })).not.toBeInTheDocument();
  });

  it("follows the active language", async () => {
    // Arrange
    renderAssignments();
    await screen.findByText("luis@example.com");

    // Act
    await act(() => i18n.changeLanguage("en"));

    // Assert
    expect(
      screen.getByRole("heading", { level: 1, name: "Trainer assignment" }),
    ).toBeInTheDocument();
    for (const header of ["Full name", "Email", "Trainer"]) {
      expect(screen.getByRole("columnheader", { name: header })).toBeInTheDocument();
    }
    expect(screen.getAllByText("No trainer")).toHaveLength(2);
  });
});
