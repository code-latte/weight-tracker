import { useState } from "react";
import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { DomainError, fail, ok } from "@weight-tracker/core/domain";
import type {
  DomainErrorCode,
  Result,
  TrainerAthleteInput,
  TrainerAthleteRelationship,
  UserSummary,
} from "@weight-tracker/core/domain";
import AssignTrainerDialog from "@/presentation/trainer-assignments/components/organisms/assign-trainer-dialog";
import renderWithProviders from "../../../../support/render-with-providers";

const user = (overrides: Partial<UserSummary>): UserSummary => ({
  id: "1b2c3d4e-5f6a-4b7c-8d9e-0f1a2b3c4d5e",
  email: "someone@example.com",
  name: "",
  surname: "",
  role: "USER",
  isActive: true,
  deactivatedAtUtc: null,
  trainer: null,
  ...overrides,
});

const athlete = user({
  id: "athlete-1",
  email: "luis@example.com",
  name: "Luis",
  surname: "Pérez",
});
const ana = user({
  id: "trainer-1",
  email: "ana@example.com",
  name: "Ana",
  surname: "García",
  role: "TRAINER",
});
const pablo = user({
  id: "trainer-2",
  email: "pablo@example.com",
  name: "Pablo",
  surname: "Ruiz",
  role: "TRAINER",
});

const linked: TrainerAthleteRelationship = {
  id: "9d5a6c4b-3b9f-4e97-8a0b-4b0d1a5e6f70",
  trainerId: ana.id,
  trainerName: "Ana",
  trainerSurname: "García",
  athleteId: athlete.id,
  athleteName: "Luis",
  athleteSurname: "Pérez",
  status: "ACTIVE",
  startedAtUtc: "2026-11-02T09:00:00Z",
  finishedAtUtc: null,
};

type Answer = (
  input: TrainerAthleteInput,
) => Promise<Result<TrainerAthleteRelationship, DomainError>>;

/** Stands in for the assignment screen: owns the athlete and closes the dialog the way the screen does. */
function Harness({
  onClose,
  onAssigned,
}: {
  onClose: (stale: boolean) => void;
  onAssigned: () => void;
}) {
  const [shown, setShown] = useState<UserSummary | null>(athlete);

  return (
    <AssignTrainerDialog
      athlete={shown}
      trainers={[ana, pablo]}
      onClose={(stale) => {
        onClose(stale);
        setShown(null);
      }}
      onAssigned={() => {
        onAssigned();
        setShown(null);
      }}
    />
  );
}

function renderDialog(answer: Answer = async () => ok(linked)) {
  const execute = vi.fn(answer);
  const onClose = vi.fn();
  const onAssigned = vi.fn();

  renderWithProviders(<Harness onClose={onClose} onAssigned={onAssigned} />, {
    getSessionUseCase: () => ({
      execute: async () => ok({ accessToken: "a", refreshToken: "r" }),
    }),
    linkAthleteToTrainerUseCase: () => ({ execute }),
  });
  return { execute, onClose, onAssigned };
}

const failure =
  (code: DomainErrorCode): Answer =>
  async () =>
    fail(new DomainError(code, "Rejected"));

const openedDialog = () => screen.findByRole("dialog", { name: "Asignar entrenador a Luis Pérez" });
const trainerField = () => screen.getByRole("combobox", { name: "Entrenador" });

async function chooseAna(actor: ReturnType<typeof userEvent.setup>) {
  await actor.click(trainerField());
  await actor.click(await screen.findByRole("option", { name: "Ana García · ana@example.com" }));
}

