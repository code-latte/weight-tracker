import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import Accordion from "@/components/accordion";

const items = [
  { question: "¿Qué es Trainera?", answer: "Una plataforma para entrenadores." },
  { question: "¿Para quién está pensada?", answer: "Para entrenadores personales." },
];

describe("Accordion", () => {
  it("starts with every answer hidden", () => {
    // Arrange & Act
    render(<Accordion items={items} />);

    // Assert
    for (const { question } of items) {
      expect(screen.getByRole("button", { name: question })).toHaveAttribute(
        "aria-expanded",
        "false",
      );
    }
    expect(screen.queryByText("Una plataforma para entrenadores.")).not.toBeVisible();
  });

  it("shows an answer when its question is activated with the keyboard", async () => {
    // Arrange
    render(<Accordion items={items} />);
    const user = userEvent.setup();
    await user.tab();

    // Act
    await user.keyboard("{Enter}");

    // Assert
    expect(screen.getByRole("button", { name: "¿Qué es Trainera?" })).toHaveAttribute(
      "aria-expanded",
      "true",
    );
    expect(screen.getByRole("region", { name: "¿Qué es Trainera?" })).toHaveTextContent(
      "Una plataforma para entrenadores.",
    );
  });

  it("hides an open answer when its question is activated again", async () => {
    // Arrange
    render(<Accordion items={items} />);
    const user = userEvent.setup();
    const question = screen.getByRole("button", { name: "¿Qué es Trainera?" });
    await user.click(question);

    // Act
    await user.click(question);

    // Assert
    expect(question).toHaveAttribute("aria-expanded", "false");
  });

  it("keeps an answer open when another one opens", async () => {
    // Arrange
    render(<Accordion items={items} />);
    const user = userEvent.setup();
    await user.click(screen.getByRole("button", { name: "¿Qué es Trainera?" }));

    // Act
    await user.click(screen.getByRole("button", { name: "¿Para quién está pensada?" }));

    // Assert
    expect(screen.getByRole("button", { name: "¿Qué es Trainera?" })).toHaveAttribute(
      "aria-expanded",
      "true",
    );
    expect(screen.getByRole("button", { name: "¿Para quién está pensada?" })).toHaveAttribute(
      "aria-expanded",
      "true",
    );
  });
});
