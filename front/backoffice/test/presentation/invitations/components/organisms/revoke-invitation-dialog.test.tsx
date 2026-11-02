import { useState } from "react";
import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { DomainError, fail, ok } from "@weight-tracker/core/domain";
import type { AthleteInvitation, DomainErrorCode, Result } from "@weight-tracker/core/domain";
import RevokeInvitationDialog from "@/presentation/invitations/components/organisms/revoke-invitation-dialog";
import renderWithProviders from "../../../../support/render-with-providers";

const pending: AthleteInvitation = {
  id: "5e6f7a8b-9c0d-4e1f-8a2b-3c4d5e6f7a8b",
  trainerId: "3c4d5e6f-7a8b-4c9d-8e0f-1a2b3c4d5e6f",
  trainerName: "Ana",
  trainerSurname: "García",
  inviteEmail: "luis@example.com",
  userId: null,
  userName: null,
  userSurname: null,
  status: "PENDING",
  createdAtUtc: "2026-10-28T10:00:00Z",
  expiresAtUtc: "2026-11-04T10:00:00Z",
  acceptedAtUtc: null,
};

type Answer = () => Promise<Result<AthleteInvitation, DomainError>>;

/** Stands in for the invitations screen: owns the target and closes the dialog the way the screen does. */
function Harness({
  onClose,
  onRevoked,
}: {
  onClose: (stale: boolean) => void;
  onRevoked: () => void;
}) {
  const [invitation, setInvitation] = useState<AthleteInvitation | null>(pending);

  return (
    <RevokeInvitationDialog
      invitation={invitation}
      onClose={(stale) => {
        onClose(stale);
        setInvitation(null);
      }}
      onRevoked={() => {
        onRevoked();
        setInvitation(null);
      }}
    />
  );
}

function renderDialog(revoke: Answer = async () => ok({ ...pending, status: "REVOKED" })) {
  const execute = vi.fn(revoke);
  const onClose = vi.fn();
  const onRevoked = vi.fn();

  renderWithProviders(<Harness onClose={onClose} onRevoked={onRevoked} />, {
    getSessionUseCase: () => ({
      execute: async () => ok({ accessToken: "a", refreshToken: "r" }),
    }),
    revokeAthleteInvitationUseCase: () => ({ execute }),
  });
  return { execute, onClose, onRevoked };
}

const failure =
  (code: DomainErrorCode): Answer =>
  async () =>
    fail(new DomainError(code, "Rejected"));
const confirm = () => screen.getByRole("button", { name: "Revocar" });

describe("RevokeInvitationDialog", () => {
  it("asks before revoking, names the email and sends nothing yet", async () => {
    // Arrange & Act
    const { execute } = renderDialog();

    // Assert
    expect(
      await screen.findByRole("dialog", { name: "¿Revocar la invitación a luis@example.com?" }),
    ).toBeInTheDocument();
    expect(
      screen.getByText("El enlace dejará de funcionar y no podrá aceptarse."),
    ).toBeInTheDocument();
    expect(confirm()).toHaveClass("MuiButton-colorError");
    expect(execute).not.toHaveBeenCalled();
  });

  it("closes on Cancelar without a request", async () => {
    // Arrange
    const { execute, onClose } = renderDialog();
    await screen.findByRole("dialog");
    const user = userEvent.setup();

    // Act
    await user.click(screen.getByRole("button", { name: "Cancelar" }));

    // Assert
    await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
    expect(onClose).toHaveBeenCalledWith(false);
    expect(execute).not.toHaveBeenCalled();
  });

  it("revokes the invitation by its id and reports it", async () => {
    // Arrange
    const { execute, onRevoked } = renderDialog();
    await screen.findByRole("dialog");
    const user = userEvent.setup();

    // Act
    await user.click(confirm());

    // Assert
    await waitFor(() => expect(onRevoked).toHaveBeenCalledTimes(1));
    expect(execute).toHaveBeenCalledWith(pending.id);
  });

  it.each([
    ["CONFLICT", "Esta invitación ya no está pendiente. Recarga la lista."],
    ["NOT_FOUND", "Esta invitación ya no existe."],
    ["FORBIDDEN", "No tienes permiso para realizar esta acción."],
  ] as const)("keeps the dialog open with the message for %s", async (code, message) => {
    // Arrange
    const { onRevoked } = renderDialog(failure(code));
    await screen.findByRole("dialog");
    const user = userEvent.setup();

    // Act
    await user.click(confirm());

    // Assert
    expect(await screen.findByText(message)).toBeInTheDocument();
    expect(screen.getByRole("dialog")).toBeInTheDocument();
    expect(confirm()).toBeEnabled();
    expect(onRevoked).not.toHaveBeenCalled();
  });

  it.each([
    ["CONFLICT", true],
    ["NOT_FOUND", true],
    ["FORBIDDEN", false],
  ] as const)("reports a stale list on close after %s: %s", async (code, stale) => {
    // Arrange
    const { onClose } = renderDialog(failure(code));
    await screen.findByRole("dialog");
    const user = userEvent.setup();
    await user.click(confirm());
    await screen.findByRole("alert");

    // Act
    await user.click(screen.getByRole("button", { name: "Cancelar" }));

    // Assert
    expect(onClose).toHaveBeenCalledWith(stale);
  });

  it("reports a stale list when closed with Escape after a conflict", async () => {
    // Arrange
    const { onClose } = renderDialog(failure("CONFLICT"));
    await screen.findByRole("dialog");
    const user = userEvent.setup();
    await user.click(confirm());
    await screen.findByRole("alert");

    // Act
    await user.keyboard("{Escape}");

    // Assert
    expect(onClose).toHaveBeenCalledWith(true);
  });
});
