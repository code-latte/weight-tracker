import { fireEvent, screen, waitFor } from "@testing-library/react-native";
import { DomainError, fail, ok, type Result } from "@weight-tracker/core/domain";
import SettingsView from "@/presentation/profile/views/settings-view";
import renderWithProviders from "../../../support/render-with-providers";

// Paper's icons load their font through expo-font, which has no assets under Jest.
jest.mock("expo-font", () => ({
  ...jest.requireActual("expo-font"),
  isLoaded: () => true,
  loadAsync: async () => {},
}));

const mockColorScheme = jest.fn<"light" | "dark" | null, []>(() => "light");

jest.mock("react-native/Libraries/Utilities/useColorScheme", () => ({
  __esModule: true,
  default: () => mockColorScheme(),
}));

interface SetupOptions {
  storedThemeMode?: string | null;
  storedLanguage?: string | null;
  storeFails?: boolean;
}

function setup({
  storedThemeMode = null,
  storedLanguage = null,
  storeFails = false,
}: SetupOptions = {}) {
  const stored = async (): Promise<Result<void, DomainError>> =>
    storeFails ? fail(new DomainError("UNEXPECTED", "Storage failed")) : ok(undefined);
  const setThemeMode = jest.fn(stored);
  const setLanguage = jest.fn(stored);
  const dependencies = {
    getPreferredThemeModeUseCase: () => ({ execute: async () => ok(storedThemeMode) }),
    getPreferredLanguageUseCase: () => ({ execute: async () => ok(storedLanguage) }),
    setPreferredThemeModeUseCase: () => ({ execute: setThemeMode }),
    setPreferredLanguageUseCase: () => ({ execute: setLanguage }),
  };

  return { setThemeMode, setLanguage, dependencies };
}

/** Paper marks the selected segment with `checked` on a button, which RNTL's `checked` filter does not read. */
async function expectSelected(name: string, selected = true) {
  await waitFor(() =>
    expect(screen.getByRole("button", { name }).props.accessibilityState).toMatchObject({
      checked: selected,
    }),
  );
}

describe("SettingsView", () => {
  beforeEach(() => {
    mockColorScheme.mockReturnValue("light");
  });

  it("shows the theme and language settings with their options", async () => {
    // Arrange & Act
    const { dependencies } = setup();
    await renderWithProviders(<SettingsView />, dependencies);

    // Assert
    expect(screen.getByText("Tema")).toBeOnTheScreen();
    expect(screen.getByRole("button", { name: "Claro" })).toBeOnTheScreen();
    expect(screen.getByRole("button", { name: "Oscuro" })).toBeOnTheScreen();
    expect(screen.getByText("Idioma")).toBeOnTheScreen();
    expect(screen.getByRole("button", { name: "Español" })).toBeOnTheScreen();
    expect(screen.getByRole("button", { name: "English" })).toBeOnTheScreen();
  });

  it("selects the stored values", async () => {
    // Arrange & Act
    const { dependencies } = setup({ storedThemeMode: "dark" });
    await renderWithProviders(<SettingsView />, dependencies);

    // Assert
    await expectSelected("Oscuro");
    await expectSelected("Claro", false);
    await expectSelected("Español");
  });

  it("selects the device colour mode when nothing is stored", async () => {
    // Arrange
    mockColorScheme.mockReturnValue("dark");
    const { dependencies } = setup();

    // Act
    await renderWithProviders(<SettingsView />, dependencies);

    // Assert
    await expectSelected("Oscuro");
  });

  it("stores the dark mode when it is chosen", async () => {
    // Arrange
    const { setThemeMode, dependencies } = setup();
    await renderWithProviders(<SettingsView />, dependencies);

    // Act
    fireEvent.press(screen.getByRole("button", { name: "Oscuro" }));

    // Assert
    await expectSelected("Oscuro");
    expect(setThemeMode).toHaveBeenCalledWith("dark");
  });

  it("stores English and shows the screen in English when it is chosen", async () => {
    // Arrange
    const { setLanguage, dependencies } = setup();
    await renderWithProviders(<SettingsView />, dependencies);

    // Act
    fireEvent.press(screen.getByRole("button", { name: "English" }));

    // Assert
    expect(await screen.findByText("Theme")).toBeOnTheScreen();
    await expectSelected("English");
    expect(setLanguage).toHaveBeenCalledWith("en");
  });

  it("applies the choice and shows no error when storing it fails", async () => {
    // Arrange
    const { dependencies } = setup({ storeFails: true });
    await renderWithProviders(<SettingsView />, dependencies);

    // Act
    fireEvent.press(screen.getByRole("button", { name: "Oscuro" }));

    // Assert
    await expectSelected("Oscuro");
    expect(screen.queryByText("Ha ocurrido un error inesperado.")).not.toBeOnTheScreen();
  });
});
