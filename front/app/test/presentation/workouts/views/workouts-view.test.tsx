import { screen } from "@testing-library/react-native";
import WorkoutsView from "@/presentation/workouts/views/workouts-view";
import renderWithProviders from "../../../support/render-with-providers";

describe("WorkoutsView", () => {
  it("shows the heading", async () => {
    // Arrange & Act
    await renderWithProviders(<WorkoutsView />);

    // Assert
    expect(screen.getByRole("heading", { name: "Entrenamientos" })).toBeOnTheScreen();
  });
});
