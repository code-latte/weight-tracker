import { act, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { DomainError, fail, ok } from "@weight-tracker/core/domain";
import type {
  CreateWorkoutTemplateRequest,
  Result,
  UserProfile,
  UserSummary,
  WorkoutTemplate,
} from "@weight-tracker/core/domain";
import i18n from "@/infrastructure/i18n";
import WorkoutTemplatesView from "@/presentation/workouts/views/workout-templates-view";
import renderWithProviders, { completeProfile } from "../../../support/render-with-providers";

const replace = vi.fn();
const push = vi.fn();

vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace, push }),
  usePathname: () => "/workout-templates",
}));

const adminProfile: UserProfile = { ...completeProfile, email: "admin@example.com", role: "ADMIN" };

const trainerId = "6a2d3f1e-0e6c-4b64-9d8f-1e7a8d2b3c4d";

const entry = {
  id: "0a1b2c3d-4e5f-4a6b-8c7d-9e0f1a2b3c4d",
  exerciseId: "1f2e3d4c-5b6a-4978-8695-a4b3c2d1e0f9",
  effort: "HIGH",
  perSide: false,
  restMinutes: null,
  sets: [],
  superSeriesId: null,
  setRecords: [],
} satisfies WorkoutTemplate["exercises"][number];

const legs: WorkoutTemplate = {
  id: "8d7c6b5a-4f3e-4d2c-9b1a-0f9e8d7c6b5a",
  name: "Pierna A",
  description: "Fuerza",
  trainerId,
  exercises: [entry, { ...entry, id: "e-2" }, { ...entry, id: "e-3" }],
};

const upper: WorkoutTemplate = {
  id: "9e8d7c6b-5a4f-4e3d-8c2b-1a0f9e8d7c6b",
  name: "Torso",
  description: null,
  trainerId: "unknown-trainer",
  exercises: [],
};

function userOf(
  id: string,
  role: string,
  name: string,
  surname: string,
  email: string,
): UserSummary {
  return { id, email, name, surname, role, isActive: true, deactivatedAtUtc: null, trainer: null };
}

const users = [
  userOf("u-1", "USER", "Ana", "García", "ana@example.com"),
  userOf(trainerId, "TRAINER", "Luis", "Pérez", "luis@example.com"),
];

function renderTemplates({
  profile = completeProfile,
  getTemplates = async () => ok([legs, upper]),
  createTemplate = async () => ok({ ...legs, id: "wt-9" }),
}: {
  profile?: UserProfile;
  getTemplates?: () => Promise<Result<WorkoutTemplate[], DomainError>>;
  createTemplate?: (
    request: CreateWorkoutTemplateRequest,
  ) => Promise<Result<WorkoutTemplate, DomainError>>;
} = {}) {
  const execute = vi.fn(getTemplates);
  const create = vi.fn(createTemplate);
  const getUsers = vi.fn(async () => ok(users));

  return {
    execute,
    create,
    getUsers,
    rendered: renderWithProviders(<WorkoutTemplatesView />, {
      getSessionUseCase: () => ({
        execute: async () => ok({ accessToken: "a", refreshToken: "r" }),
      }),
      getMyProfileUseCase: () => ({ execute: async () => ok(profile) }),
      getWorkoutTemplatesUseCase: () => ({ execute }),
      createWorkoutTemplateUseCase: () => ({ execute: create }),
      getUsersUseCase: () => ({ execute: getUsers }),
    }),
  };
}

function bodyRows() {
  const [, ...rows] = screen.getAllByRole("row");
  return rows;
}

const cellsOf = (row: HTMLElement) =>
  within(row)
    .getAllByRole("cell")
    .map((cell) => cell.textContent);

async function openDialog(user: ReturnType<typeof userEvent.setup>) {
  await user.click(await screen.findByRole("button", { name: "Nueva plantilla" }));
  return screen.findByRole("dialog", { name: "Nueva plantilla" });
}

