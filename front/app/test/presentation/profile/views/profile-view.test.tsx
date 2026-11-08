import { act, fireEvent, screen, waitFor, within } from "@testing-library/react-native";
import { DomainError, fail, ok } from "@weight-tracker/core/domain";
import type { Result, UserProfile } from "@weight-tracker/core/domain";
import i18n from "@/infrastructure/i18n";
import ProfileView from "@/presentation/profile/views/profile-view";
import renderWithProviders from "../../../support/render-with-providers";

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

interface SetupOptions {
  profileResult?: () => Promise<Result<UserProfile, DomainError>>;
  trainerResult?: () => Promise<Result<UserProfile | null, DomainError>>;
  logoutResult?: Promise<Result<void, DomainError>>;
}

function setup({
  profileResult = async () => ok(athlete),
  trainerResult = async () => ok(trainer),
  logoutResult = Promise.resolve(ok(undefined)),
}: SetupOptions = {}) {
  const getProfile = jest.fn(profileResult);
  const getTrainer = jest.fn(trainerResult);
  const logout = jest.fn(() => logoutResult);
  const getSession = jest.fn(async () => ok(null));
  const dependencies = {
    getMyProfileUseCase: () => ({ execute: getProfile }),
    getUserTrainerUseCase: () => ({ execute: getTrainer }),
    logoutUseCase: () => ({ execute: logout }),
    getSessionUseCase: () => ({ execute: getSession }),
  };

  return { getProfile, getTrainer, logout, getSession, dependencies };
}

const menuTitles = [
  "Mis Datos",
  "Mis Objetivos",
  "Limitaciones",
  "Notificaciones",
  "Acerca de Trainera",
];

