import { screen } from "@testing-library/react-native";
import ProgressView from "@/presentation/progress/views/progress-view";
import renderWithProviders from "../../../support/render-with-providers";

describe("ProgressView", () => {
  it("shows the heading", async () => {
    // Arrange & Act
    await renderWithProviders(<ProgressView />);

    // Assert
    expect(screen.getByRole("heading", { name: "Progreso" })).toBeOnTheScreen();
  });
});
