import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { setSink, type TrackedEvent } from "@/analytics/track";
import EarlyAccessForm from "@/sections/early-access-form";
import FoundingTrainers from "@/sections/founding-trainers";

const confirmation =
  "Hemos recibido tu solicitud. Nos pondremos en contacto contigo para conocer mejor tu forma de trabajar y explicarte cómo participar en Trainera.";

/** A fetch that answers every request with `status`, or never answers for `pending`. */
function fakeFetch(status: number | "pending" | "offline") {
  return vi.fn(async () => {
    if (status === "pending") {
      return new Promise<Response>(() => {});
    }
    if (status === "offline") {
      throw new TypeError("Failed to fetch");
    }
    return new Response(null, { status });
  });
}

function renderForm(status: number | "pending" | "offline" = 204) {
  const fetch = fakeFetch(status);
  vi.stubGlobal("fetch", fetch);
  render(<EarlyAccessForm />);
  return { fetch, user: userEvent.setup() };
}

const emailField = () => screen.getByRole("textbox", { name: "Email" });
const consentBox = () => screen.getByRole("checkbox", { name: /política de privacidad/ });
const submitButton = () => screen.getByRole("button", { name: /Quiero participar|Enviando/ });

async function fill(user: ReturnType<typeof userEvent.setup>, email: string, consent = true) {
  await user.type(emailField(), email);
  if (consent) {
    await user.click(consentBox());
  }
}

