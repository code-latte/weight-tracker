import { useState } from "react";
import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { DomainError, fail, ok } from "@weight-tracker/core/domain";
import type { DomainErrorCode, Result, UserSummary } from "@weight-tracker/core/domain";
import ChangeUserActivationDialog, {
  type ActivationChange,
} from "@/presentation/users/components/organisms/change-user-activation-dialog";
import renderWithProviders from "../../../../support/render-with-providers";

const active: UserSummary = {
  id: "6a2d3f1e-0e6c-4b64-9d8f-1e7a8d2b3c4d",
  email: "ana@example.com",
  name: "Ana",
  surname: "García",
  role: "TRAINER",
  isActive: true,
  deactivatedAtUtc: null,
  trainer: null,
};

const inactive: UserSummary = {
  ...active,
  id: "7b0c1f7e-3a54-4d4e-9d0e-1a2b3c4d5e6f",
  name: "",
  surname: "",
  isActive: false,
  deactivatedAtUtc: "2026-10-20T09:30:00Z",
  trainer: null,
};

type Answer = () => Promise<Result<UserSummary, DomainError>>;

/** Stands in for the users screen: owns the target user and closes the dialog the way the screen does. */
function Harness({
  target,
  onClose,
  onChanged,
}: {
  target: UserSummary;
  onClose: (stale: boolean) => void;
  onChanged: (change: ActivationChange) => void;
}) {
  const [user, setUser] = useState<UserSummary | null>(target);

  return (
    <ChangeUserActivationDialog
      user={user}
      onClose={(stale) => {
        onClose(stale);
        setUser(null);
      }}
      onChanged={(change) => {
        onChanged(change);
        setUser(null);
      }}
    />
  );
}

function renderDialog(
  target: UserSummary,
  {
    deactivate = async () => ok({ ...active, isActive: false }),
    activate = async () => ok({ ...inactive, isActive: true }),
  }: { deactivate?: Answer; activate?: Answer } = {},
) {
  const deactivateExecute = vi.fn(deactivate);
  const activateExecute = vi.fn(activate);
  const onClose = vi.fn();
  const onChanged = vi.fn();

  renderWithProviders(<Harness target={target} onClose={onClose} onChanged={onChanged} />, {
    getSessionUseCase: () => ({
      execute: async () => ok({ accessToken: "a", refreshToken: "r" }),
    }),
    deactivateUserUseCase: () => ({ execute: deactivateExecute }),
    activateUserUseCase: () => ({ execute: activateExecute }),
  });
  return { deactivateExecute, activateExecute, onClose, onChanged };
}

const failure =
  (code: DomainErrorCode): Answer =>
  async () =>
    fail(new DomainError(code, "Rejected"));

describe("ChangeUserActivationDialog", () => {
  it("asks before deactivating, names the user and sends nothing yet", async () => {
    // Arrange & Act
    const { deactivateExecute, activateExecute } = renderDialog(active);

    // Assert
    expect(
      await screen.findByRole("dialog", { name: "¿Desactivar a Ana García?" }),
    ).toBeInTheDocument();
    expect(
      screen.getByText(
        "No podrá iniciar sesión ni recibir nuevos entrenamientos. Sus datos se conservan y podrás reactivarlo cuando quieras.",
      ),
    ).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Desactivar" })).toHaveClass("MuiButton-colorError");
    expect(screen.getByRole("button", { name: "Cancelar" })).toBeInTheDocument();
    expect(deactivateExecute).not.toHaveBeenCalled();
    expect(activateExecute).not.toHaveBeenCalled();
  });

  it("asks before activating, with the email when the user has no name", async () => {
    // Arrange & Act
    renderDialog(inactive);

    // Assert
    expect(
      await screen.findByRole("dialog", { name: "¿Activar a ana@example.com?" }),
    ).toBeInTheDocument();
    expect(
      screen.getByText("Podrá volver a iniciar sesión y recibir entrenamientos."),
    ).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Activar" })).toHaveClass("MuiButton-colorPrimary");
  });

  it("closes on Cancelar without a request", async () => {
    // Arrange
    const { deactivateExecute, onClose } = renderDialog(active);
    await screen.findByRole("dialog");
    const user = userEvent.setup();

    // Act
    await user.click(screen.getByRole("button", { name: "Cancelar" }));

    // Assert
    await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
    expect(onClose).toHaveBeenCalledWith(false);
    expect(deactivateExecute).not.toHaveBeenCalled();
  });

  it("deactivates an active user by its id and reports it", async () => {
    // Arrange
    const { deactivateExecute, activateExecute, onChanged } = renderDialog(active);
    await screen.findByRole("dialog");
    const user = userEvent.setup();

    // Act
    await user.click(screen.getByRole("button", { name: "Desactivar" }));

    // Assert
    await waitFor(() => expect(onChanged).toHaveBeenCalledWith("deactivated"));
    expect(deactivateExecute).toHaveBeenCalledWith(active.id);
    expect(activateExecute).not.toHaveBeenCalled();
  });

  it("activates an inactive user by its id and reports it", async () => {
    // Arrange
    const { deactivateExecute, activateExecute, onChanged } = renderDialog(inactive);
    await screen.findByRole("dialog");
    const user = userEvent.setup();

    // Act
    await user.click(screen.getByRole("button", { name: "Activar" }));

    // Assert
    await waitFor(() => expect(onChanged).toHaveBeenCalledWith("activated"));
    expect(activateExecute).toHaveBeenCalledWith(inactive.id);
    expect(deactivateExecute).not.toHaveBeenCalled();
  });

  it.each([
    ["CONFLICT", "El usuario ya estaba en ese estado. Recarga la lista."],
    ["NOT_FOUND", "Este usuario ya no existe."],
    ["FORBIDDEN", "No tienes permiso para realizar esta acción."],
  ] as const)("keeps the dialog open with the message for %s", async (code, message) => {
    // Arrange
    const { onChanged } = renderDialog(active, { deactivate: failure(code) });
    await screen.findByRole("dialog");
    const user = userEvent.setup();

    // Act
    await user.click(screen.getByRole("button", { name: "Desactivar" }));

    // Assert
    expect(await screen.findByText(message)).toBeInTheDocument();
    expect(screen.getByRole("dialog")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Desactivar" })).toBeEnabled();
    expect(onChanged).not.toHaveBeenCalled();
  });

  it.each([
    ["CONFLICT", true],
    ["NOT_FOUND", true],
    ["FORBIDDEN", false],
  ] as const)("reports a stale list on close after %s: %s", async (code, stale) => {
    // Arrange
    const { onClose } = renderDialog(active, { deactivate: failure(code) });
    await screen.findByRole("dialog");
    const user = userEvent.setup();
    await user.click(screen.getByRole("button", { name: "Desactivar" }));
    await screen.findByRole("alert");

    // Act
    await user.click(screen.getByRole("button", { name: "Cancelar" }));

    // Assert
    expect(onClose).toHaveBeenCalledWith(stale);
  });

  it("reports a stale list when closed with Escape after a conflict", async () => {
    // Arrange
    const { onClose } = renderDialog(active, { deactivate: failure("CONFLICT") });
    await screen.findByRole("dialog");
    const user = userEvent.setup();
    await user.click(screen.getByRole("button", { name: "Desactivar" }));
    await screen.findByRole("alert");

    // Act
    await user.keyboard("{Escape}");

    // Assert
    expect(onClose).toHaveBeenCalledWith(true);
  });
});