describe("WorkoutTemplatesView", () => {
  beforeEach(() => {
    replace.mockClear();
    push.mockClear();
  });

  it("shows the title and one row per template in the API's order, read once without a trainer", async () => {
    // Arrange
    const { execute, rendered } = renderTemplates();

    // Act
    await rendered;

    // Assert
    expect(
      await screen.findByRole("heading", { name: "Plantillas de entrenamiento" }),
    ).toBeInTheDocument();
    await screen.findByText("Pierna A");
    expect(bodyRows().map((row) => within(row).getAllByRole("cell")[0]!.textContent)).toEqual([
      "Pierna A",
      "Torso",
    ]);
    expect(execute).toHaveBeenCalledTimes(1);
    expect(execute).toHaveBeenCalledWith({});
  });

  it("shows the name, the description and the exercise count, and an empty description", async () => {
    // Arrange & Act
    await renderTemplates().rendered;

    // Assert
    await screen.findByText("Pierna A");
    const [legsRow, upperRow] = bodyRows();
    expect(cellsOf(legsRow!)).toEqual(["Pierna A", "Fuerza", "3", ""]);
    expect(cellsOf(upperRow!)).toEqual(["Torso", "", "0", ""]);
  });

  it("shows the trainer column to an admin, with the trainer's name and a dash when unknown", async () => {
    // Arrange & Act
    await renderTemplates({ profile: adminProfile }).rendered;

    // Assert
    expect(await screen.findByRole("columnheader", { name: "Entrenador" })).toBeInTheDocument();
    const [legsRow, upperRow] = bodyRows();
    await waitFor(() =>
      expect(within(legsRow!).getAllByRole("cell")[2]).toHaveTextContent("Luis Pérez"),
    );
    expect(within(upperRow!).getAllByRole("cell")[2]).toHaveTextContent("—");
    expect(screen.queryByText(trainerId)).not.toBeInTheDocument();
  });

  it("hides the trainer column from a trainer and reads no users", async () => {
    // Arrange
    const { getUsers, rendered } = renderTemplates();

    // Act
    await rendered;

    // Assert
    await screen.findByText("Pierna A");
    expect(screen.queryByRole("columnheader", { name: "Entrenador" })).not.toBeInTheDocument();
    expect(within(bodyRows()[0]!).getAllByRole("cell")).toHaveLength(4);
    expect(getUsers).not.toHaveBeenCalled();
  });

  it("shows the empty message and no table when there are no templates", async () => {
    // Arrange & Act
    await renderTemplates({ getTemplates: async () => ok([]) }).rendered;

    // Assert
    expect(await screen.findByText("Todavía no hay plantillas.")).toBeInTheDocument();
    expect(screen.queryByRole("table")).not.toBeInTheDocument();
  });

  it("shows the skeleton and no table while the templates load", async () => {
    // Arrange & Act
    await renderTemplates({ getTemplates: () => new Promise(() => {}) }).rendered;

    // Assert
    expect(await screen.findByLabelText("Cargando")).toBeInTheDocument();
    expect(screen.queryByRole("table")).not.toBeInTheDocument();
  });

  it("shows the error and reads the templates again when Reintentar is pressed", async () => {
    // Arrange
    const getTemplates = vi
      .fn<() => Promise<Result<WorkoutTemplate[], DomainError>>>()
      .mockResolvedValueOnce(fail(new DomainError("NETWORK", "")))
      .mockResolvedValueOnce(ok([legs]));
    const { execute, rendered } = renderTemplates({ getTemplates });
    await rendered;
    const user = userEvent.setup();
    await screen.findByText("No hay conexión con el servidor.");

    // Act
    await user.click(screen.getByRole("button", { name: "Reintentar" }));

    // Assert
    expect(await screen.findByText("Pierna A")).toBeInTheDocument();
    expect(execute).toHaveBeenCalledTimes(2);
  });

  it("links each row's edit action to that template's screen", async () => {
    // Arrange & Act
    await renderTemplates().rendered;

    // Assert
    expect(await screen.findByRole("link", { name: "Editar Pierna A" })).toHaveAttribute(
      "href",
      `/workout-template?id=${legs.id}`,
    );
  });

  it("opens the create dialog with only a name and a description for a trainer, sending nothing", async () => {
    // Arrange
    const { create, rendered } = renderTemplates();
    await rendered;
    const user = userEvent.setup();

    // Act
    const dialog = await openDialog(user);

    // Assert
    expect(within(dialog).getByRole("textbox", { name: "Nombre" })).toHaveValue("");
    expect(within(dialog).getByRole("textbox", { name: "Descripción" })).toHaveValue("");
    expect(within(dialog).queryByRole("combobox", { name: "Entrenador" })).not.toBeInTheDocument();
    expect(within(dialog).queryByRole("combobox", { name: "Atleta" })).not.toBeInTheDocument();
    expect(within(dialog).queryByRole("group", { name: "Semana" })).not.toBeInTheDocument();
    expect(create).not.toHaveBeenCalled();
  });

  it("reports a blank name and a long description and sends nothing", async () => {
    // Arrange
    const { create, rendered } = renderTemplates();
    await rendered;
    const user = userEvent.setup();
    const dialog = await openDialog(user);
    await user.click(within(dialog).getByRole("textbox", { name: "Descripción" }));
    await user.paste("a".repeat(1025));

    // Act
    await user.click(within(dialog).getByRole("button", { name: "Crear" }));

    // Assert
    expect(within(dialog).getByText("Este campo es obligatorio.")).toBeInTheDocument();
    expect(within(dialog).getByText("No puede superar los 1024 caracteres.")).toBeInTheDocument();
    expect(create).not.toHaveBeenCalled();
  });

  it("creates a trainer's template once, trimmed and without a trainer, and opens it", async () => {
    // Arrange
    const { create, rendered } = renderTemplates();
    await rendered;
    const user = userEvent.setup();
    const dialog = await openDialog(user);
    await user.click(within(dialog).getByRole("textbox", { name: "Nombre" }));
    await user.paste("  Pierna A  ");

    // Act
    await user.click(within(dialog).getByRole("button", { name: "Crear" }));

    // Assert
    await waitFor(() => expect(push).toHaveBeenCalledWith("/workout-template?id=wt-9"));
    expect(create).toHaveBeenCalledTimes(1);
    expect(create).toHaveBeenCalledWith({ name: "Pierna A" });
  });

  it("asks an admin for the trainer and sends the chosen one", async () => {
    // Arrange
    const { create, rendered } = renderTemplates({ profile: adminProfile });
    await rendered;
    const user = userEvent.setup();
    const dialog = await openDialog(user);
    await user.click(within(dialog).getByRole("textbox", { name: "Nombre" }));
    await user.paste("Pierna A");
    await user.click(within(dialog).getByRole("button", { name: "Crear" }));
    expect(within(dialog).getByText("Selecciona un entrenador.")).toBeInTheDocument();
    const trainerField = within(dialog).getByRole("combobox", { name: "Entrenador" });
    await waitFor(() => expect(trainerField).toBeEnabled());
    await user.click(trainerField);
    await user.click(await screen.findByRole("option", { name: "Luis Pérez · luis@example.com" }));

    // Act
    await user.click(within(dialog).getByRole("button", { name: "Crear" }));

    // Assert
    await waitFor(() => expect(create).toHaveBeenCalledTimes(1));
    expect(create).toHaveBeenCalledWith({ name: "Pierna A", trainerId });
  });

  it("keeps the dialog and the typed values when the creation is refused", async () => {
    // Arrange
    const { rendered } = renderTemplates({
      createTemplate: async () => fail(new DomainError("FORBIDDEN", "")),
    });
    await rendered;
    const user = userEvent.setup();
    const dialog = await openDialog(user);
    await user.click(within(dialog).getByRole("textbox", { name: "Nombre" }));
    await user.paste("Pierna A");

    // Act
    await user.click(within(dialog).getByRole("button", { name: "Crear" }));

    // Assert
    expect(await within(dialog).findByRole("alert")).toBeInTheDocument();
    expect(within(dialog).getByRole("textbox", { name: "Nombre" })).toHaveValue("Pierna A");
    expect(push).not.toHaveBeenCalled();
  });

  it("closes on Cancelar without creating and opens empty again", async () => {
    // Arrange
    const { create, rendered } = renderTemplates();
    await rendered;
    const user = userEvent.setup();
    const dialog = await openDialog(user);
    await user.click(within(dialog).getByRole("textbox", { name: "Nombre" }));
    await user.paste("Pierna A");
    await user.click(within(dialog).getByRole("button", { name: "Cancelar" }));
    await waitFor(() =>
      expect(screen.queryByRole("dialog", { name: "Nueva plantilla" })).not.toBeInTheDocument(),
    );

    // Act
    const reopened = await openDialog(user);

    // Assert
    expect(within(reopened).getByRole("textbox", { name: "Nombre" })).toHaveValue("");
    expect(create).not.toHaveBeenCalled();
  });

  it("follows the active language", async () => {
    // Arrange
    await renderTemplates().rendered;
    await screen.findByText("Pierna A");

    // Act
    await act(() => i18n.changeLanguage("en"));

    // Assert
    expect(screen.getByRole("heading", { name: "Workout templates" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "New template" })).toBeInTheDocument();
  });
});
