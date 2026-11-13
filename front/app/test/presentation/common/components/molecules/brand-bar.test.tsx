import { screen } from "@testing-library/react-native";
import BrandBar from "@/presentation/common/components/molecules/brand-bar";
import renderWithProviders from "../../../../support/render-with-providers";

describe("BrandBar", () => {
  it("shows the logo, named after the product", async () => {
    // Arrange & Act
    await renderWithProviders(<BrandBar name="Trainera" />);

    // Assert
    expect(screen.getByRole("image", { name: "Trainera" })).toBeOnTheScreen();
  });
});
