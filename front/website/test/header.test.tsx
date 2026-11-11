import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it } from "vitest";
import { setSink, type TrackedEvent } from "@/analytics/track";
import Header from "@/sections/header";

/** The header's own navigation, as on wide screens; the mobile menu is a second one. */
const desktopNav = () => screen.getAllByRole("navigation", { name: "Menú principal" })[0];

describe("Header", () => {
  afterEach(() => {
    setSink(() => {});
  });

  it("links to each section and to the early-access form", () => {
    // Arrange & Act
    render(<Header />);

    // Assert
    const links = within(desktopNav()).getAllByRole("link");
    expect(links.map((link) => [link.textContent, link.getAttribute("href")])).toEqual([
      ["Funcionalidades", "#funcionalidades"],
      ["Cómo funciona", "#como-funciona"],
      ["Para entrenadores", "#para-entrenadores"],
      ["FAQ", "#faq"],
    ]);
    expect(screen.getAllByRole("link", { name: "Acceso anticipado" })[0]).toHaveAttribute(
      "href",
      "#acceso-anticipado",
    );
    expect(screen.getByRole("link", { name: "Trainera, ir al inicio" })).toHaveAttribute(
      "href",
      "#inicio",
    );
  });

  it("reports a click on the early-access button from the header", async () => {
    // Arrange
    const events: TrackedEvent[] = [];
    setSink((event) => events.push(event));
    render(<Header />);
    const user = userEvent.setup();

    // Act
    await user.click(screen.getAllByRole("link", { name: "Acceso anticipado" })[0]);

    // Assert
    expect(events).toEqual([expect.objectContaining({ name: "cta_click", location: "header" })]);
  });

  it("opens the menu with its links", async () => {
    // Arrange
    render(<Header />);
    const user = userEvent.setup();

    // Act
    await user.click(screen.getByRole("button", { name: "Abrir menú" }));

    // Assert
    const menu = document.getElementById("menu-movil")!;
    expect(menu).toBeVisible();
    expect(within(menu).getByRole("link", { name: "FAQ" })).toHaveAttribute("href", "#faq");
    expect(screen.getByRole("button", { name: "Cerrar menú" })).toHaveAttribute(
      "aria-expanded",
      "true",
    );
  });

  it("closes the menu when a link in it is followed", async () => {
    // Arrange
    render(<Header />);
    const user = userEvent.setup();
    await user.click(screen.getByRole("button", { name: "Abrir menú" }));
    const menu = document.getElementById("menu-movil")!;

    // Act
    await user.click(within(menu).getByRole("link", { name: "FAQ" }));

    // Assert
    expect(menu).not.toBeVisible();
    expect(screen.getByRole("button", { name: "Abrir menú" })).toHaveAttribute(
      "aria-expanded",
      "false",
    );
  });

  it("closes the menu with Escape and returns focus to its button", async () => {
    // Arrange
    render(<Header />);
    const user = userEvent.setup();
    await user.click(screen.getByRole("button", { name: "Abrir menú" }));

    // Act
    await user.keyboard("{Escape}");

    // Assert
    expect(document.getElementById("menu-movil")).not.toBeVisible();
    expect(screen.getByRole("button", { name: "Abrir menú" })).toHaveFocus();
  });
});
