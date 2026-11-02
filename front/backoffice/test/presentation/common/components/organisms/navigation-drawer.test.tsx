import { screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ok } from "@weight-tracker/core/domain";
import type { DomainError, Result, UserProfile } from "@weight-tracker/core/domain";
import NavigationDrawerProvider from "@/presentation/common/components/navigation-drawer-provider";
import NavigationDrawer from "@/presentation/common/components/organisms/navigation-drawer";
import LoadProfile from "../../../../support/load-profile";
import renderWithProviders, { completeProfile } from "../../../../support/render-with-providers";

const replace = vi.fn();
const navigation = vi.hoisted(() => ({ pathname: "/" }));

vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace }),
  usePathname: () => navigation.pathname,
}));

const adminProfile: UserProfile = { ...completeProfile, email: "admin@example.com", role: "ADMIN" };

function renderDrawerFor(getProfile: () => Promise<Result<UserProfile, DomainError>>) {
  return renderWithProviders(
    <LoadProfile>
      <NavigationDrawer />
    </LoadProfile>,
    { getMyProfileUseCase: () => ({ execute: getProfile }) },
  );
}

function renderAsAdmin() {
  return renderDrawerFor(async () => ok(adminProfile));
}

const items = [
  "Inicio",
  "Todos los atletas",
  "Invitaciones",
  "Planes asignados",
  "Calendario",
  "Plantillas de entrenamiento",
  "Catálogo de ejercicios",
  "Feedback de entrenamientos",
  "Revisiones semanales",
  "Pendientes de revisión",
];

