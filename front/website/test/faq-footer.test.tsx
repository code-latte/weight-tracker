import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import Faq from "@/sections/faq";
import FinalCta from "@/sections/final-cta";
import Footer from "@/sections/footer";

describe("Faq", () => {
  it("lists the seven questions", () => {
    // Arrange & Act
    render(<Faq />);

    // Assert
    expect(screen.getAllByRole("button")).toHaveLength(7);
    expect(screen.getByRole("button", { name: "¿Qué es Trainera?" })).toBeInTheDocument();
  });

  it("answers the price question with plans and no amount", async () => {
    // Arrange
    render(<Faq />);
    const user = userEvent.setup();

    // Act
    await user.click(screen.getByRole("button", { name: "¿Cuánto cuesta Trainera?" }));

    // Assert
    const answer = screen.getByRole("region", { name: "¿Cuánto cuesta Trainera?" });
    expect(answer).toHaveTextContent("planes");
    expect(answer.textContent).not.toMatch(/\d|€|\$/);
  });
});

describe("FinalCta", () => {
  it("leads its call to action to the form", () => {
    // Arrange & Act
    render(<FinalCta />);

    // Assert
    expect(
      screen.getByRole("heading", {
        name: "Tu trabajo es entrenar. Nosotros queremos ayudarte con todo lo demás.",
      }),
    ).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Solicitar acceso anticipado" })).toHaveAttribute(
      "href",
      "#acceso-anticipado",
    );
  });
});

describe("Footer", () => {
  it("links to the four legal pages", () => {
    // Arrange & Act
    render(<Footer />);

    // Assert
    const legal = screen.getByRole("navigation", { name: "Legal" });
    expect(
      within(legal)
        .getAllByRole("link")
        .map((link) => [link.textContent, link.getAttribute("href")]),
    ).toEqual([
      ["Aviso legal", "/aviso-legal"],
      ["Política de privacidad", "/privacidad"],
      ["Política de cookies", "/cookies"],
      ["Condiciones de uso", "/condiciones"],
    ]);
  });

  it("leads Contacto to the form and shows no social profile or email", () => {
    // Arrange & Act
    render(<Footer />);

    // Assert
    expect(screen.getByRole("link", { name: "Contacto" })).toHaveAttribute(
      "href",
      "#acceso-anticipado",
    );
    for (const name of [/instagram/i, /linkedin/i, /tiktok/i]) {
      expect(screen.queryByRole("link", { name })).not.toBeInTheDocument();
    }
    expect(screen.queryByRole("link", { name: /@/ })).not.toBeInTheDocument();
    expect(screen.getByText("© 2026 Trainera. Todos los derechos reservados.")).toBeInTheDocument();
  });
});
