import { useState } from "react";
import { act, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { DomainError, fail, ok } from "@weight-tracker/core/domain";
import type {
  CreatedAthleteInvitation,
  InviteAthleteRequest,
  Result,
} from "@weight-tracker/core/domain";
import i18n from "@/infrastructure/i18n";
import InviteAthleteDialog from "@/presentation/invitations/components/organisms/invite-athlete-dialog";
import renderWithProviders from "../../../../support/render-with-providers";

const created: CreatedAthleteInvitation = {
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
  token: "raw-token",
};

/** Stands in for the invitations screen: owns the open state and can open the dialog again. */
function Harness({ onInvited }: { onInvited: () => void }) {
  const [open, setOpen] = useState(true);

  return (
    <>
      <button type="button" onClick={() => setOpen(true)}>
        Abrir
      </button>
      <InviteAthleteDialog open={open} onClose={() => setOpen(false)} onInvited={onInvited} />
    </>
  );
}

function renderDialog({
  invite = async () => ok(created),
}: {
  invite?: (
    request: InviteAthleteRequest,
  ) => Promise<Result<CreatedAthleteInvitation, DomainError>>;
} = {}) {
  const execute = vi.fn(invite);
  const onInvited = vi.fn();

  renderWithProviders(<Harness onInvited={onInvited} />, {
    getSessionUseCase: () => ({
      execute: async () => ok({ accessToken: "a", refreshToken: "r" }),
    }),
    inviteAthleteUseCase: () => ({ execute }),
  });
  return { execute, onInvited };
}

const openedDialog = () => screen.findByRole("dialog", { name: "Invitar atleta" });
const emailField = () => screen.getByRole("textbox", { name: "Email" });
const submit = () => screen.getByRole("button", { name: "Enviar invitación" });

describe("InviteAthleteDialog", () => {
  it("asks for an email with an empty field", async () => {
    // Arrange & Act
    renderDialog();

    // Assert
    await openedDialog();
    expect(emailField()).toHaveValue("");
    expect(screen.getByRole("button", { name: "Cancelar" })).toBeInTheDocument();
    expect(submit()).toBeInTheDocument();
  });

  it("reports an empty email and sends nothing", async () => {
    // Arrange
    const { execute } = renderDialog();
    await openedDialog();
    const user = userEvent.setup();

    // Act
    await user.click(submit());

    // Assert
    expect(await screen.findByText("Este campo es obligatorio.")).toBeInTheDocument();
    expect(emailField()).toHaveAccessibleDescription("Este campo es obligatorio.");
    expect(execute).not.toHaveBeenCalled();
  });

  it("reports a malformed email and sends nothing", async () => {
    // Arrange
    const { execute } = renderDialog();
    await openedDialog();
    const user = userEvent.setup();
    await user.type(emailField(), "luis.example.com");

    // Act
    await user.click(submit());

    // Assert
    expect(await screen.findByText("Introduce un email válido.")).toBeInTheDocument();
    expect(execute).not.toHaveBeenCalled();
  });

  it("sends the email trimmed", async () => {
    // Arrange
    const { execute } = renderDialog();
    await openedDialog();
    const user = userEvent.setup();
    await user.type(emailField(), "  luis@example.com  ");

    // Act
    await user.click(submit());

    // Assert
    await waitFor(() => expect(execute).toHaveBeenCalledWith({ email: "luis@example.com" }));
  });

  it("disables Enviar invitación while the invitation is being sent", async () => {
    // Arrange
    renderDialog({ invite: () => new Promise(() => {}) });
    await openedDialog();
    const user = userEvent.setup();
    await user.type(emailField(), "luis@example.com");

    // Act
    await user.click(submit());

    // Assert
    expect(await screen.findByRole("button", { name: "Enviar invitación" })).toBeDisabled();
  });

  it("reports the invitation and closes", async () => {
    // Arrange
    const { onInvited } = renderDialog();
    await openedDialog();
    const user = userEvent.setup();
    await user.type(emailField(), "luis@example.com");

    // Act
    await user.click(submit());

    // Assert
    await waitFor(() => expect(onInvited).toHaveBeenCalledTimes(1));
    await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
  });

  it.each([
    [
      "CONFLICT",
      "No se puede invitar a este email: ya tiene una invitación pendiente, ya entrena con alguien o no es una cuenta de atleta.",
    ],
    ["VALIDATION", "Introduce un email válido."],
    ["FORBIDDEN", "No tienes permiso para realizar esta acción."],
  ] as const)("keeps the dialog and the email when the API answers %s", async (code, message) => {
    // Arrange
    const { onInvited } = renderDialog({ invite: async () => fail(new DomainError(code, "")) });
    await openedDialog();
    const user = userEvent.setup();
    await user.type(emailField(), "luis@example.com");

    // Act
    await user.click(submit());

    // Assert
    expect(await screen.findByText(message)).toBeInTheDocument();
    expect(screen.getByRole("dialog", { name: "Invitar atleta" })).toBeInTheDocument();
    expect(emailField()).toHaveValue("luis@example.com");
    expect(onInvited).not.toHaveBeenCalled();
  });

  it("closes on Cancelar without a request and opens again empty", async () => {
    // Arrange
    const { execute } = renderDialog();
    await openedDialog();
    const user = userEvent.setup();
    await user.type(emailField(), "luis@example.com");
    await user.click(screen.getByRole("button", { name: "Cancelar" }));
    await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());

    // Act
    await user.click(screen.getByRole("button", { name: "Abrir" }));

    // Assert
    await openedDialog();
    expect(emailField()).toHaveValue("");
    expect(execute).not.toHaveBeenCalled();
  });

  it("follows the active language", async () => {
    // Arrange
    renderDialog();
    await openedDialog();

    // Act
    await act(() => i18n.changeLanguage("en"));

    // Assert
    expect(screen.getByRole("dialog", { name: "Invite athlete" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Send invitation" })).toBeInTheDocument();
  });
});