describe("NavigationDrawer", () => {
  beforeEach(() => {
    replace.mockClear();
    navigation.pathname = "/";
  });

  it("shows the app name row, then every section and item in order", async () => {
    // Arrange & Act
    await renderWithProviders(<NavigationDrawer />);

    // Assert
    const nav = screen.getByRole("navigation", { name: "Menú principal" });
    expect(
      within(nav)
        .getAllByText(/./)
        .map((element) => element.textContent),
    ).toEqual([
      "Inicio",
      "Atletas",
      "Todos los atletas",
      "Invitaciones",
      "Programación",
      "Planes asignados",
      "Calendario",
      "Biblioteca",
      "Plantillas de entrenamiento",
      "Catálogo de ejercicios",
      "Seguimiento",
      "Feedback de entrenamientos",
      "Revisiones semanales",
      "Pendientes de revisión",
    ]);
  });

  it("shows the app name as a logo row that is not an entry", async () => {
    // Arrange & Act
    await renderWithProviders(<NavigationDrawer />);

    // Assert
    const logo = screen.getByRole("img", { name: "Trainera" });
    expect(logo).toHaveAttribute("src", "/trainera-logo-dark.svg");
    expect(logo.closest("a, button, [role='button']")).toBeNull();
  });

  it("makes Inicio the first entry, linked to home and selected", async () => {
    // Arrange & Act
    await renderWithProviders(<NavigationDrawer />);

    // Assert
    const nav = screen.getByRole("navigation", { name: "Menú principal" });
    const [first] = within(nav).getAllByRole("link");
    expect(first).toHaveTextContent("Inicio");
    expect(first).toHaveAttribute("href", "/");
    expect(first).toHaveClass("Mui-selected");
  });

  it("links Planes asignados to its screen, not selected elsewhere", async () => {
    // Arrange & Act
    await renderWithProviders(<NavigationDrawer />);

    // Assert
    const entry = screen.getByRole("link", { name: "Planes asignados" });
    expect(entry).toHaveAttribute("href", "/assigned-plans");
    expect(entry).not.toHaveClass("Mui-selected");
  });

  it("selects Planes asignados and not Inicio on its screen", async () => {
    // Arrange
    navigation.pathname = "/assigned-plans";

    // Act
    await renderWithProviders(<NavigationDrawer />);

    // Assert
    expect(screen.getByRole("link", { name: "Planes asignados" })).toHaveClass("Mui-selected");
    expect(screen.getByRole("link", { name: "Inicio" })).not.toHaveClass("Mui-selected");
  });

  it("keeps every other item without a link", async () => {
    // Arrange & Act
    await renderWithProviders(<NavigationDrawer />);

    // Assert
    const nav = screen.getByRole("navigation", { name: "Menú principal" });
    expect(
      within(nav)
        .getAllByRole("link")
        .map((link) => link.textContent),
    ).toEqual(["Inicio", "Todos los atletas", "Invitaciones", "Planes asignados"]);
  });

  it("shows an icon on every item", async () => {
    // Arrange & Act
    await renderWithProviders(<NavigationDrawer />);

    // Assert
    for (const item of items) {
      const entry = screen.getByText(item).closest("a, [role='button']") as HTMLElement;
      expect(within(entry).getByTestId(/Icon$/)).toBeInTheDocument();
    }
  });

  it("no longer shows the settings section or logout", async () => {
    // Arrange & Act
    await renderWithProviders(<NavigationDrawer />);

    // Assert
    for (const text of [
      "Configuración",
      "Mi perfil",
      "Preferencias",
      "Cerrar sesión",
      "Invitaciones pendientes",
    ]) {
      expect(screen.queryByText(text)).not.toBeInTheDocument();
    }
  });

  it("does nothing when a section header or a placeholder item is clicked", async () => {
    // Arrange
    const execute = vi.fn();
    await renderWithProviders(<NavigationDrawer />, { logoutUseCase: () => ({ execute }) });
    const user = userEvent.setup();

    // Act
    await user.click(screen.getByText("Atletas"));
    await user.click(screen.getByRole("button", { name: "Calendario" }));

    // Assert
    expect(execute).not.toHaveBeenCalled();
    expect(replace).not.toHaveBeenCalled();
  });

  it("shows the users copy to an admin, linked to the users screen", async () => {
    // Arrange & Act
    await renderAsAdmin();

    // Assert
    const entry = await screen.findByRole("link", { name: "Todos los usuarios" });
    expect(entry).toHaveAttribute("href", "/users");
    expect(entry).not.toHaveClass("Mui-selected");
    expect(screen.getByText("Usuarios")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Invitaciones" })).toHaveAttribute(
      "href",
      "/invitations",
    );
    expect(screen.queryByText("Atletas")).not.toBeInTheDocument();
    expect(screen.queryByText("Todos los atletas")).not.toBeInTheDocument();
  });

  it("lists the admin's users section in order, with Asignación de entrenadores linked to its screen", async () => {
    // Arrange & Act
    await renderAsAdmin();

    // Assert
    const entry = await screen.findByRole("link", { name: "Asignación de entrenadores" });
    expect(entry).toHaveAttribute("href", "/trainer-assignments");
    expect(entry).not.toHaveClass("Mui-selected");
    const names = screen
      .getAllByRole("link")
      .map((link) => link.textContent)
      .filter((name) =>
        ["Todos los usuarios", "Asignación de entrenadores", "Invitaciones"].includes(name ?? ""),
      );
    expect(names).toEqual(["Todos los usuarios", "Asignación de entrenadores", "Invitaciones"]);
  });

  it("selects Asignación de entrenadores and no other users entry on its screen", async () => {
    // Arrange
    navigation.pathname = "/trainer-assignments";

    // Act
    await renderAsAdmin();

    // Assert
    expect(await screen.findByRole("link", { name: "Asignación de entrenadores" })).toHaveClass(
      "Mui-selected",
    );
    expect(screen.getByRole("link", { name: "Todos los usuarios" })).not.toHaveClass(
      "Mui-selected",
    );
    expect(screen.getByRole("link", { name: "Invitaciones" })).not.toHaveClass("Mui-selected");
  });

  it("selects Todos los usuarios and not Inicio on the users screen", async () => {
    // Arrange
    navigation.pathname = "/users";

    // Act
    await renderAsAdmin();

    // Assert
    expect(await screen.findByRole("link", { name: "Todos los usuarios" })).toHaveClass(
      "Mui-selected",
    );
    expect(screen.getByRole("link", { name: "Inicio" })).not.toHaveClass("Mui-selected");
  });

  it("keeps the athletes copy for a trainer, linked to its screens and not to the users one", async () => {
    // Arrange & Act
    await renderDrawerFor(async () => ok(completeProfile));

    // Assert
    expect(await screen.findByText("Atletas")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Todos los atletas" })).toHaveAttribute(
      "href",
      "/athletes",
    );
    expect(screen.getByRole("link", { name: "Invitaciones" })).toHaveAttribute(
      "href",
      "/invitations",
    );
    expect(screen.queryByText("Usuarios")).not.toBeInTheDocument();
    expect(screen.queryByText("Todos los usuarios")).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: /usuarios/i })).not.toBeInTheDocument();
    expect(screen.queryByText("Asignación de entrenadores")).not.toBeInTheDocument();
  });

  it.each([
    ["/athletes", "Todos los atletas"],
    ["/invitations", "Invitaciones"],
  ])("selects the trainer's entry on %s and not Inicio", async (pathname, name) => {
    // Arrange
    navigation.pathname = pathname;

    // Act
    await renderDrawerFor(async () => ok(completeProfile));

    // Assert
    expect(await screen.findByRole("link", { name })).toHaveClass("Mui-selected");
    expect(screen.getByRole("link", { name: "Inicio" })).not.toHaveClass("Mui-selected");
  });

  it("selects the admin's Invitaciones and not Todos los usuarios on the invitations screen", async () => {
    // Arrange
    navigation.pathname = "/invitations";

    // Act
    await renderAsAdmin();

    // Assert
    expect(await screen.findByRole("link", { name: "Todos los usuarios" })).not.toHaveClass(
      "Mui-selected",
    );
    expect(screen.getByRole("link", { name: "Invitaciones" })).toHaveClass("Mui-selected");
  });

  it("shows the athletes copy while the profile loads", async () => {
    // Arrange & Act
    await renderDrawerFor(() => new Promise(() => {}));

    // Assert
    expect(screen.getByText("Atletas")).toBeInTheDocument();
    expect(screen.getByText("Todos los atletas")).toBeInTheDocument();
    expect(screen.queryByText("Todos los usuarios")).not.toBeInTheDocument();
  });

  it("shows the collapse button in the logo row, expanded", async () => {
    // Arrange & Act
    await renderWithProviders(
      <NavigationDrawerProvider>
        <NavigationDrawer />
      </NavigationDrawerProvider>,
    );

    // Assert
    const toggle = screen.getByRole("button", { name: "Contraer menú" });
    expect(toggle).toHaveAttribute("aria-expanded", "true");
    expect(toggle.parentElement).toContainElement(screen.getByRole("img", { name: "Trainera" }));
  });

  it("collapses to a rail of icons that keep their names", async () => {
    // Arrange
    await renderWithProviders(
      <NavigationDrawerProvider>
        <NavigationDrawer />
      </NavigationDrawerProvider>,
    );

    // Act
    await userEvent.setup().click(screen.getByRole("button", { name: "Contraer menú" }));

    // Assert
    expect(screen.getByRole("button", { name: "Expandir menú" })).toHaveAttribute(
      "aria-expanded",
      "false",
    );
    expect(screen.queryByRole("img", { name: "Trainera" })).not.toBeInTheDocument();
    expect(screen.queryByText("Atletas")).not.toBeInTheDocument();
    expect(screen.queryByText("Inicio")).not.toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Inicio" })).toHaveAttribute("href", "/");
    expect(screen.getByRole("link", { name: "Planes asignados" })).toHaveAttribute(
      "href",
      "/assigned-plans",
    );
    expect(screen.getByRole("button", { name: "Calendario" })).toBeInTheDocument();
  });

  it("keeps Inicio selected while collapsed", async () => {
    // Arrange
    await renderWithProviders(
      <NavigationDrawerProvider>
        <NavigationDrawer />
      </NavigationDrawerProvider>,
    );

    // Act
    await userEvent.setup().click(screen.getByRole("button", { name: "Contraer menú" }));

    // Assert
    expect(screen.getByRole("link", { name: "Inicio" })).toHaveClass("Mui-selected");
  });

  it("expands again with the labels and the logo", async () => {
    // Arrange
    await renderWithProviders(
      <NavigationDrawerProvider>
        <NavigationDrawer />
      </NavigationDrawerProvider>,
    );
    const user = userEvent.setup();
    await user.click(screen.getByRole("button", { name: "Contraer menú" }));

    // Act
    await user.click(screen.getByRole("button", { name: "Expandir menú" }));

    // Assert
    expect(screen.getByRole("button", { name: "Contraer menú" })).toHaveAttribute(
      "aria-expanded",
      "true",
    );
    expect(screen.getByRole("img", { name: "Trainera" })).toBeInTheDocument();
    expect(screen.getByText("Atletas")).toBeInTheDocument();
    expect(screen.getByText("Inicio")).toBeInTheDocument();
  });
});
