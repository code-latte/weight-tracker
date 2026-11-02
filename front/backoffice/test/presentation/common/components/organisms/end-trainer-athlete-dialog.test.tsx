import { useState } from "react";
import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { DomainError, fail, ok } from "@weight-tracker/core/domain";
import type {
  DomainErrorCode,
  Result,
  TrainerAthleteRelationship,
} from "@weight-tracker/core/domain";
import EndTrainerAthleteDialog, {
  type EndRelationshipTarget,
} from "@/presentation/common/components/organisms/end-trainer-athlete-dialog";
import renderWithProviders from "../../../../support/render-with-providers";

const target: EndRelationshipTarget = {
  trainerId: "6a2d3f1e-0e6c-4b64-9d8f-1e7a8d2b3c4d",
  athleteId: "7b0c1f7e-3a54-4d4e-9d0e-1a2b3c4d5e6f",
  athleteName: "Luis Pérez",
};

const ended: TrainerAthleteRelationship = {
  id: "9d5a6c4b-3b9f-4e97-8a0b-4b0d1a5e6f70",
  trainerId: target.trainerId,
  trainerName: "Ana",
  trainerSurname: "García",
  athleteId: target.athleteId,
  athleteName: "Luis",
  athleteSurname: "Pérez",
  status: "ENDED",
  startedAtUtc: "2026-10-01T09:00:00Z",
  finishedAtUtc: "2026-11-02T09:00:00Z",
};

type Answer = () => Promise<Result<TrainerAthleteRelationship, DomainError>>;

/** Stands in for a screen: owns the target and closes the dialog the way the screens do. */
function Harness({
  initial,
  onClose,
  onEnded,
}: {
  initial: EndRelationshipTarget;
  onClose: (stale: boolean) => void;
  onEnded: () => void;
}) {
  const [shown, setShown] = useState<EndRelationshipTarget | null>(initial);

  return (
    <EndTrainerAthleteDialog
      target={shown}
      onClose={(stale) => {
        onClose(stale);
        setShown(null);
      }}
      onEnded={() => {
        onEnded();
        setShown(null);
      }}
    />
  );
}

function renderDialog(answer: Answer = async () => ok(ended), initial = target) {
  const execute = vi.fn(answer);
  const onClose = vi.fn();
  const onEnded = vi.fn();

  renderWithProviders(<Harness initial={initial} onClose={onClose} onEnded={onEnded} />, {
    getSessionUseCase: () => ({
      execute: async () => ok({ accessToken: "a", refreshToken: "r" }),
    }),
    endTrainerAthleteUseCase: () => ({ execute }),
  });
  return { execute, onClose, onEnded };
}

const failure =
  (code: DomainErrorCode): Answer =>
  async () =>
    fail(new DomainError(code, "Rejected"));

describe("EndTrainerAthleteDialog", () => {
  it("asks before ending, names the athlete and sends nothing yet", async () => {
    // Arrange & Act
    const { execute } = renderDialog();

    // Assert
    expect(
      await screen.findByRole("dialog", { name: "¿Finalizar la relación con Luis Pérez?" }),
    ).toBeInTheDocument();
    expect(
      screen.getByText("El atleta dejará de estar asignado a este entrenador."),
    ).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Finalizar" })).toHaveClass("MuiButton-colorError");
    expect(screen.getByRole("button", { name: "Cancelar" })).toBeInTheDocument();
    expect(execute).not.toHaveBeenCalled();
  });

  it("speaks to the trainer when they end their own relationship", async () => {
    // Arrange & Act
    renderDialog(async () => ok(ended), { ...target, ownRelationship: true });

    // Assert
    await screen.findByRole("dialog", { name: "¿Finalizar la relación con Luis Pérez?" });
    expect(
      screen.getByText("Dejarás de entrenar a este atleta y ya no lo verás en tu lista."),
    ).toBeInTheDocument();
    expect(
      screen.queryByText("El atleta dejará de estar asignado a este entrenador."),
    ).not.toBeInTheDocument();
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

  it("ends the relationship for the trainer and the athlete and reports it", async () => {
    // Arrange
    const { execute, onEnded } = renderDialog();
    await screen.findByRole("dialog");
    const user = userEvent.setup();

    // Act
    await user.click(screen.getByRole("button", { name: "Finalizar" }));

    // Assert
    await waitFor(() => expect(onEnded).toHaveBeenCalledTimes(1));
    expect(execute).toHaveBeenCalledTimes(1);
    expect(execute).toHaveBeenCalledWith({
      trainerId: target.trainerId,
      athleteId: target.athleteId,
    });
  });

  it.each([
    ["NOT_FOUND", "Esta relación ya no está activa. Recarga la lista."],
    ["CONFLICT", "Esta relación ya no está activa. Recarga la lista."],
    ["NETWORK", "No hay conexión con el servidor."],
  ] as const)("keeps the dialog open with the message for %s", async (code, message) => {
    // Arrange
    const { onEnded } = renderDialog(failure(code));
    await screen.findByRole("dialog");
    const user = userEvent.setup();

    // Act
    await user.click(screen.getByRole("button", { name: "Finalizar" }));

    // Assert
    expect(await screen.findByText(message)).toBeInTheDocument();
    expect(screen.getByRole("dialog")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Finalizar" })).toBeEnabled();
    expect(onEnded).not.toHaveBeenCalled();
  });

  it.each([
    ["NOT_FOUND", true],
    ["CONFLICT", true],
    ["FORBIDDEN", false],
  ] as const)("reports a stale list on close after %s: %s", async (code, stale) => {
    // Arrange
    const { onClose } = renderDialog(failure(code));
    await screen.findByRole("dialog");
    const user = userEvent.setup();
    await user.click(screen.getByRole("button", { name: "Finalizar" }));
    await screen.findByRole("alert");

    // Act
    await user.click(screen.getByRole("button", { name: "Cancelar" }));

    // Assert
    expect(onClose).toHaveBeenCalledWith(stale);
  });
});