describe("AssignTrainerDialog", () => {
  it("names the athlete and offers exactly the given trainers", async () => {
    // Arrange
    renderDialog();
    await openedDialog();
    const actor = userEvent.setup();

    // Act
    await actor.click(trainerField());

    // Assert
    const options = await screen.findAllByRole("option");
    expect(options.map((option) => option.textContent)).toEqual([
      "Ana García · ana@example.com",
      "Pablo Ruiz · pablo@example.com",
    ]);
  });

  it.each([
    ["the name ignoring accents", "garcia"],
    ["the email", "ana@"],
  ])("filters the trainers by %s", async (_field, text) => {
    // Arrange
    renderDialog();
    await openedDialog();
    const actor = userEvent.setup();

    // Act
    await actor.type(trainerField(), text);

    // Assert
    const options = await screen.findAllByRole("option");
    expect(options.map((option) => option.textContent)).toEqual(["Ana García · ana@example.com"]);
  });

  it("says when no trainer matches", async () => {
    // Arrange
    renderDialog();
    await openedDialog();
    const actor = userEvent.setup();

    // Act
    await actor.type(trainerField(), "zzz");

    // Assert
    expect(await screen.findByText("Ningún entrenador coincide.")).toBeInTheDocument();
  });

  it("asks for a trainer and sends nothing when none is chosen", async () => {
    // Arrange
    const { execute } = renderDialog();
    await openedDialog();
    const actor = userEvent.setup();

    // Act
    await actor.click(screen.getByRole("button", { name: "Asignar" }));

    // Assert
    expect(await screen.findByText("Selecciona un entrenador.")).toBeInTheDocument();
    expect(execute).not.toHaveBeenCalled();
  });

  it("links the athlete to the chosen trainer once and reports it", async () => {
    // Arrange
    const { execute, onAssigned } = renderDialog();
    await openedDialog();
    const actor = userEvent.setup();
    await chooseAna(actor);

    // Act
    await actor.click(screen.getByRole("button", { name: "Asignar" }));

    // Assert
    await waitFor(() => expect(onAssigned).toHaveBeenCalledTimes(1));
    expect(execute).toHaveBeenCalledTimes(1);
    expect(execute).toHaveBeenCalledWith({ trainerId: ana.id, athleteId: athlete.id });
  });

  it.each([
    ["VALIDATION", "Selecciona un entrenador válido."],
    ["NOT_FOUND", "El entrenador o el atleta ya no existe."],
    [
      "CONFLICT",
      "No se puede asignar este entrenador: el atleta ya tiene uno o la relación no es válida.",
    ],
  ] as const)("keeps the dialog open with the trainer chosen after %s", async (code, message) => {
    // Arrange
    const { onAssigned } = renderDialog(failure(code));
    await openedDialog();
    const actor = userEvent.setup();
    await chooseAna(actor);

    // Act
    await actor.click(screen.getByRole("button", { name: "Asignar" }));

    // Assert
    expect(await screen.findByText(message)).toBeInTheDocument();
    expect(screen.getByRole("dialog")).toBeInTheDocument();
    expect(trainerField()).toHaveValue("Ana García · ana@example.com");
    expect(onAssigned).not.toHaveBeenCalled();
  });

  it.each([
    ["CONFLICT", true],
    ["NOT_FOUND", true],
    ["VALIDATION", false],
  ] as const)("reports a stale list on close after %s: %s", async (code, stale) => {
    // Arrange
    const { onClose } = renderDialog(failure(code));
    await openedDialog();
    const actor = userEvent.setup();
    await chooseAna(actor);
    await actor.click(screen.getByRole("button", { name: "Asignar" }));
    await screen.findByRole("alert");

    // Act
    await actor.click(screen.getByRole("button", { name: "Cancelar" }));

    // Assert
    expect(onClose).toHaveBeenCalledWith(stale);
  });

  it("closes on Cancelar without a request", async () => {
    // Arrange
    const { execute, onClose } = renderDialog();
    await openedDialog();
    const actor = userEvent.setup();

    // Act
    await actor.click(screen.getByRole("button", { name: "Cancelar" }));

    // Assert
    await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
    expect(onClose).toHaveBeenCalledWith(false);
    expect(execute).not.toHaveBeenCalled();
  });
});
