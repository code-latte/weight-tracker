import { fireEvent, screen } from "@testing-library/react-native";
import SectionHeader from "@/presentation/common/components/molecules/section-header";
import renderWithProviders from "../../../../support/render-with-providers";

// Paper's icons load their font through expo-font, which has no assets under Jest.
jest.mock("expo-font", () => ({
  ...jest.requireActual("expo-font"),
  isLoaded: () => true,
  loadAsync: async () => {},
}));

describe("SectionHeader", () => {
  it("shows the section title as a heading", async () => {
    // Arrange
    const onBack = jest.fn();

    // Act
    await renderWithProviders(<SectionHeader title="Ajustes" backLabel="Volver" onBack={onBack} />);

    // Assert
    expect(screen.getByRole("heading", { name: "Ajustes" })).toBeOnTheScreen();
  });

  it("calls onBack when the back caret is pressed", async () => {
    // Arrange
    const onBack = jest.fn();
    await renderWithProviders(<SectionHeader title="Ajustes" backLabel="Volver" onBack={onBack} />);

    // Act
    fireEvent.press(screen.getByLabelText("Volver"));

    // Assert
    expect(onBack).toHaveBeenCalledTimes(1);
  });
});