describe("ProfileView", () => {
  it("shows the title and the settings button", async () => {
    // Arrange & Act
    const { dependencies } = setup();
    await renderWithProviders(<ProfileView />, dependencies);

    // Assert
    expect(screen.getByRole("heading", { name: "Perfil" })).toBeOnTheScreen();
    expect(screen.getByRole("button", { name: "Ajustes" })).toBeOnTheScreen();
  });

  it("shows a loading indicator and no identity while the profile is read", async () => {
    // Arrange & Act
    const { dependencies } = setup({ profileResult: () => new Promise(() => {}) });
    await renderWithProviders(<ProfileView />, dependencies);

    // Assert
    expect(screen.getByRole("progressbar")).toBeOnTheScreen();
    expect(screen.queryByText("Ana López")).not.toBeOnTheScreen();
  });

  it("shows the athlete's initials, full name and email", async () => {
    // Arrange & Act
    const { dependencies } = setup();
    await renderWithProviders(<ProfileView />, dependencies);

    // Assert
    expect(await screen.findByText("Ana López")).toBeOnTheScreen();
    expect(screen.getByText("AL")).toBeOnTheScreen();
    expect(screen.getByText("ana@example.com")).toBeOnTheScreen();
  });

  it("reads the trainer of the signed-in athlete", async () => {
    // Arrange
    const { getTrainer, dependencies } = setup();

    // Act
    await renderWithProviders(<ProfileView />, dependencies);

    // Assert
    await waitFor(() => expect(getTrainer).toHaveBeenCalledWith(athlete.id));
  });

  it("shows the trainer card with the trainer's initials and name", async () => {
    // Arrange & Act
    const { dependencies } = setup();
    await renderWithProviders(<ProfileView />, dependencies);

    // Assert
    expect(await screen.findByText("Mi entrenador")).toBeOnTheScreen();
    expect(screen.getByText("CR")).toBeOnTheScreen();
    expect(screen.getByText("Carlos Ruiz")).toBeOnTheScreen();
  });

  it("shows no trainer card when the athlete has no trainer", async () => {
    // Arrange & Act
    const { dependencies } = setup({ trainerResult: async () => ok(null) });
    await renderWithProviders(<ProfileView />, dependencies);

    // Assert
    expect(await screen.findByText("Ana López")).toBeOnTheScreen();
    expect(screen.queryByText("Mi entrenador")).not.toBeOnTheScreen();
    expect(screen.getByText("Mis Datos")).toBeOnTheScreen();
  });

  it("shows the five menu items in order, with subtitles on the first three", async () => {
    // Arrange & Act
    const { dependencies } = setup();
    await renderWithProviders(<ProfileView />, dependencies);

    // Assert
    await screen.findByText("Mis Datos");
    const shown = screen
      .getAllByText(new RegExp(`^(${menuTitles.join("|")})$`))
      .map((element) => element.props.children);
    expect(shown).toEqual(menuTitles);
    expect(screen.getByText("Medidas, pesos, edad...")).toBeOnTheScreen();
    expect(screen.getByText("Grasa, Músculo, Rendimiento...")).toBeOnTheScreen();
    expect(screen.getByText("Lesiones, molestias, observaciones...")).toBeOnTheScreen();
  });

  it("does nothing when the settings button or a menu item is pressed", async () => {
    // Arrange
    const { getProfile, getTrainer, logout, dependencies } = setup();
    await renderWithProviders(<ProfileView />, dependencies);
    await screen.findByText("Mis Datos");

    // Act
    await fireEvent.press(screen.getByRole("button", { name: "Ajustes" }));
    await fireEvent.press(screen.getByText("Mis Datos"));

    // Assert
    expect(getProfile).toHaveBeenCalledTimes(1);
    expect(getTrainer).toHaveBeenCalledTimes(1);
    expect(logout).not.toHaveBeenCalled();
    expect(screen.getByText("Ana López")).toBeOnTheScreen();
  });

  it("shows the error and retry, and no identity, when the profile cannot be read", async () => {
    // Arrange & Act
    const { getTrainer, dependencies } = setup({
      profileResult: async () => fail(new DomainError("NETWORK", "")),
    });
    await renderWithProviders(<ProfileView />, dependencies);

    // Assert
    expect(await screen.findByText("No hay conexión con el servidor.")).toBeOnTheScreen();
    expect(screen.getByRole("button", { name: "Reintentar" })).toBeOnTheScreen();
    expect(screen.queryByText("Ana López")).not.toBeOnTheScreen();
    expect(getTrainer).not.toHaveBeenCalled();
  });

  it("shows the error and no partial profile when the trainer cannot be read", async () => {
    // Arrange & Act
    const { dependencies } = setup({
      trainerResult: async () => fail(new DomainError("UNEXPECTED", "")),
    });
    await renderWithProviders(<ProfileView />, dependencies);

    // Assert
    expect(await screen.findByText("Ha ocurrido un error inesperado.")).toBeOnTheScreen();
    expect(screen.getByRole("button", { name: "Reintentar" })).toBeOnTheScreen();
    expect(screen.queryByText("Ana López")).not.toBeOnTheScreen();
  });

  it("reads the profile and the trainer again when retry is pressed", async () => {
    // Arrange
    const profileResult = jest
      .fn<Promise<Result<UserProfile, DomainError>>, []>()
      .mockResolvedValueOnce(fail(new DomainError("NETWORK", "")))
      .mockResolvedValue(ok(athlete));
    const { getTrainer, dependencies } = setup({ profileResult });
    await renderWithProviders(<ProfileView />, dependencies);
    await screen.findByRole("button", { name: "Reintentar" });

    // Act
    await fireEvent.press(screen.getByRole("button", { name: "Reintentar" }));

    // Assert
    expect(await screen.findByText("Ana López")).toBeOnTheScreen();
    expect(screen.getByText("Carlos Ruiz")).toBeOnTheScreen();
    expect(profileResult).toHaveBeenCalledTimes(2);
    expect(getTrainer).toHaveBeenCalledTimes(1);
  });

  it("shows the logout button", async () => {
    // Arrange & Act
    const { dependencies } = setup();
    await renderWithProviders(<ProfileView />, dependencies);

    // Assert
    expect(screen.getByRole("button", { name: "Cerrar sesión" })).toBeOnTheScreen();
  });

  it("logs out and reads the session again when the logout button is pressed", async () => {
    // Arrange
    const { logout, getSession, dependencies } = setup();
    await renderWithProviders(<ProfileView />, dependencies);
    await waitFor(() => expect(getSession).toHaveBeenCalledTimes(1));

    // Act
    await fireEvent.press(screen.getByRole("button", { name: "Cerrar sesión" }));

    // Assert
    await waitFor(() => expect(getSession).toHaveBeenCalledTimes(2));
    expect(logout).toHaveBeenCalledTimes(1);
  });

  it("disables the logout button while logging out", async () => {
    // Arrange
    const { dependencies } = setup({ logoutResult: new Promise(() => {}) });
    await renderWithProviders(<ProfileView />, dependencies);

    // Act
    await fireEvent.press(screen.getByRole("button", { name: "Cerrar sesión" }));

    // Assert
    await waitFor(() =>
      expect(screen.getByRole("button", { name: "Cerrar sesión" })).toBeDisabled(),
    );
  });

  it("shows the error and does not read the session again when logging out fails", async () => {
    // Arrange
    const { getSession, dependencies } = setup({
      logoutResult: Promise.resolve(fail(new DomainError("UNEXPECTED", ""))),
    });
    await renderWithProviders(<ProfileView />, dependencies);
    await waitFor(() => expect(getSession).toHaveBeenCalledTimes(1));

    // Act
    await fireEvent.press(screen.getByRole("button", { name: "Cerrar sesión" }));

    // Assert
    expect(await screen.findByText("Ha ocurrido un error inesperado.")).toBeOnTheScreen();
    expect(getSession).toHaveBeenCalledTimes(1);
  });

  it("shows the texts in English", async () => {
    // Arrange
    const { dependencies } = setup();
    await renderWithProviders(<ProfileView />, dependencies);
    await screen.findByText("Ana López");

    // Act
    await act(async () => {
      await i18n.changeLanguage("en");
    });

    // Assert
    expect(screen.getByRole("heading", { name: "Profile" })).toBeOnTheScreen();
    expect(screen.getByRole("button", { name: "Settings" })).toBeOnTheScreen();
    for (const title of ["My data", "My goals", "Limitations", "Notifications", "About Trainera"]) {
      expect(screen.getByText(title)).toBeOnTheScreen();
    }
    expect(
      within(screen.getByRole("button", { name: "Log out" })).getByText("Log out"),
    ).toBeOnTheScreen();
  });
});
