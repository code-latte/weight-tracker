import { act, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { DomainError, fail, ok } from "@weight-tracker/core/domain";
import type {
  AthleteInvitation,
  CreatedAthleteInvitation,
  Result,
  Session,
  UserProfile,
} from "@weight-tracker/core/domain";
import i18n from "@/infrastructure/i18n";
import InvitationsView from "@/presentation/invitations/views/invitations-view";
import renderWithProviders, { completeProfile } from "../../../support/render-with-providers";

const replace = vi.fn();
const push = vi.fn();

vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace, push }),
  usePathname: () => "/invitations",
}));

const adminProfile: UserProfile = { ...completeProfile, email: "admin@example.com", role: "ADMIN" };

const invitation = (overrides: Partial<AthleteInvitation>): AthleteInvitation => ({
  id: "5e6f7a8b-9c0d-4e1f-8a2b-3c4d5e6f7a8b",
  trainerId: completeProfile.id,
  trainerName: "Ana",
  trainerSurname: "García",
  inviteEmail: "athlete@example.com",
  userId: null,
  userName: null,
  userSurname: null,
  status: "PENDING",
  createdAtUtc: "2026-10-28T10:00:00Z",
  expiresAtUtc: "2026-11-04T10:00:00Z",
  acceptedAtUtc: null,
  ...overrides,
});

const toLuis = invitation({ id: "i1", inviteEmail: "luis@example.com" });
const toMarta = invitation({ id: "i2", inviteEmail: "marta@example.com", status: "ACCEPTED" });
const toCarla = invitation({ id: "i3", inviteEmail: "carla@example.com", status: "REVOKED" });
const fromJose = invitation({
  id: "i4",
  trainerId: "6a2d3f1e-0e6c-4b64-9d8f-1e7a8d2b3c4d",
  trainerName: "José",
  trainerSurname: "Ruiz",
  inviteEmail: "sara@example.com",
});

const toNuevo = invitation({ id: "i5", inviteEmail: "nuevo@example.com" });

const created: CreatedAthleteInvitation = {
  id: toNuevo.id,
  trainerId: toNuevo.trainerId,
  trainerName: toNuevo.trainerName,
  trainerSurname: toNuevo.trainerSurname,
  inviteEmail: toNuevo.inviteEmail,
  userId: null,
  userName: null,
  userSurname: null,
  status: "PENDING",
  createdAtUtc: toNuevo.createdAtUtc,
  expiresAtUtc: toNuevo.expiresAtUtc,
  token: "raw-token",
};

type List = () => Promise<Result<AthleteInvitation[], DomainError>>;

function renderInvitations({
  profile = completeProfile,
  getInvitations = async () => ok([toLuis, toMarta, toCarla]),
  invite = async () => ok(created),
  revoke = async () => ok({ ...toLuis, status: "REVOKED" }),
  session = async () => ok({ accessToken: "a", refreshToken: "r" }),
}: {
  profile?: UserProfile;
  getInvitations?: List;
  invite?: () => Promise<Result<CreatedAthleteInvitation, DomainError>>;
  revoke?: () => Promise<Result<AthleteInvitation, DomainError>>;
  session?: () => Promise<Result<Session | null, DomainError>>;
} = {}) {
  const execute = vi.fn(getInvitations);
  const inviteExecute = vi.fn(invite);
  const revokeExecute = vi.fn(revoke);

  const rendered = renderWithProviders(<InvitationsView />, {
    getSessionUseCase: () => ({ execute: session }),
    getMyProfileUseCase: () => ({ execute: async () => ok(profile) }),
    getAthleteInvitationsUseCase: () => ({ execute }),
    inviteAthleteUseCase: () => ({ execute: inviteExecute }),
    revokeAthleteInvitationUseCase: () => ({ execute: revokeExecute }),
  });
  return { execute, inviteExecute, revokeExecute, rendered };
}

