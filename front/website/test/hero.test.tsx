import { render, screen, within } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import AthleteExperience from "@/sections/athlete-experience";
import Hero from "@/sections/hero";
import HowItWorks from "@/sections/how-it-works";
import Problem from "@/sections/problem";

describe("Hero", () => {
  it("holds the page's only level-1 heading", () => {
    // Arrange & Act
    render(<Hero />);

    // Assert
    const headings = screen.getAllByRole("heading", { level: 1 });
    expect(headings).toHaveLength(1);
    expect(headings[0]).toHaveTextContent("Menos gestión. Más entrenamiento.");
  });

  it("leads its main call to action to the form and the secondary one to the features", () => {
    // Arrange & Act
    render(<Hero />);

    // Assert
    expect(screen.getByRole("link", { name: "Solicitar acceso anticipado" })).toHaveAttribute(
      "href",
      "#acceso-anticipado",
    );
    expect(screen.getByRole("link", { name: "Descubrir Trainera" })).toHaveAttribute(
      "href",
      "#funcionalidades",
    );
  });

  it("marks its product images as demonstrative", () => {
    // Arrange & Act
    render(<Hero />);

    // Assert
    const images = screen.getAllByRole("img");
    expect(images.length).toBeGreaterThan(0);
    for (const image of images) {
      expect(image.getAttribute("alt")).toMatch(/^Imagen demostrativa: /);
    }
  });
});

describe("Problem", () => {
  it("shows the four problems and the closing line", () => {
    // Arrange & Act
    render(<Problem />);

    // Assert
    const section = screen.getByRole("region", { name: /Gestionarlo todo no debería serlo/ });
    expect(
      within(section)
        .getAllByRole("heading", { level: 3 })
        .map((heading) => heading.textContent),
    ).toEqual([
      "PDFs y documentos dispersos",
      "Seguimiento manual",
      "Progresiones difíciles de controlar",
      "Demasiado tiempo gestionando",
    ]);
    expect(
      within(section).getByText(/lo que realmente importa: tus deportistas/),
    ).toBeInTheDocument();
  });
});

describe("HowItWorks", () => {
  it("lists the three steps, labelling the one that is not available yet", () => {
    // Arrange & Act
    render(<HowItWorks />);

    // Assert
    const steps = screen.getAllByRole("listitem");
    expect(steps.map((step) => within(step).getByRole("heading").textContent)).toEqual([
      "Organiza",
      "Entrena",
      "AnalizaPróximamente",
    ]);
  });
});

describe("AthleteExperience", () => {
  it("shows the trainer's benefits and labels the athlete capabilities not available yet", () => {
    // Arrange & Act
    render(<AthleteExperience />);

    // Assert
    for (const benefit of ["Control", "Profesionalización", "Eficiencia"]) {
      expect(screen.getByText(benefit)).toBeInTheDocument();
    }
    expect(screen.getByText("Temporizador de descanso").parentElement).toHaveTextContent(
      "Próximamente",
    );
    expect(screen.getByText("Entrenamiento del día").parentElement).not.toHaveTextContent(
      "Próximamente",
    );
  });
});
