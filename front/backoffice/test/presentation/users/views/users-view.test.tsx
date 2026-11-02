import { act, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { DomainError, fail, ok } from "@weight-tracker/core/domain";
import type {
  CreatedUserResponse,
  Result,
  UserProfile,
  UserSummary,
} from "@weight-tracker/core/domain";
import i18n from "@/infrastructure/i18n";
import UsersView from "@/presentation/users/views/users-view";
import renderWithProviders, { completeProfile } from "../../../support/render-with-providers";

const replace = vi.fn();
const push = vi.fn();

vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace, push }),
  usePathname: () => "/users",
}));

const adminProfile: UserProfile = { ...completeProfile, email: "admin@example.com", role: "ADMIN" };

const ana: UserSummary = {
  id: "5f1c2f0e-9d5b-4a53-8c7e-0d6f7c1a2b3c",
  email: "ana@example.com",
  name: "Ana",
  surname: "García",
  role: "TRAINER",
  isActive: true,
  deactivatedAtUtc: null,
  trainer: null,
};
const luis: UserSummary = {
  id: "6a2d3f1e-0e6c-4b64-9d8f-1e7a8d2b3c4d",
  email: "luis@example.com",
  name: "Luis",
  surname: "Pérez",
  role: "USER",
  isActive: true,
  deactivatedAtUtc: null,
  trainer: null,
};
const marta: UserSummary = {
  id: "7b3e4a2f-1f7d-4c75-8e9a-2f8b9e3c4d5e",
  email: "marta@example.com",
  name: "Marta",
  surname: "Anaya",
  role: "ADMIN",
  isActive: true,
  deactivatedAtUtc: null,
  trainer: null,
};
const nameless: UserSummary = {
  id: "8c4f5b3a-2a8e-4d86-9fab-3a9c0f4d5e6f",
  email: "nameless@example.com",
  name: "",
  surname: "",
  role: "USER",
  isActive: true,
  deactivatedAtUtc: null,
  trainer: null,
};

const carla: UserSummary = {
  id: "9d5a6c4b-3b9f-4e97-8a0b-4b0d1a5e6f70",
  email: "carla@example.com",
  name: "Carla",
  surname: "Ruiz",
  role: "TRAINER",
  isActive: false,
  deactivatedAtUtc: "2026-10-20T09:30:00Z",
  trainer: null,
};

const created: CreatedUserResponse = {
  id: luis.id,
  email: luis.email,
  name: luis.name,
  surname: luis.surname,
  role: luis.role,
  dateBirth: "1990-05-17",
};

function renderUsers({
  profile = adminProfile,
  getUsers = async () => ok([ana, luis, marta]),
  createUser = async () => ok(created),
  deactivateUser = async () => ok({ ...luis, isActive: false }),
  activateUser = async () => ok({ ...carla, isActive: true }),
}: {
  profile?: UserProfile;
  getUsers?: () => Promise<Result<UserSummary[], DomainError>>;
  createUser?: () => Promise<Result<CreatedUserResponse, DomainError>>;
  deactivateUser?: () => Promise<Result<UserSummary, DomainError>>;
  activateUser?: () => Promise<Result<UserSummary, DomainError>>;
} = {}) {
  const execute = vi.fn(getUsers);
  const create = vi.fn(createUser);
  const deactivate = vi.fn(deactivateUser);
  const activate = vi.fn(activateUser);

  renderWithProviders(<UsersView />, {
    getSessionUseCase: () => ({
      execute: async () => ok({ accessToken: "a", refreshToken: "r" }),
    }),
    getMyProfileUseCase: () => ({ execute: async () => ok(profile) }),
    getUsersUseCase: () => ({ execute }),
    createUserUseCase: () => ({ execute: create }),
    deactivateUserUseCase: () => ({ execute: deactivate }),
    activateUserUseCase: () => ({ execute: activate }),
  });
  return { execute, create, deactivate, activate };
}

function bodyRows() {
  const [, ...rows] = screen.getAllByRole("row");
  return rows;
}

const emailsShown = () => bodyRows().map((row) => within(row).getAllByRole("cell")[1]!.textContent);

const searchField = () => screen.getByRole("searchbox", { name: "Buscar por nombre o email" });

