import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import LegalPage from "@/components/legal-page";
import { legalPageOf } from "@/content/legal";

describe("LegalPage", () => {
  it("shows the pending notice while the text has not been supplied", () => {
    // Arrange
    const page = legalPageOf("/privacidad");

    // Act
    render(<LegalPage page={page} />);

    // Assert
    expect(
      screen.getByRole("heading", { level: 1, name: "Política de privacidad" }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("heading", { level: 2, name: "Contenido pendiente" }),
    ).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Volver al inicio" })).toHaveAttribute("href", "/");
  });

  it("shows the text once it is supplied", () => {
    // Arrange
    const page = { ...legalPageOf("/aviso-legal"), body: ["Trainera es un servicio de ejemplo."] };

    // Act
    render(<LegalPage page={page} />);

    // Assert
    expect(screen.getByText("Trainera es un servicio de ejemplo.")).toBeInTheDocument();
    expect(screen.queryByText("Contenido pendiente")).not.toBeInTheDocument();
  });

  it("links the logo back to the landing", () => {
    // Arrange
    const page = legalPageOf("/cookies");

    // Act
    render(<LegalPage page={page} />);

    // Assert
    expect(screen.getByRole("link", { name: "Trainera, ir al inicio" })).toHaveAttribute(
      "href",
      "/",
    );
  });
});
