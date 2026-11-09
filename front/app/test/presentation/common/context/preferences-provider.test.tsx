import { Text } from "react-native";
import { fireEvent, screen } from "@testing-library/react-native";
import { DomainError, fail, ok } from "@weight-tracker/core/domain";
import usePreferences from "@/presentation/common/hooks/use-preferences";
import renderWithProviders from "../../../support/render-with-providers";

const mockColorScheme = jest.fn<"light" | "dark" | null, []>(() => "light");

jest.mock("react-native/Libraries/Utilities/useColorScheme", () => ({
  __esModule: true,
  default: () => mockColorScheme(),
}));

const unexpected = fail(new DomainError("UNEXPECTED", "Storage failed"));

function stored(themeMode: unknown, language: unknown) {
  return {
    getPreferredThemeModeUseCase: () => ({ execute: async () => ok(themeMode as string | null) }),
    getPreferredLanguageUseCase: () => ({ execute: async () => ok(language as string | null) }),
  };
}

function Probe() {
  const { themeMode, language, ready, setThemeMode, setLanguage } = usePreferences();

  return (
    <>
      <Text>{`ready:${ready}`}</Text>
      <Text>{`theme:${themeMode}`}</Text>
      <Text>{`language:${language}`}</Text>
      <Text onPress={() => setThemeMode("dark")}>dark</Text>
      <Text onPress={() => setLanguage("en")}>english</Text>
    </>
  );
}

function renderProbe(dependencies: Parameters<typeof renderWithProviders>[1] = {}) {
  // The helper mounts PreferencesProvider around the probe.
  return renderWithProviders(<Probe />, dependencies);
}

describe("PreferencesProvider", () => {
  beforeEach(() => {
    mockColorScheme.mockReturnValue("light");
  });

  it("is not ready while the stored preferences are being read", async () => {
    // Arrange
    const getPreferredThemeModeUseCase = () => ({ execute: () => new Promise<never>(() => {}) });

    // Act
    await renderProbe({ getPreferredThemeModeUseCase });

    // Assert
    expect(screen.getByText("ready:false")).toBeOnTheScreen();
  });

  it("uses the stored values over the device ones", async () => {
    // Arrange
    mockColorScheme.mockReturnValue("light");

    // Act
    await renderProbe(stored("dark", "en"));

    // Assert
    expect(await screen.findByText("ready:true")).toBeOnTheScreen();
    expect(screen.getByText("theme:dark")).toBeOnTheScreen();
    expect(screen.getByText("language:en")).toBeOnTheScreen();
  });

  it("follows the device when nothing is stored", async () => {
    // Arrange
    mockColorScheme.mockReturnValue("dark");

    // Act
    await renderProbe(stored(null, null));

    // Assert
    expect(await screen.findByText("ready:true")).toBeOnTheScreen();
    expect(screen.getByText("theme:dark")).toBeOnTheScreen();
    expect(screen.getByText("language:es")).toBeOnTheScreen();
  });

  it("ignores stored values that are not known options", async () => {
    // Arrange
    mockColorScheme.mockReturnValue("light");

    // Act
    await renderProbe(stored("sepia", "fr"));

    // Assert
    expect(await screen.findByText("ready:true")).toBeOnTheScreen();
    expect(screen.getByText("theme:light")).toBeOnTheScreen();
    expect(screen.getByText("language:es")).toBeOnTheScreen();
  });

  it("follows the device and becomes ready when reading fails", async () => {
    // Arrange
    mockColorScheme.mockReturnValue("dark");
    const failing = {
      getPreferredThemeModeUseCase: () => ({ execute: async () => unexpected }),
      getPreferredLanguageUseCase: () => ({ execute: async () => unexpected }),
    };

    // Act
    await renderProbe(failing);

    // Assert
    expect(await screen.findByText("ready:true")).toBeOnTheScreen();
    expect(screen.getByText("theme:dark")).toBeOnTheScreen();
    expect(screen.getByText("language:es")).toBeOnTheScreen();
  });

  it("stays not ready until both preferences are read", async () => {
    // Arrange
    const getPreferredLanguageUseCase = () => ({ execute: () => new Promise<never>(() => {}) });

    // Act
    await renderProbe({ ...stored("dark", null), getPreferredLanguageUseCase });

    // Assert
    expect(screen.getByText("ready:false")).toBeOnTheScreen();
  });

  it("applies and stores a chosen colour mode", async () => {
    // Arrange
    const execute = jest.fn(async () => ok(undefined));
    await renderProbe({ setPreferredThemeModeUseCase: () => ({ execute }) });
    await screen.findByText("ready:true");

    // Act
    fireEvent.press(screen.getByText("dark"));

    // Assert
    expect(await screen.findByText("theme:dark")).toBeOnTheScreen();
    expect(execute).toHaveBeenCalledWith("dark");
  });

  it("applies and stores a chosen language", async () => {
    // Arrange
    const execute = jest.fn(async () => ok(undefined));
    await renderProbe({ setPreferredLanguageUseCase: () => ({ execute }) });
    await screen.findByText("ready:true");

    // Act
    fireEvent.press(screen.getByText("english"));

    // Assert
    expect(await screen.findByText("language:en")).toBeOnTheScreen();
    expect(execute).toHaveBeenCalledWith("en");
  });

  it("still applies a choice when storing it fails", async () => {
    // Arrange
    const setPreferredThemeModeUseCase = () => ({ execute: async () => unexpected });
    await renderProbe({ setPreferredThemeModeUseCase });
    await screen.findByText("ready:true");

    // Act
    fireEvent.press(screen.getByText("dark"));

    // Assert
    expect(await screen.findByText("theme:dark")).toBeOnTheScreen();
  });
});