describe("UsersView", () => {
  beforeEach(() => {
    replace.mockClear();
    push.mockClear();
  });

  it("shows the title and one row per user in the API's order", async () => {
    // Arrange & Act
    renderUsers();

    // Assert
    expect(
      await screen.findByRole("heading", { level: 1, name: "Todos los usuarios" }),
    ).toBeInTheDocument();
    await screen.findByText("ana@example.com");
    expect(emailsShown()).toEqual(["ana@example.com", "luis@example.com", "marta@example.com"]);
  });

  it("shows the full name, the email and the profile of each user", async () => {
    // Arrange & Act
    renderUsers();

    // Assert
    await screen.findByText("ana@example.com");
    for (const header of ["Nombre completo", "Email", "Perfil"]) {
      expect(screen.getByRole("columnheader", { name: header })).toBeInTheDocument();
    }
    const cells = within(bodyRows()[0]!).getAllByRole("cell").slice(0, 3);
    expect(cells.map((cell) => cell.textContent)).toEqual([
      "Ana García",
      "ana@example.com",
      "Entrenador",
    ]);
  });

  it("shows a dash for a user without name nor surname", async () => {
    // Arrange & Act
    renderUsers({ getUsers: async () => ok([nameless]) });

    // Assert
    await screen.findByText("nameless@example.com");
    expect(within(bodyRows()[0]!).getAllByRole("cell")[0]).toHaveTextContent("—");
  });

  it.each([
    ["the email", "luis@", ["luis@example.com"]],
    ["the name", "marta", ["marta@example.com"]],
    ["the surname, ignoring case and accents", "GARCIA", ["ana@example.com"]],
  ])("filters by %s without a request", async (_case, text, emails) => {
    // Arrange
    const { execute } = renderUsers();
    const user = userEvent.setup();
    await screen.findByText("ana@example.com");

    // Act
    await user.type(searchField(), text);

    // Assert
    expect(emailsShown()).toEqual(emails);
    expect(execute).toHaveBeenCalledTimes(1);
  });

  it("shows every user again when the search is cleared", async () => {
    // Arrange
    renderUsers();
    const user = userEvent.setup();
    await screen.findByText("ana@example.com");
    await user.type(searchField(), "luis");

    // Act
    await user.clear(searchField());

    // Assert
    expect(emailsShown()).toHaveLength(3);
  });

  it("filters by the selected profile pill", async () => {
    // Arrange
    renderUsers();
    const user = userEvent.setup();
    await screen.findByText("ana@example.com");
    const pills = screen.getByRole("group", { name: "Perfil" });

    // Act
    await user.click(within(pills).getByRole("button", { name: "Entrenador" }));

    // Assert
    expect(emailsShown()).toEqual(["ana@example.com"]);
    expect(within(pills).getByRole("button", { name: "Entrenador" })).toHaveAttribute(
      "aria-pressed",
      "true",
    );
  });

  it("clears the profile filter when the selected pill is pressed again", async () => {
    // Arrange
    renderUsers();
    const user = userEvent.setup();
    await screen.findByText("ana@example.com");
    const pills = screen.getByRole("group", { name: "Perfil" });
    await user.click(within(pills).getByRole("button", { name: "Entrenador" }));

    // Act
    await user.click(within(pills).getByRole("button", { name: "Entrenador" }));

    // Assert
    expect(emailsShown()).toHaveLength(3);
    for (const pill of within(pills).getAllByRole("button")) {
      expect(pill).toHaveAttribute("aria-pressed", "false");
    }
  });

  it("combines the profile pill and the search", async () => {
    // Arrange
    renderUsers();
    const user = userEvent.setup();
    await screen.findByText("ana@example.com");
    const pills = screen.getByRole("group", { name: "Perfil" });
    await user.click(within(pills).getByRole("button", { name: "Administrador" }));

    // Act
    await user.type(searchField(), "ana");

    // Assert
    expect(emailsShown()).toEqual(["marta@example.com"]);
  });

  it("shows the empty message and no table when there are no users", async () => {
    // Arrange & Act
    renderUsers({ getUsers: async () => ok([]) });

    // Assert
    expect(await screen.findByText("Todavía no hay usuarios.")).toBeInTheDocument();
    expect(screen.queryByRole("table")).not.toBeInTheDocument();
  });

  it("shows the no-match message when the search matches nobody", async () => {
    // Arrange
    renderUsers();
    const user = userEvent.setup();
    await screen.findByText("ana@example.com");

    // Act
    await user.type(searchField(), "nadie");

    // Assert
    expect(screen.getByText("Ningún usuario coincide con la búsqueda.")).toBeInTheDocument();
    expect(screen.queryByRole("table")).not.toBeInTheDocument();
  });

  it("shows the skeleton and no table while the users load", async () => {
    // Arrange & Act
    renderUsers({ getUsers: () => new Promise(() => {}) });

    // Assert
    expect(await screen.findByLabelText("Cargando")).toBeInTheDocument();
    expect(screen.queryByRole("table")).not.toBeInTheDocument();
  });

  it("shows the error and reads the users again when Reintentar is pressed", async () => {
    // Arrange
    const getUsers = vi
      .fn<() => Promise<Result<UserSummary[], DomainError>>>()
      .mockResolvedValueOnce(fail(new DomainError("NETWORK", "")))
      .mockResolvedValueOnce(ok([ana]));
    const { execute } = renderUsers({ getUsers });
    const user = userEvent.setup();
    await screen.findByText("No hay conexión con el servidor.");

    // Act
    await user.click(screen.getByRole("button", { name: "Reintentar" }));

    // Assert
    expect(await screen.findByText("ana@example.com")).toBeInTheDocument();
    expect(execute).toHaveBeenCalledTimes(2);
  });

  it("shows the forbidden error to a trainer, whose listing the API only serves to admins", async () => {
    // Arrange & Act
    renderUsers({
      profile: completeProfile,
      getUsers: async () => fail(new DomainError("FORBIDDEN", "Admins only")),
    });

    // Assert
    expect(
      await screen.findByText("No tienes permiso para realizar esta acción."),
    ).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Reintentar" })).toBeInTheDocument();
  });

  it("opens the creation modal and sends nothing when an admin presses Añadir usuario", async () => {
    // Arrange
    const { execute, create } = renderUsers();
    const user = userEvent.setup();
    await screen.findByText("ana@example.com");

    // Act
    await user.click(screen.getByRole("button", { name: "Añadir usuario" }));

    // Assert
    expect(await screen.findByRole("dialog", { name: "Nuevo usuario" })).toBeInTheDocument();
    expect(create).not.toHaveBeenCalled();
    expect(execute).toHaveBeenCalledTimes(1);
  });

  it("hides the button and the modal from a trainer", async () => {
    // Arrange & Act
    renderUsers({ profile: completeProfile });

    // Assert
    await screen.findByText("ana@example.com");
    expect(screen.queryByRole("button", { name: "Añadir usuario" })).not.toBeInTheDocument();
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  });

  it("confirms the creation, closes the modal and reads the users again", async () => {
    // Arrange
    const getUsers = vi
      .fn<() => Promise<Result<UserSummary[], DomainError>>>()
      .mockResolvedValueOnce(ok([ana]))
      .mockResolvedValueOnce(ok([ana, luis]));
    const { execute } = renderUsers({ getUsers });
    const user = userEvent.setup();
    await screen.findByText("ana@example.com");
    await user.click(screen.getByRole("button", { name: "Añadir usuario" }));
    const dialog = await screen.findByRole("dialog", { name: "Nuevo usuario" });
    await user.type(within(dialog).getByRole("textbox", { name: "Email" }), "luis@example.com");
    await user.type(within(dialog).getByLabelText("Contraseña"), "Str0ng!Passw0rd");

    // Act
    await user.click(within(dialog).getByRole("button", { name: "Crear usuario" }));

    // Assert
    expect(await screen.findByText("Usuario creado.")).toBeInTheDocument();
    await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
    expect(await screen.findByText("luis@example.com")).toBeInTheDocument();
    expect(execute).toHaveBeenCalledTimes(2);
  });

  it("hides the inactive users on load, with the switch off", async () => {
    // Arrange & Act
    renderUsers({ getUsers: async () => ok([ana, carla, luis]) });

    // Assert
    await screen.findByText("ana@example.com");
    expect(emailsShown()).toEqual(["ana@example.com", "luis@example.com"]);
    expect(screen.getByRole("switch", { name: "Mostrar inactivos" })).not.toBeChecked();
  });

  it("shows the inactive users, muted and marked, when the switch is turned on", async () => {
    // Arrange
    renderUsers({ getUsers: async () => ok([ana, carla, luis]) });
    await screen.findByText("ana@example.com");
    const user = userEvent.setup();

    // Act
    await user.click(screen.getByRole("switch", { name: "Mostrar inactivos" }));

    // Assert
    expect(emailsShown()).toEqual(["ana@example.com", "carla@example.com", "luis@example.com"]);
    const carlaRow = bodyRows()[1]!;
    expect(within(carlaRow).getByText("Inactivo")).toBeInTheDocument();
    expect(within(bodyRows()[0]!).queryByText("Inactivo")).not.toBeInTheDocument();
    expect(within(bodyRows()[2]!).queryByText("Inactivo")).not.toBeInTheDocument();
  });

  it("gives the deactivation date in the inactive chip's tooltip", async () => {
    // Arrange
    renderUsers({ getUsers: async () => ok([carla]) });
    const user = userEvent.setup();
    await screen.findByText("Ningún usuario coincide con la búsqueda.");
    await user.click(screen.getByRole("switch", { name: "Mostrar inactivos" }));

    // Act
    await user.hover(screen.getByText("Inactivo"));

    // Assert
    expect(await screen.findByRole("tooltip")).toHaveTextContent("Desactivado el 20/10/2026");
  });

  it("hides the inactive users again when the switch is turned off", async () => {
    // Arrange
    renderUsers({ getUsers: async () => ok([ana, carla, luis]) });
    await screen.findByText("ana@example.com");
    const user = userEvent.setup();
    const toggle = screen.getByRole("switch", { name: "Mostrar inactivos" });
    await user.click(toggle);

    // Act
    await user.click(toggle);

    // Assert
    expect(emailsShown()).toEqual(["ana@example.com", "luis@example.com"]);
  });

  it("combines the switch with the profile pill", async () => {
    // Arrange
    renderUsers({ getUsers: async () => ok([ana, carla, luis]) });
    await screen.findByText("ana@example.com");
    const user = userEvent.setup();
    await user.click(screen.getByRole("switch", { name: "Mostrar inactivos" }));

    // Act
    await user.click(screen.getByRole("button", { name: "Entrenador" }));

    // Assert
    expect(emailsShown()).toEqual(["ana@example.com", "carla@example.com"]);
  });

  it("shows the no-match message when every user is inactive and the switch is off", async () => {
    // Arrange & Act
    renderUsers({ getUsers: async () => ok([carla]) });

    // Assert
    expect(await screen.findByText("Ningún usuario coincide con la búsqueda.")).toBeInTheDocument();
    expect(screen.queryByText("Todavía no hay usuarios.")).not.toBeInTheDocument();
  });

  it("offers Desactivar for an active user and Activar for an inactive one", async () => {
    // Arrange
    renderUsers({ getUsers: async () => ok([ana, carla, luis]) });
    await screen.findByText("ana@example.com");
    const user = userEvent.setup();
    await user.click(screen.getByRole("switch", { name: "Mostrar inactivos" }));
    await user.click(screen.getByRole("button", { name: "Acciones de Luis Pérez" }));
    const activeMenu = await screen.findByRole("menu");
    expect(within(activeMenu).getByRole("menuitem", { name: "Desactivar" })).toBeInTheDocument();
    expect(within(activeMenu).queryByRole("menuitem", { name: "Activar" })).not.toBeInTheDocument();
    await user.keyboard("{Escape}");
    await waitFor(() => expect(screen.queryByRole("menu")).not.toBeInTheDocument());

    // Act
    await user.click(screen.getByRole("button", { name: "Acciones de Carla Ruiz" }));

    // Assert
    const inactiveMenu = await screen.findByRole("menu");
    expect(within(inactiveMenu).getByRole("menuitem", { name: "Activar" })).toBeInTheDocument();
    expect(
      within(inactiveMenu).queryByRole("menuitem", { name: "Desactivar" }),
    ).not.toBeInTheDocument();
  });

  it("gives the admin no actions on their own row", async () => {
    // Arrange & Act
    renderUsers();

    // Assert
    await screen.findByText("ana@example.com");
    expect(
      screen.queryByRole("button", { name: "Acciones de Ana García" }),
    ).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Acciones de Luis Pérez" })).toBeInTheDocument();
  });

  it("deactivates a user after the confirmation and reads the users again", async () => {
    // Arrange
    const getUsers = vi
      .fn<() => Promise<Result<UserSummary[], DomainError>>>()
      .mockResolvedValueOnce(ok([ana, luis]))
      .mockResolvedValueOnce(ok([ana, { ...luis, isActive: false }]));
    const { execute, deactivate, activate } = renderUsers({ getUsers });
    const user = userEvent.setup();
    await screen.findByText("luis@example.com");
    await user.click(screen.getByRole("button", { name: "Acciones de Luis Pérez" }));
    await user.click(await screen.findByRole("menuitem", { name: "Desactivar" }));
    const dialog = await screen.findByRole("dialog", { name: "¿Desactivar a Luis Pérez?" });
    expect(deactivate).not.toHaveBeenCalled();

    // Act
    await user.click(within(dialog).getByRole("button", { name: "Desactivar" }));

    // Assert
    expect(await screen.findByText("Usuario desactivado.")).toBeInTheDocument();
    await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
    expect(deactivate).toHaveBeenCalledWith(luis.id);
    expect(activate).not.toHaveBeenCalled();
    expect(execute).toHaveBeenCalledTimes(2);
    await waitFor(() => expect(screen.queryByText("luis@example.com")).not.toBeInTheDocument());
  });

  it("activates an inactive user after the confirmation", async () => {
    // Arrange
    const { activate } = renderUsers({ getUsers: async () => ok([ana, carla]) });
    const user = userEvent.setup();
    await screen.findByText("ana@example.com");
    await user.click(screen.getByRole("switch", { name: "Mostrar inactivos" }));
    await user.click(screen.getByRole("button", { name: "Acciones de Carla Ruiz" }));
    await user.click(await screen.findByRole("menuitem", { name: "Activar" }));
    const dialog = await screen.findByRole("dialog", { name: "¿Activar a Carla Ruiz?" });

    // Act
    await user.click(within(dialog).getByRole("button", { name: "Activar" }));

    // Assert
    expect(await screen.findByText("Usuario activado.")).toBeInTheDocument();
    expect(activate).toHaveBeenCalledWith(carla.id);
  });

  it("reads the users again when the dialog closes after a conflict", async () => {
    // Arrange
    const { execute } = renderUsers({
      getUsers: async () => ok([ana, luis]),
      deactivateUser: async () => fail(new DomainError("CONFLICT", "Already deactivated")),
    });
    const user = userEvent.setup();
    await screen.findByText("luis@example.com");
    await user.click(screen.getByRole("button", { name: "Acciones de Luis Pérez" }));
    await user.click(await screen.findByRole("menuitem", { name: "Desactivar" }));
    const dialog = await screen.findByRole("dialog");
    await user.click(within(dialog).getByRole("button", { name: "Desactivar" }));
    await within(dialog).findByText("El usuario ya estaba en ese estado. Recarga la lista.");

    // Act
    await user.click(within(dialog).getByRole("button", { name: "Cancelar" }));

    // Assert
    await waitFor(() => expect(execute).toHaveBeenCalledTimes(2));
  });

  it("shows no activation actions to a trainer", async () => {
    // Arrange & Act
    renderUsers({ profile: completeProfile, getUsers: async () => ok([ana, carla, luis]) });

    // Assert
    await screen.findByText("luis@example.com");
    expect(screen.queryByRole("columnheader", { name: "Acciones" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /^Acciones de/ })).not.toBeInTheDocument();
    expect(screen.queryByRole("menu")).not.toBeInTheDocument();
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  });

  it("follows the active language", async () => {
    // Arrange
    renderUsers();
    await screen.findByText("ana@example.com");

    // Act
    await act(() => i18n.changeLanguage("en"));

    // Assert
    expect(screen.getByRole("heading", { level: 1, name: "All users" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Add user" })).toBeInTheDocument();
    for (const header of ["Full name", "Email", "Profile"]) {
      expect(screen.getByRole("columnheader", { name: header })).toBeInTheDocument();
    }
    expect(screen.getByRole("searchbox", { name: "Search by name or email" })).toBeInTheDocument();
    expect(screen.getByText("Trainer", { selector: "td" })).toBeInTheDocument();
  });

  it("follows the active language in the activation copy", async () => {
    // Arrange
    renderUsers();
    const user = userEvent.setup();
    await screen.findByText("luis@example.com");
    await act(() => i18n.changeLanguage("en"));
    await user.click(screen.getByRole("button", { name: "Actions for Luis Pérez" }));
    await user.click(await screen.findByRole("menuitem", { name: "Deactivate" }));

    // Act
    const dialog = await screen.findByRole("dialog", { name: "Deactivate Luis Pérez?" });

    // Assert
    // The open dialog hides the page behind it from the accessibility tree.
    expect(screen.getByRole("switch", { name: "Show inactive", hidden: true })).toBeInTheDocument();
    expect(within(dialog).getByRole("button", { name: "Deactivate" })).toBeInTheDocument();
  });
});
