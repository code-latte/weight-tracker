import { render, screen, within } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import LegalPage from "@/components/legal-page";
import { legalPageOf } from "@/content/legal";

describe("LegalPage", () => {
  it("shows the pending notice while the text has not been supplied", () => {
    // Arrange
    const page = legalPageOf("/cookies");

    // Act
    render(<LegalPage page={page} />);

    // Assert
    expect(
      screen.getByRole("heading", { level: 1, name: "Política de cookies" }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("heading", { level: 2, name: "Contenido pendiente" }),
    ).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Volver al inicio" })).toHaveAttribute("href", "/");
  });

  it("shows the text once it is supplied", () => {
    // Arrange
    const page = {
      ...legalPageOf("/aviso-legal"),
      body: [
        {
          id: "objeto",
          heading: "1. Objeto",
          blocks: [{ paragraph: "Trainera es un servicio de ejemplo." }],
        },
      ],
    };

    // Act
    render(<LegalPage page={page} />);

    // Assert
    expect(screen.getByRole("heading", { level: 2, name: "1. Objeto" })).toBeInTheDocument();
    expect(screen.getByText("Trainera es un servicio de ejemplo.")).toBeInTheDocument();
    expect(screen.queryByText("Contenido pendiente")).not.toBeInTheDocument();
  });

  it("shows the privacy policy with its update date and an index of its fourteen sections", () => {
    // Arrange
    const page = legalPageOf("/privacidad");

    // Act
    render(<LegalPage page={page} />);

    // Assert
    expect(
      screen.getByRole("heading", { level: 1, name: "Política de privacidad" }),
    ).toBeInTheDocument();
    expect(screen.getByText("11 de noviembre de 2026")).toHaveAttribute("datetime", "2026-11-11");
    const links = within(screen.getByRole("navigation", { name: "Índice" })).getAllByRole("link");
    expect(links).toHaveLength(14);
    expect(links[0]).toHaveAttribute("href", "#responsable");
    expect(links[0]).toHaveTextContent("1. Responsable del tratamiento");
    expect(
      screen.getByRole("heading", { level: 2, name: "14. Contacto" }).closest("section"),
    ).toHaveAttribute("id", "contacto");
    expect(screen.queryByText("Contenido pendiente")).not.toBeInTheDocument();
  });

  it("shows the controller's details as labelled pairs", () => {
    // Arrange
    const page = legalPageOf("/privacidad");

    // Act
    render(<LegalPage page={page} />);

    // Assert
    const section = screen
      .getByRole("heading", { level: 2, name: "1. Responsable del tratamiento" })
      .closest("section")!;
    const terms = within(section)
      .getAllByRole("term")
      .map((term) => term.textContent);
    expect(terms).toEqual([
      "Responsable",
      "Nombre comercial",
      "Sitio web",
      "Correo electrónico de contacto",
      "País de establecimiento",
    ]);
    expect(within(section).getByText("Víctor Lendinez Agudo")).toBeInTheDocument();
  });

  it("turns e-mail and web addresses into links, the web ones in a new tab", () => {
    // Arrange
    const page = legalPageOf("/privacidad");

    // Act
    render(<LegalPage page={page} />);

    // Assert
    for (const link of screen.getAllByRole("link", { name: "privacidad@trainera.es" })) {
      expect(link).toHaveAttribute("href", "mailto:privacidad@trainera.es");
    }
    const aepd = screen.getByRole("link", { name: "www.aepd.es" });
    expect(aepd).toHaveAttribute("href", "https://www.aepd.es");
    expect(aepd).toHaveAttribute("target", "_blank");
    expect(aepd).toHaveAttribute("rel", "noopener noreferrer");
  });

  it("shows marked words in bold and list terms before their text", () => {
    // Arrange
    const page = legalPageOf("/privacidad");

    // Act
    render(<LegalPage page={page} />);

    // Assert
    expect(screen.getByText("consentimiento del interesado").tagName).toBe("STRONG");
    expect(screen.queryByText(/\*\*/)).not.toBeInTheDocument();
    const rights = screen
      .getByRole("heading", { level: 2, name: "7. Derechos de los usuarios" })
      .closest("section")!;
    expect(within(rights).getAllByRole("listitem")[0]).toHaveTextContent(
      "Acceso: conocer qué datos personales se están tratando.",
    );
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