describe("EarlyAccessForm", () => {
  let events: TrackedEvent[];

  beforeEach(() => {
    events = [];
    setSink((event) => events.push(event));
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    setSink(() => {});
  });

  it("asks only for the email and the privacy consent", () => {
    // Arrange & Act
    renderForm();

    // Assert
    expect(screen.getAllByRole("textbox")).toEqual([emailField()]);
    expect(consentBox()).not.toBeChecked();
    expect(screen.getByRole("link", { name: "política de privacidad" })).toHaveAttribute(
      "href",
      "/privacidad",
    );
    expect(screen.getByRole("button", { name: "Quiero participar" })).toBeEnabled();
  });

  it("sends nothing for an invalid email and says why", async () => {
    // Arrange
    const { fetch, user } = renderForm();
    await fill(user, "ana@");

    // Act
    await user.click(submitButton());

    // Assert
    expect(fetch).not.toHaveBeenCalled();
    expect(emailField()).toHaveAttribute("aria-invalid", "true");
    expect(emailField()).toHaveAccessibleDescription("Introduce un email válido.");
  });

  it("sends nothing for an email longer than the API accepts", async () => {
    // Arrange
    const { fetch, user } = renderForm();
    await fill(user, `${"a".repeat(250)}@example.com`);

    // Act
    await user.click(submitButton());

    // Assert
    expect(fetch).not.toHaveBeenCalled();
    expect(emailField()).toHaveAccessibleDescription("Introduce un email válido.");
  });

  it("sends nothing without the consent and says why", async () => {
    // Arrange
    const { fetch, user } = renderForm();
    await fill(user, "ana@example.com", false);

    // Act
    await user.click(submitButton());

    // Assert
    expect(fetch).not.toHaveBeenCalled();
    expect(consentBox()).toHaveAccessibleDescription("Debes aceptar la política de privacidad.");
  });

  it("sends the trimmed email once, however often it is pressed", async () => {
    // Arrange
    const { fetch, user } = renderForm("pending");
    await fill(user, " ana@example.com ");

    // Act
    await user.dblClick(submitButton());

    // Assert
    expect(fetch).toHaveBeenCalledTimes(1);
    expect(fetch.mock.calls[0]).toEqual([
      "http://localhost:5199/customer-leads",
      expect.objectContaining({
        method: "POST",
        body: JSON.stringify({ email: "ana@example.com" }),
      }),
    ]);
  });

  it("disables the button and shows the loading state while sending", async () => {
    // Arrange
    const { user } = renderForm("pending");
    await fill(user, "ana@example.com");

    // Act
    await user.click(submitButton());

    // Assert
    expect(screen.getByRole("button", { name: "Enviando…" })).toBeDisabled();
    expect(emailField()).toBeDisabled();
  });

  it("replaces the form with the confirmation when the lead is accepted", async () => {
    // Arrange
    const { user } = renderForm(204);
    await fill(user, "ana@example.com");

    // Act
    await user.click(submitButton());

    // Assert
    expect(await screen.findByText(confirmation)).toBeInTheDocument();
    expect(screen.getByText("¡Gracias por tu interés!")).toBeInTheDocument();
    expect(screen.queryByRole("textbox")).not.toBeInTheDocument();
  });

  it("says the email is invalid when the API refuses it, keeping what was typed", async () => {
    // Arrange
    const { user } = renderForm(400);
    await fill(user, "ana@example.com");

    // Act
    await user.click(submitButton());

    // Assert
    expect(await screen.findByRole("alert")).toHaveTextContent("Introduce un email válido.");
    expect(emailField()).toHaveValue("ana@example.com");
    expect(consentBox()).toBeChecked();
  });

  it("says to wait when there were too many requests", async () => {
    // Arrange
    const { user } = renderForm(429);
    await fill(user, "ana@example.com");

    // Act
    await user.click(submitButton());

    // Assert
    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Has enviado demasiadas solicitudes. Inténtalo de nuevo en unos minutos.",
    );
    expect(emailField()).toHaveValue("ana@example.com");
  });

  it("lets the visitor try again when the request gets no answer", async () => {
    // Arrange
    const { user } = renderForm("offline");
    await fill(user, "ana@example.com");

    // Act
    await user.click(submitButton());

    // Assert
    expect(await screen.findByRole("alert")).toHaveTextContent(
      "No hemos podido enviar tu solicitud. Inténtalo de nuevo.",
    );
    expect(screen.getByRole("button", { name: "Quiero participar" })).toBeEnabled();
  });

  it("shows the confirmation and sends nothing when the hidden field is filled", async () => {
    // Arrange
    const { fetch, user } = renderForm(204);
    await fill(user, "bot@example.com");
    const honeypot = document.querySelector<HTMLInputElement>('input[name="company"]')!;
    await user.type(honeypot, "Acme");

    // Act
    await user.click(submitButton());

    // Assert
    expect(await screen.findByText(confirmation)).toBeInTheDocument();
    expect(fetch).not.toHaveBeenCalled();
  });

  it("hides the extra field from assistive technology", () => {
    // Arrange & Act
    renderForm();

    // Assert
    const honeypot = document.querySelector<HTMLInputElement>('input[name="company"]')!;
    expect(honeypot.closest('[aria-hidden="true"]')).not.toBeNull();
    expect(honeypot).toHaveAttribute("tabindex", "-1");
  });

  it("reports the start of the form once and the outcome of the submission", async () => {
    // Arrange
    const { user } = renderForm(204);
    await fill(user, "ana@example.com");
    await user.click(emailField());

    // Act
    await user.click(submitButton());

    // Assert
    await waitFor(() =>
      expect(events.map((event) => event.name)).toEqual(["form_start", "form_submit"]),
    );
    expect(events[1]).toEqual(expect.objectContaining({ outcome: "sent" }));
  });
});

describe("FoundingTrainers", () => {
  it("shows the programme, its limit as a condition and no counter of places", () => {
    // Arrange & Act
    vi.stubGlobal("fetch", fakeFetch(204));
    render(<FoundingTrainers />);

    // Assert
    expect(screen.getByText("PROGRAMA FOUNDING TRAINERS")).toBeInTheDocument();
    expect(
      screen.getByRole("heading", {
        level: 2,
        name: "Ayúdanos a construir la herramienta que los entrenadores necesitan.",
      }),
    ).toBeInTheDocument();
    expect(screen.getByText("Plazas limitadas a 10 entrenadores.")).toBeInTheDocument();
    expect(screen.queryByText(/quedan|restantes|plazas disponibles/i)).not.toBeInTheDocument();
    expect(
      screen.getByText("Acceso anticipado gratuito durante el desarrollo."),
    ).toBeInTheDocument();
    vi.unstubAllGlobals();
  });
});
