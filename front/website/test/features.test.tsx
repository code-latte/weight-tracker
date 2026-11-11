import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import Ai from "@/sections/ai";
import Features from "@/sections/features";

/** The list item that holds a capability's text. */
const itemOf = (text: string) => screen.getByText(text).closest("li")!;

describe("Features", () => {
  it("shows the four capabilities", () => {
    // Arrange & Act
    render(<Features />);

    // Assert
    expect(
      screen.getAllByRole("heading", { level: 3 }).map((heading) => heading.textContent),
    ).toEqual([
      "Todos tus deportistas bajo control.",
      "Planifica con estructura. Entrena con intención.",
      "Cada entrenamiento cuenta. Cada progreso también.",
      "Conoce cómo está evolucionando cada deportista.Próximamente",
    ]);
  });

  it("shows workout planning without any Próximamente label", () => {
    // Arrange & Act
    render(<Features />);

    // Assert
    const planning = screen.getByRole("article", {
      name: "Planifica con estructura. Entrena con intención.",
    });
    expect(planning).not.toHaveTextContent("Próximamente");
  });

  it("labels the capabilities the product does not have yet", () => {
    // Arrange & Act
    render(<Features />);

    // Assert
    for (const text of [
      "Feedback de entrenamientos",
      "Gráficas de progresión",
      "Estadísticas",
      "Evolución de pesos",
    ]) {
      expect(itemOf(text)).toHaveTextContent("Próximamente");
    }
    expect(itemOf("Registro de resultados")).not.toHaveTextContent("Próximamente");
  });
});

describe("Ai", () => {
  it("labels every capability as coming and keeps the trainer in control", () => {
    // Arrange & Act
    render(<Ai />);

    // Assert
    const capabilities = screen.getAllByRole("listitem");
    expect(capabilities).toHaveLength(5);
    for (const capability of capabilities) {
      expect(capability).toHaveTextContent("Próximamente");
    }
    expect(screen.getByText("La IA propone. El entrenador decide.")).toBeInTheDocument();
  });
});
