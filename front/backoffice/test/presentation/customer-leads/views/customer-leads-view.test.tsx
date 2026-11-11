import { act, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { DomainError, fail, ok } from "@weight-tracker/core/domain";
import type { CustomerLead, Result, Session, UserProfile } from "@weight-tracker/core/domain";
import i18n from "@/infrastructure/i18n";
import CustomerLeadsView from "@/presentation/customer-leads/views/customer-leads-view";
import renderWithProviders, { completeProfile } from "../../../support/render-with-providers";

const replace = vi.fn();
const push = vi.fn();

vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace, push }),
  usePathname: () => "/customer-leads",
}));

const adminProfile: UserProfile = { ...completeProfile, email: "admin@example.com", role: "ADMIN" };

const newest: CustomerLead = { email: "ana@example.com", registeredAtUtc: "2026-11-10T08:15:00Z" };
const oldest: CustomerLead = { email: "luis@example.com", registeredAtUtc: "2026-11-09T17:40:00Z" };

type LeadsAnswer = () => Promise<Result<CustomerLead[], DomainError>>;

/** The registration time as the screen shows it, in the given language and this machine's time zone. */
function shownDate(value: string, language = "es") {
  return new Intl.DateTimeFormat(language, {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  }).format(new Date(value));
}

function renderLeads({
  profile = adminProfile,
  getLeads = async () => ok([newest, oldest]),
  session = async () => ok({ accessToken: "a", refreshToken: "r" }),
}: {
  profile?: UserProfile;
  getLeads?: LeadsAnswer;
  session?: () => Promise<Result<Session | null, DomainError>>;
} = {}) {
  const execute = vi.fn(getLeads);

  const rendered = renderWithProviders(<CustomerLeadsView />, {
    getSessionUseCase: () => ({ execute: session }),
    getMyProfileUseCase: () => ({ execute: async () => ok(profile) }),
    getCustomerLeadsUseCase: () => ({ execute }),
  });
  return { execute, rendered };
}

function bodyRows() {
  const [, ...rows] = screen.getAllByRole("row");
  return rows;
}

const cellsOf = (row: HTMLElement) =>
  within(row)
    .getAllByRole("cell")
    .map((cell) => cell.textContent);

describe("CustomerLeadsView", () => {
  beforeEach(() => {
    replace.mockClear();
    push.mockClear();
  });

  it("shows the title and every lead in the API's order", async () => {
    // Arrange & Act
    const { execute } = renderLeads();

    // Assert
    expect(await screen.findByRole("heading", { level: 1, name: "Leads" })).toBeInTheDocument();
    await screen.findByText("ana@example.com");
    expect(bodyRows().map((row) => cellsOf(row)[0])).toEqual([
      "ana@example.com",
      "luis@example.com",
    ]);
    expect(execute).toHaveBeenCalledTimes(1);
  });

  it("shows each lead's email and registration date and time", async () => {
    // Arrange & Act
    renderLeads();

    // Assert
    await screen.findByText("ana@example.com");
    expect(screen.getByRole("columnheader", { name: "Email" })).toBeInTheDocument();
    expect(screen.getByRole("columnheader", { name: "Fecha de registro" })).toBeInTheDocument();
    expect(cellsOf(bodyRows()[0]!)).toEqual(["ana@example.com", shownDate("2026-11-10T08:15:00Z")]);
  });

  it("sends a visitor without a session to login and reads no leads", async () => {
    // Arrange & Act
    const { execute, rendered } = renderLeads({ session: async () => ok(null) });
    await rendered;

    // Assert
    await waitFor(() => expect(replace).toHaveBeenCalledWith("/login"));
    expect(execute).not.toHaveBeenCalled();
  });

  it("shows the skeleton and no table while the leads load", async () => {
    // Arrange & Act
    renderLeads({ getLeads: () => new Promise(() => {}) });

    // Assert
    expect(await screen.findByLabelText("Cargando")).toBeInTheDocument();
    expect(screen.queryByRole("table")).not.toBeInTheDocument();
  });

  it("shows the empty message when the API returns no leads", async () => {
    // Arrange & Act
    renderLeads({ getLeads: async () => ok([]) });

    // Assert
    expect(await screen.findByText("Todavía no hay leads.")).toBeInTheDocument();
    expect(screen.queryByRole("table")).not.toBeInTheDocument();
  });

  it("shows the error and reads the leads again when Reintentar is pressed", async () => {
    // Arrange
    const getLeads = vi
      .fn<LeadsAnswer>()
      .mockResolvedValueOnce(fail(new DomainError("NETWORK", "")))
      .mockResolvedValueOnce(ok([newest]));
    const { execute } = renderLeads({ getLeads });
    const actor = userEvent.setup();
    await screen.findByText("No hay conexión con el servidor.");

    // Act
    await actor.click(screen.getByRole("button", { name: "Reintentar" }));

    // Assert
    expect(await screen.findByText("ana@example.com")).toBeInTheDocument();
    expect(execute).toHaveBeenCalledTimes(2);
  });

  it("shows the forbidden error and no lead to a trainer who opens the URL directly", async () => {
    // Arrange & Act
    renderLeads({
      profile: completeProfile,
      getLeads: async () => fail(new DomainError("FORBIDDEN", "Admins only")),
    });

    // Assert
    expect(
      await screen.findByText("No tienes permiso para realizar esta acción."),
    ).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Reintentar" })).toBeInTheDocument();
    expect(screen.queryByRole("table")).not.toBeInTheDocument();
    expect(screen.queryByText("ana@example.com")).not.toBeInTheDocument();
  });

  it("follows the active language", async () => {
    // Arrange
    renderLeads();
    await screen.findByText("ana@example.com");

    // Act
    await act(() => i18n.changeLanguage("en"));

    // Assert
    expect(screen.getByRole("heading", { level: 1, name: "Leads" })).toBeInTheDocument();
    for (const header of ["Email", "Registered on"]) {
      expect(screen.getByRole("columnheader", { name: header })).toBeInTheDocument();
    }
    expect(screen.getByText(shownDate("2026-11-10T08:15:00Z", "en"))).toBeInTheDocument();
  });
});