const renderAsAdmin = (getInvitations: List = async () => ok([toLuis, toCarla, fromJose])) =>
  renderInvitations({ profile: adminProfile, getInvitations });

function bodyRows() {
  const [, ...rows] = screen.getAllByRole("row");
  return rows;
}

const cellsOf = (row: HTMLElement) =>
  within(row)
    .getAllByRole("cell")
    .map((cell) => cell.textContent);

/** The invited email column: the first for a trainer, the second for an admin. */
const emailsShown = (column = 0) => bodyRows().map((row) => cellsOf(row)[column]);

describe("InvitationsView", () => {
  beforeEach(() => {
    replace.mockClear();
    push.mockClear();
  });

  describe("as a trainer", () => {
    it("shows the title and one row per invitation in the API's order", async () => {
      // Arrange & Act
      const { execute } = renderInvitations();

      // Assert
      expect(
        await screen.findByRole("heading", { level: 1, name: "Invitaciones" }),
      ).toBeInTheDocument();
      await screen.findByText("luis@example.com");
      expect(emailsShown()).toEqual(["luis@example.com", "marta@example.com", "carla@example.com"]);
      expect(execute).toHaveBeenCalledTimes(1);
    });

    it("shows the email, the status and both dates, and no trainer column", async () => {
      // Arrange & Act
      renderInvitations();

      // Assert
      await screen.findByText("luis@example.com");
      for (const header of ["Email", "Estado", "Enviada", "Caduca"]) {
        expect(screen.getByRole("columnheader", { name: header })).toBeInTheDocument();
      }
      expect(screen.queryByRole("columnheader", { name: "Entrenador" })).not.toBeInTheDocument();
      expect(cellsOf(bodyRows()[0]!).slice(0, 4)).toEqual([
        "luis@example.com",
        "Pendiente",
        "28/10/2026",
        "04/11/2026",
      ]);
      expect(cellsOf(bodyRows()[1]!)[1]).toBe("Aceptada");
      expect(cellsOf(bodyRows()[2]!)[1]).toBe("Revocada");
    });

    it("filters by the invited email", async () => {
      // Arrange
      renderInvitations();
      await screen.findByText("luis@example.com");
      const user = userEvent.setup();

      // Act
      await user.type(screen.getByRole("searchbox", { name: "Buscar por email" }), "marta@");

      // Assert
      expect(emailsShown()).toEqual(["marta@example.com"]);
    });

    it("filters by the selected status pill", async () => {
      // Arrange
      renderInvitations();
      await screen.findByText("luis@example.com");
      const user = userEvent.setup();

      // Act
      await user.click(screen.getByRole("button", { name: "Revocada" }));

      // Assert
      expect(emailsShown()).toEqual(["carla@example.com"]);
      expect(screen.getByRole("button", { name: "Revocada" })).toHaveAttribute(
        "aria-pressed",
        "true",
      );
    });

    it("clears the status filter when the selected pill is pressed again", async () => {
      // Arrange
      renderInvitations();
      await screen.findByText("luis@example.com");
      const user = userEvent.setup();
      await user.click(screen.getByRole("button", { name: "Revocada" }));

      // Act
      await user.click(screen.getByRole("button", { name: "Revocada" }));

      // Assert
      expect(emailsShown()).toHaveLength(3);
    });

    it("combines the status pill and the search", async () => {
      // Arrange
      renderInvitations();
      await screen.findByText("luis@example.com");
      const user = userEvent.setup();
      await user.click(screen.getByRole("button", { name: "Pendiente" }));

      // Act
      await user.type(screen.getByRole("searchbox", { name: "Buscar por email" }), "marta@");

      // Assert
      expect(screen.getByText("Ninguna invitación coincide con la búsqueda.")).toBeInTheDocument();
    });

    it("offers Invitar atleta and Revocar only on pending rows", async () => {
      // Arrange & Act
      renderInvitations();

      // Assert
      await screen.findByText("luis@example.com");
      expect(screen.getByRole("button", { name: "Invitar atleta" })).toBeInTheDocument();
      const [luisRow, martaRow, carlaRow] = bodyRows();
      expect(within(luisRow!).getByRole("button", { name: "Revocar" })).toBeInTheDocument();
      expect(within(martaRow!).queryByRole("button", { name: "Revocar" })).not.toBeInTheDocument();
      expect(within(carlaRow!).queryByRole("button", { name: "Revocar" })).not.toBeInTheDocument();
    });

    it("sends an invitation from the modal and reads the invitations again", async () => {
      // Arrange
      const getInvitations = vi
        .fn<List>()
        .mockResolvedValueOnce(ok([toLuis]))
        .mockResolvedValueOnce(ok([toNuevo, toLuis]));
      const { execute, inviteExecute } = renderInvitations({ getInvitations });
      const user = userEvent.setup();
      await screen.findByText("luis@example.com");
      await user.click(screen.getByRole("button", { name: "Invitar atleta" }));
      const dialog = await screen.findByRole("dialog", { name: "Invitar atleta" });
      await user.type(within(dialog).getByRole("textbox", { name: "Email" }), "nuevo@example.com");

      // Act
      await user.click(within(dialog).getByRole("button", { name: "Enviar invitación" }));

      // Assert
      expect(await screen.findByText("Invitación enviada.")).toBeInTheDocument();
      await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
      expect(await screen.findByText("nuevo@example.com")).toBeInTheDocument();
      expect(inviteExecute).toHaveBeenCalledWith({ email: "nuevo@example.com" });
      expect(execute).toHaveBeenCalledTimes(2);
    });

    it("revokes a pending invitation after the confirmation and reads the invitations again", async () => {
      // Arrange
      const getInvitations = vi
        .fn<List>()
        .mockResolvedValueOnce(ok([toLuis]))
        .mockResolvedValueOnce(ok([{ ...toLuis, status: "REVOKED" }]));
      const { execute, revokeExecute } = renderInvitations({ getInvitations });
      const user = userEvent.setup();
      await screen.findByText("luis@example.com");
      await user.click(screen.getByRole("button", { name: "Revocar" }));
      const dialog = await screen.findByRole("dialog", {
        name: "¿Revocar la invitación a luis@example.com?",
      });
      expect(revokeExecute).not.toHaveBeenCalled();

      // Act
      await user.click(within(dialog).getByRole("button", { name: "Revocar" }));

      // Assert
      expect(await screen.findByText("Invitación revocada.")).toBeInTheDocument();
      await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
      expect(revokeExecute).toHaveBeenCalledWith(toLuis.id);
      expect(execute).toHaveBeenCalledTimes(2);
      await waitFor(() => expect(cellsOf(bodyRows()[0]!)[1]).toBe("Revocada"));
    });

    it("reads the invitations again when the revoke dialog closes after a conflict", async () => {
      // Arrange
      const { execute } = renderInvitations({
        getInvitations: async () => ok([toLuis]),
        revoke: async () => fail(new DomainError("CONFLICT", "Not pending")),
      });
      const user = userEvent.setup();
      await screen.findByText("luis@example.com");
      await user.click(screen.getByRole("button", { name: "Revocar" }));
      const dialog = await screen.findByRole("dialog");
      await user.click(within(dialog).getByRole("button", { name: "Revocar" }));
      await within(dialog).findByText("Esta invitación ya no está pendiente. Recarga la lista.");

      // Act
      await user.click(within(dialog).getByRole("button", { name: "Cancelar" }));

      // Assert
      await waitFor(() => expect(execute).toHaveBeenCalledTimes(2));
    });
  });

  describe("as an admin", () => {
    it("shows the trainer's full name in the Entrenador column", async () => {
      // Arrange & Act
      renderAsAdmin();

      // Assert
      await screen.findByText("sara@example.com");
      for (const header of ["Entrenador", "Email del atleta", "Estado", "Enviada", "Caduca"]) {
        expect(screen.getByRole("columnheader", { name: header })).toBeInTheDocument();
      }
      expect(cellsOf(bodyRows()[2]!).slice(0, 2)).toEqual(["José Ruiz", "sara@example.com"]);
    });

    it.each([
      ["the trainer", "jose ruiz", ["sara@example.com"]],
      ["the athlete email", "carla@", ["carla@example.com"]],
    ])("filters by %s", async (_field, text, emails) => {
      // Arrange
      renderAsAdmin();
      await screen.findByText("sara@example.com");
      const user = userEvent.setup();

      // Act
      await user.type(
        screen.getByRole("searchbox", { name: "Buscar por entrenador o email" }),
        text,
      );

      // Assert
      expect(emailsShown(1)).toEqual(emails);
    });

    it("filters by the selected status pill", async () => {
      // Arrange
      renderAsAdmin();
      await screen.findByText("sara@example.com");
      const user = userEvent.setup();

      // Act
      await user.click(screen.getByRole("button", { name: "Pendiente" }));

      // Assert
      expect(emailsShown(1)).toEqual(["luis@example.com", "sara@example.com"]);
    });

    it("shows no invite button, revoke action or dialog", async () => {
      // Arrange & Act
      renderAsAdmin();

      // Assert
      await screen.findByText("sara@example.com");
      expect(screen.queryByRole("button", { name: "Invitar atleta" })).not.toBeInTheDocument();
      expect(screen.queryByRole("button", { name: "Revocar" })).not.toBeInTheDocument();
      expect(screen.queryByRole("columnheader", { name: "Acciones" })).not.toBeInTheDocument();
      expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    });
  });

  it("sends a visitor without a session to login and reads no invitations", async () => {
    // Arrange & Act
    const { execute, rendered } = renderInvitations({ session: async () => ok(null) });
    await rendered;

    // Assert
    await waitFor(() => expect(replace).toHaveBeenCalledWith("/login"));
    expect(execute).not.toHaveBeenCalled();
  });

  it("shows the empty message and no table when there are no invitations", async () => {
    // Arrange & Act
    renderInvitations({ getInvitations: async () => ok([]) });

    // Assert
    expect(await screen.findByText("Todavía no hay invitaciones.")).toBeInTheDocument();
    expect(screen.queryByRole("table")).not.toBeInTheDocument();
  });

  it("shows the skeleton and no table while the invitations load", async () => {
    // Arrange & Act
    renderInvitations({ getInvitations: () => new Promise(() => {}) });

    // Assert
    expect(await screen.findByLabelText("Cargando")).toBeInTheDocument();
    expect(screen.queryByRole("table")).not.toBeInTheDocument();
  });

  it("shows the error and reads the invitations again when Reintentar is pressed", async () => {
    // Arrange
    const getInvitations = vi
      .fn<List>()
      .mockResolvedValueOnce(fail(new DomainError("NETWORK", "")))
      .mockResolvedValueOnce(ok([toLuis]));
    const { execute } = renderInvitations({ getInvitations });
    const user = userEvent.setup();
    await screen.findByText("No hay conexión con el servidor.");

    // Act
    await user.click(screen.getByRole("button", { name: "Reintentar" }));

    // Assert
    expect(await screen.findByText("luis@example.com")).toBeInTheDocument();
    expect(execute).toHaveBeenCalledTimes(2);
  });

  it("follows the active language", async () => {
    // Arrange
    renderInvitations();
    await screen.findByText("luis@example.com");

    // Act
    await act(() => i18n.changeLanguage("en"));

    // Assert
    expect(screen.getByRole("heading", { level: 1, name: "Invitations" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Invite athlete" })).toBeInTheDocument();
    for (const pill of ["Pending", "Accepted", "Rejected", "Revoked"]) {
      expect(screen.getByRole("button", { name: pill })).toBeInTheDocument();
    }
  });
});
