import { useState } from "react";
import { act, fireEvent, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { DomainError, fail, ok } from "@weight-tracker/core/domain";
import type {
  CreateWorkoutRequest,
  DomainErrorCode,
  Result,
  TrainerAthlete,
  UserProfile,
  UserSummary,
  Workout,
  WorkoutTemplate,
  CreateWorkoutFromTemplateInput,
  GetWorkoutTemplatesInput,
} from "@weight-tracker/core/domain";
import i18n from "@/infrastructure/i18n";
import CreateWorkoutDialog from "@/presentation/workouts/components/organisms/create-workout-dialog";
import LoadProfile from "../../../../support/load-profile";
import renderWithProviders, { completeProfile } from "../../../../support/render-with-providers";

const push = vi.fn();

vi.mock("next/navigation", () => ({
  useRouter: () => ({ push, replace: vi.fn() }),
  usePathname: () => "/assigned-plans",
}));

const athleteId = "5f1c2f0e-9d5b-4a53-8c7e-0d6f7c1a2b3c";
const trainerId = "6a2d3f1e-0e6c-4b64-9d8f-1e7a8d2b3c4d";
const adminProfile: UserProfile = { ...completeProfile, role: "ADMIN" };

const created: Workout = {
  id: "w-1",
  name: "Pierna",
  description: null,
  workoutDate: "2026-11-02",
  userId: athleteId,
  trainerId: null,
  status: "DRAFT",
  mesocycleId: null,
  mesocycleWeekNumber: null,
  reviewedAt: null,
  reviewedBy: null,
  resultsUpdatedAt: null,
  exercises: [],
};

const beaId = "7b3e4a2f-1f7d-4c75-8e90-2f8b9e3c4d5e";

function athleteOf(id: string, name: string, surname: string, email: string): TrainerAthlete {
  return {
    id: `r-${email}`,
    athleteId: id,
    email,
    name,
    surname,
    trainerName: "Ana",
    trainerSurname: "García",
    startedAtUtc: "2026-01-01T00:00:00Z",
  };
}

function userOf(
  id: string,
  role: string,
  name: string,
  surname: string,
  email: string,
  isActive = true,
): UserSummary {
  return { id, email, name, surname, role, isActive, deactivatedAtUtc: null, trainer: null };
}

const myAthletes = [
  athleteOf(athleteId, "Ana", "García", "ana@example.com"),
  athleteOf(beaId, "Bea", "López", "bea@example.com"),
];

const users = [
  userOf(athleteId, "USER", "Ana", "García", "ana@example.com"),
  userOf("c-1", "USER", "Carla", "Ruiz", "carla@example.com", false),
  userOf(trainerId, "TRAINER", "Luis", "Pérez", "luis@example.com"),
  userOf("m-1", "ADMIN", "Marta", "Gil", "marta@example.com"),
];

function templateOf(id: string, name: string, description: string | null, exercises: number) {
  const exercise = {
    id: `${id}-e`,
    exerciseId: "ex-squat",
    effort: "HIGH",
    perSide: false,
    restMinutes: null,
    sets: [],
    superSeriesId: null,
    setRecords: [],
  };
  return {
    id,
    name,
    description,
    trainerId,
    exercises: Array.from({ length: exercises }, (_, index) => ({
      ...exercise,
      id: `${id}-e${index}`,
    })),
  } satisfies WorkoutTemplate;
}

const templates = [
  templateOf("wt-1", "Pierna A", "Fuerza", 3),
  templateOf("wt-2", "Torso", null, 1),
];

/** Stands in for the workouts screen: owns the open state and can open the dialog again. */
function Harness() {
  const [open, setOpen] = useState(true);

  return (
    <LoadProfile>
      <button type="button" onClick={() => setOpen(true)}>
        Abrir
      </button>
      <CreateWorkoutDialog open={open} onClose={() => setOpen(false)} />
    </LoadProfile>
  );
}

function renderDialog({
  profile = completeProfile,
  createWorkout = async () => ok(created),
  getMyAthletes = async () => ok(myAthletes),
  getUsers = async () => ok(users),
  getTemplates = async () => ok(templates),
  createFromTemplate = async () => ok({ ...created, id: "w-9" }),
}: {
  profile?: UserProfile;
  createWorkout?: (request: CreateWorkoutRequest) => Promise<Result<Workout, DomainError>>;
  getMyAthletes?: () => Promise<Result<TrainerAthlete[], DomainError>>;
  getUsers?: () => Promise<Result<UserSummary[], DomainError>>;
  getTemplates?: (
    input: GetWorkoutTemplatesInput,
  ) => Promise<Result<WorkoutTemplate[], DomainError>>;
  createFromTemplate?: (
    input: CreateWorkoutFromTemplateInput,
  ) => Promise<Result<Workout, DomainError>>;
} = {}) {
  const execute = vi.fn(createWorkout);
  const myAthletesExecute = vi.fn(getMyAthletes);
  const usersExecute = vi.fn(getUsers);
  const templatesExecute = vi.fn(getTemplates);
  const fromTemplateExecute = vi.fn(createFromTemplate);

  return {
    execute,
    getMyAthletes: myAthletesExecute,
    getUsers: usersExecute,
    getTemplates: templatesExecute,
    createFromTemplate: fromTemplateExecute,
    rendered: renderWithProviders(<Harness />, {
      getSessionUseCase: () => ({
        execute: async () => ok({ accessToken: "a", refreshToken: "r" }),
      }),
      getMyProfileUseCase: () => ({ execute: async () => ok(profile) }),
      createWorkoutUseCase: () => ({ execute }),
      getMyAthletesUseCase: () => ({ execute: myAthletesExecute }),
      getUsersUseCase: () => ({ execute: usersExecute }),
      getWorkoutTemplatesUseCase: () => ({ execute: templatesExecute }),
      createWorkoutFromTemplateUseCase: () => ({ execute: fromTemplateExecute }),
    }),
  };
}

/** Waits until the profile and the people are read, so the fields that depend on them are settled. */
async function openedDialog() {
  const dialog = await screen.findByRole("dialog", { name: "Nuevo entrenamiento" });
  await waitFor(() => expect(athleteField()).toBeEnabled());
  return dialog;
}

const athleteField = () => screen.getByRole("combobox", { name: "Atleta" });
const trainerField = () => screen.getByRole("combobox", { name: "Entrenador" });

async function fill(
  user: ReturnType<typeof userEvent.setup>,
  values: Partial<Record<string, string>>,
) {
  for (const [label, value = ""] of Object.entries(values)) {
    const field = screen.getByRole("textbox", { name: label });
    await user.clear(field);
    // Pasting keeps long values fast; the field still receives a real change event.
    await user.click(field);
    await user.paste(value);
  }
}

async function choose(
  user: ReturnType<typeof userEvent.setup>,
  field: () => HTMLElement,
  option: string,
) {
  await user.click(field());
  await user.click(await screen.findByRole("option", { name: option }));
}

const ana = "Ana García · ana@example.com";
const luis = "Luis Pérez · luis@example.com";

/** Fills the name and chooses Ana: everything a trainer needs to create a workout. */
async function fillTrainerForm(user: ReturnType<typeof userEvent.setup>) {
  await fill(user, { Nombre: "  Pierna  " });
  await choose(user, athleteField, ana);
}

const optionNames = () => screen.getAllByRole("option").map((option) => option.textContent);

const weekField = () => screen.getByRole("group", { name: "Semana" });

/** Thursday 29 October 2026, local noon: the current week is 26 Oct – 1 Nov 2026. */
const today = new Date(2026, 9, 29, 12);

describe("CreateWorkoutDialog", () => {
  beforeEach(() => {
    push.mockClear();
    // Only `Date` is faked, so user events and promises keep running.
    vi.useFakeTimers({ toFake: ["Date"] });
    vi.setSystemTime(today);
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it("asks for the workout's data with empty fields and the current week", async () => {
    // Arrange & Act
    await renderDialog().rendered;

    // Assert
    await openedDialog();
    for (const name of ["Nombre", "Descripción"]) {
      expect(screen.getByRole("textbox", { name })).toHaveValue("");
    }
    expect(athleteField()).toHaveValue("");
    expect(screen.queryByRole("textbox", { name: "ID del atleta" })).not.toBeInTheDocument();
    expect(within(weekField()).getByText("Semana del 26 oct – 1 nov")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Cancelar" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Crear" })).toBeInTheDocument();
  });

  it("asks an admin for the trainer", async () => {
    // Arrange & Act
    await renderDialog({ profile: adminProfile }).rendered;

    // Assert
    await openedDialog();
    expect(trainerField()).toHaveValue("");
  });

  it("does not ask a trainer for the trainer", async () => {
    // Arrange & Act
    await renderDialog().rendered;

    // Assert
    await openedDialog();
    expect(screen.queryByRole("combobox", { name: "Entrenador" })).not.toBeInTheDocument();
    expect(screen.queryByRole("textbox", { name: "ID del entrenador" })).not.toBeInTheDocument();
  });

  it("reports every empty required field and sends nothing", async () => {
    // Arrange
    const { execute, rendered } = renderDialog();
    await rendered;
    await openedDialog();
    const user = userEvent.setup();

    // Act
    await user.click(screen.getByRole("button", { name: "Crear" }));

    // Assert
    expect(await screen.findByText("Selecciona un atleta.")).toBeInTheDocument();
    expect(screen.getAllByText("Este campo es obligatorio.")).toHaveLength(1);
    expect(screen.getByRole("textbox", { name: "Nombre" })).toHaveAccessibleDescription(
      "Este campo es obligatorio.",
    );
    expect(athleteField()).toHaveAccessibleDescription("Selecciona un atleta.");
    expect(screen.getByRole("textbox", { name: "Descripción" })).not.toHaveAccessibleDescription();
    expect(within(weekField()).queryByText("Este campo es obligatorio.")).not.toBeInTheDocument();
    expect(execute).not.toHaveBeenCalled();
  });

  it.each([
    {
      case: "a blank name",
      values: { Nombre: "   " },
      label: "Nombre",
      message: "Este campo es obligatorio.",
    },
    {
      case: "a 257-character name",
      values: { Nombre: "a".repeat(257) },
      label: "Nombre",
      message: "No puede superar los 256 caracteres.",
    },
    {
      case: "a 1025-character description",
      values: { Descripción: "a".repeat(1025) },
      label: "Descripción",
      message: "No puede superar los 1024 caracteres.",
    },
  ])("reports $case and sends nothing", async ({ values, label, message }) => {
    // Arrange
    const { execute, rendered } = renderDialog();
    await rendered;
    await openedDialog();
    const user = userEvent.setup();
    await fillTrainerForm(user);
    await fill(user, values);

    // Act
    await user.click(screen.getByRole("button", { name: "Crear" }));

    // Assert
    expect(await screen.findByRole("textbox", { name: label })).toHaveAccessibleDescription(
      message,
    );
    expect(execute).not.toHaveBeenCalled();
  });

  it("reports an athlete id that is not a UUID and sends nothing", async () => {
    // Arrange
    const { execute, rendered } = renderDialog({
      getMyAthletes: async () => ok([athleteOf("ana", "Ana", "García", "ana@example.com")]),
    });
    await rendered;
    await openedDialog();
    const user = userEvent.setup();
    await fillTrainerForm(user);

    // Act
    await user.click(screen.getByRole("button", { name: "Crear" }));

    // Assert
    await waitFor(() =>
      expect(athleteField()).toHaveAccessibleDescription("Introduce un identificador válido."),
    );
    expect(execute).not.toHaveBeenCalled();
  });

  it("asks an admin to choose the trainer and sends nothing", async () => {
    // Arrange
    const { execute, rendered } = renderDialog({ profile: adminProfile });
    await rendered;
    await openedDialog();
    const user = userEvent.setup();
    await fillTrainerForm(user);

    // Act
    await user.click(screen.getByRole("button", { name: "Crear" }));

    // Assert
    await waitFor(() =>
      expect(trainerField()).toHaveAccessibleDescription("Selecciona un entrenador."),
    );
    expect(execute).not.toHaveBeenCalled();
  });

  it("clears the message of a picker when a person is chosen", async () => {
    // Arrange
    await renderDialog().rendered;
    await openedDialog();
    const user = userEvent.setup();
    await user.click(screen.getByRole("button", { name: "Crear" }));
    expect(await screen.findByText("Selecciona un atleta.")).toBeInTheDocument();

    // Act
    await choose(user, athleteField, ana);

    // Assert
    expect(screen.queryByText("Selecciona un atleta.")).not.toBeInTheDocument();
  });

  it("sends a trainer's workout trimmed, without description nor trainer, once", async () => {
    // Arrange
    const { execute, rendered } = renderDialog();
    await rendered;
    await openedDialog();
    const user = userEvent.setup();
    await fillTrainerForm(user);
    await user.click(within(weekField()).getByRole("button", { name: "Semana siguiente" }));

    // Act
    await user.click(screen.getByRole("button", { name: "Crear" }));

    // Assert
    await waitFor(() => expect(execute).toHaveBeenCalledTimes(1));
    expect(execute).toHaveBeenCalledWith({
      name: "Pierna",
      workoutDate: "2026-11-04",
      userId: athleteId,
    });
  });

  it("sends an admin's workout with its description and trainer", async () => {
    // Arrange
    const { execute, rendered } = renderDialog({ profile: adminProfile });
    await rendered;
    await openedDialog();
    const user = userEvent.setup();
    await fillTrainerForm(user);
    await fill(user, { Descripción: "Día 1" });
    await choose(user, trainerField, luis);

    // Act
    await user.click(screen.getByRole("button", { name: "Crear" }));

    // Assert
    await waitFor(() =>
      expect(execute).toHaveBeenCalledWith({
        name: "Pierna",
        description: "Día 1",
        workoutDate: "2026-10-28",
        userId: athleteId,
        trainerId,
      }),
    );
  });

  describe("people", () => {
    it("offers a trainer exactly their athletes, in order, without reading every user", async () => {
      // Arrange
      const { getMyAthletes, getUsers, rendered } = renderDialog();
      await rendered;
      await openedDialog();
      const user = userEvent.setup();

      // Act
      await user.click(athleteField());

      // Assert
      expect(optionNames()).toEqual([ana, "Bea López · bea@example.com"]);
      expect(getMyAthletes).toHaveBeenCalledTimes(1);
      expect(getUsers).not.toHaveBeenCalled();
    });

    it("offers an admin the active athletes and the active trainers of one users request", async () => {
      // Arrange
      const { getMyAthletes, getUsers, rendered } = renderDialog({ profile: adminProfile });
      await rendered;
      await openedDialog();
      const user = userEvent.setup();

      // Act
      await user.click(athleteField());
      const athletes = optionNames();
      await user.click(screen.getByRole("option", { name: ana }));
      await user.click(trainerField());

      // Assert
      expect(athletes).toEqual([ana]);
      expect(optionNames()).toEqual([luis]);
      expect(getUsers).toHaveBeenCalledTimes(1);
      expect(getMyAthletes).not.toHaveBeenCalled();
    });

    it("shows only the email of a person without a name", async () => {
      // Arrange
      await renderDialog({
        getMyAthletes: async () => ok([athleteOf(athleteId, "", "", "new@example.com")]),
      }).rendered;
      await openedDialog();
      const user = userEvent.setup();

      // Act
      await user.click(athleteField());

      // Assert
      expect(optionNames()).toEqual(["new@example.com"]);
    });

    it("filters by name ignoring accents", async () => {
      // Arrange
      await renderDialog().rendered;
      await openedDialog();
      const user = userEvent.setup();

      // Act
      await user.type(athleteField(), "garcia");

      // Assert
      expect(optionNames()).toEqual([ana]);
    });

    it("filters by email", async () => {
      // Arrange
      await renderDialog().rendered;
      await openedDialog();
      const user = userEvent.setup();

      // Act
      await user.type(athleteField(), "bea@");

      // Assert
      expect(optionNames()).toEqual(["Bea López · bea@example.com"]);
    });

    it("says so when no athlete matches", async () => {
      // Arrange
      await renderDialog().rendered;
      await openedDialog();
      const user = userEvent.setup();

      // Act
      await user.type(athleteField(), "zzz");

      // Assert
      expect(await screen.findByText("Ningún atleta coincide.")).toBeInTheDocument();
    });

    it("says so when no trainer matches", async () => {
      // Arrange
      await renderDialog({ profile: adminProfile }).rendered;
      await openedDialog();
      const user = userEvent.setup();

      // Act
      await user.type(trainerField(), "zzz");

      // Assert
      expect(await screen.findByText("Ningún entrenador coincide.")).toBeInTheDocument();
    });

    it("disables the pickers and Crear while loading but keeps the other fields editable", async () => {
      // Arrange
      await renderDialog({ getMyAthletes: () => new Promise(() => {}) }).rendered;
      await screen.findByRole("dialog", { name: "Nuevo entrenamiento" });
      const user = userEvent.setup();

      // Act
      await fill(user, { Nombre: "Pierna" });

      // Assert
      expect(screen.getByRole("textbox", { name: "Nombre" })).toHaveValue("Pierna");
      expect(athleteField()).toBeDisabled();
      expect(screen.getByRole("button", { name: "Crear" })).toBeDisabled();
    });

    it("shows the error with a retry that reads the people again", async () => {
      // Arrange
      let failing = true;
      const { getMyAthletes, rendered } = renderDialog({
        getMyAthletes: async () =>
          failing ? fail(new DomainError("NETWORK", "Offline")) : ok(myAthletes),
      });
      await rendered;
      const user = userEvent.setup();
      const retry = await screen.findByRole("button", { name: "Reintentar" });
      expect(athleteField()).toBeDisabled();
      expect(screen.getByRole("button", { name: "Crear" })).toBeDisabled();
      failing = false;

      // Act
      await user.click(retry);

      // Assert
      await waitFor(() => expect(athleteField()).toBeEnabled());
      expect(getMyAthletes).toHaveBeenCalledTimes(2);
      expect(screen.queryByRole("button", { name: "Reintentar" })).not.toBeInTheDocument();
      expect(screen.getByRole("button", { name: "Crear" })).toBeEnabled();
    });

    it.each(["FORBIDDEN", "NETWORK"] as DomainErrorCode[])(
      "shows the %s error and keeps Crear disabled",
      async (code) => {
        // Arrange & Act
        await renderDialog({
          getMyAthletes: async () => fail(new DomainError(code, "Rejected")),
        }).rendered;

        // Assert
        expect(await screen.findByRole("button", { name: "Reintentar" })).toBeInTheDocument();
        expect(screen.getByRole("button", { name: "Crear" })).toBeDisabled();
      },
    );
  });

  it("disables Crear while the workout is being created", async () => {
    // Arrange
    const { rendered } = renderDialog({ createWorkout: () => new Promise(() => {}) });
    await rendered;
    await openedDialog();
    const user = userEvent.setup();
    await fillTrainerForm(user);

    // Act
    await user.click(screen.getByRole("button", { name: "Crear" }));

    // Assert
    expect(await screen.findByRole("button", { name: "Crear" })).toBeDisabled();
  });

  it("closes and opens the created workout's screen", async () => {
    // Arrange
    const { rendered } = renderDialog();
    await rendered;
    await openedDialog();
    const user = userEvent.setup();
    await fillTrainerForm(user);

    // Act
    await user.click(screen.getByRole("button", { name: "Crear" }));

    // Assert
    await waitFor(() => expect(push).toHaveBeenCalledWith("/workout?id=w-1"));
    await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
  });

  it.each([
    ["NOT_FOUND", "No hemos encontrado lo que buscas."],
    ["VALIDATION", "Revisa los datos introducidos."],
  ] as const)("keeps the dialog and the values when the API answers %s", async (code, message) => {
    // Arrange
    const { rendered } = renderDialog({
      createWorkout: async () => fail(new DomainError(code, "")),
    });
    await rendered;
    await openedDialog();
    const user = userEvent.setup();
    await fillTrainerForm(user);

    // Act
    await user.click(screen.getByRole("button", { name: "Crear" }));

    // Assert
    expect(await screen.findByText(message)).toBeInTheDocument();
    expect(screen.getByRole("dialog", { name: "Nuevo entrenamiento" })).toBeInTheDocument();
    expect(screen.getByRole("textbox", { name: "Nombre" })).toHaveValue("  Pierna  ");
    expect(athleteField()).toHaveValue(ana);
    expect(push).not.toHaveBeenCalled();
  });

  it("closes on Cancelar without a request and opens again empty", async () => {
    // Arrange
    const { execute, rendered } = renderDialog();
    await rendered;
    await openedDialog();
    const user = userEvent.setup();
    await fill(user, { Nombre: "Pierna" });
    await choose(user, athleteField, ana);
    await user.click(screen.getByRole("button", { name: "Cancelar" }));
    await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());

    // Act
    await user.click(screen.getByRole("button", { name: "Abrir" }));

    // Assert
    await openedDialog();
    expect(screen.getByRole("textbox", { name: "Nombre" })).toHaveValue("");
    expect(athleteField()).toHaveValue("");
    expect(execute).not.toHaveBeenCalled();
  });

  it("follows the active language", async () => {
    // Arrange
    await renderDialog({ profile: adminProfile }).rendered;
    await openedDialog();

    // Act
    await act(() => i18n.changeLanguage("en"));

    // Assert
    expect(screen.getByRole("dialog", { name: "New workout" })).toBeInTheDocument();
    for (const name of ["Name", "Description"]) {
      expect(screen.getByRole("textbox", { name })).toBeInTheDocument();
    }
    for (const name of ["Athlete", "Trainer"]) {
      expect(screen.getByRole("combobox", { name })).toBeInTheDocument();
    }
    const week = screen.getByRole("group", { name: "Week" });
    expect(within(week).getByText("Week of Oct 26 – Nov 1")).toBeInTheDocument();
    for (const name of ["Previous week", "Next week", "Pick a week"]) {
      expect(within(week).getByRole("button", { name })).toBeInTheDocument();
    }
    expect(screen.getByRole("button", { name: "Cancel" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Create" })).toBeInTheDocument();
  });

  describe("week field", () => {
    it("shows the selected week with its buttons", async () => {
      // Arrange & Act
      await renderDialog().rendered;

      // Assert
      await openedDialog();
      const field = weekField();
      expect(within(field).getByText("Semana")).toBeInTheDocument();
      expect(within(field).getByText("Semana del 26 oct – 1 nov")).toBeInTheDocument();
      for (const name of ["Semana anterior", "Semana siguiente", "Elegir semana"]) {
        expect(within(field).getByRole("button", { name })).toBeInTheDocument();
      }
    });

    it("moves to the previous week without a request", async () => {
      // Arrange
      const { execute, rendered } = renderDialog();
      await rendered;
      await openedDialog();
      const user = userEvent.setup();

      // Act
      await user.click(within(weekField()).getByRole("button", { name: "Semana anterior" }));

      // Assert
      expect(within(weekField()).getByText("Semana del 19 oct – 25 oct")).toBeInTheDocument();
      expect(execute).not.toHaveBeenCalled();
    });

    it("moves to the next week across a month", async () => {
      // Arrange
      await renderDialog().rendered;
      await openedDialog();
      const user = userEvent.setup();

      // Act
      await user.click(within(weekField()).getByRole("button", { name: "Semana siguiente" }));

      // Assert
      expect(within(weekField()).getByText("Semana del 2 nov – 8 nov")).toBeInTheDocument();
    });

    it("disables the week field while the workout is being created", async () => {
      // Arrange
      const { rendered } = renderDialog({ createWorkout: () => new Promise(() => {}) });
      await rendered;
      await openedDialog();
      const user = userEvent.setup();
      await fillTrainerForm(user);

      // Act
      await user.click(screen.getByRole("button", { name: "Crear" }));

      // Assert
      await waitFor(() =>
        expect(within(weekField()).getByRole("button", { name: "Semana anterior" })).toBeDisabled(),
      );
      expect(within(weekField()).getByRole("button", { name: "Semana siguiente" })).toBeDisabled();
      expect(within(weekField()).getByRole("button", { name: "Elegir semana" })).toBeDisabled();
    });

    it("selects the week of the day picked in the calendar", async () => {
      // Arrange
      await renderDialog().rendered;
      await openedDialog();
      const picker = within(weekField()).getByLabelText("Elegir semana", { selector: "input" });

      // Act
      fireEvent.change(picker, { target: { value: "2026-11-13" } });

      // Assert
      expect(within(weekField()).getByText("Semana del 9 nov – 15 nov")).toBeInTheDocument();
    });

    it("shows both years and sends the Wednesday of a week spanning two years", async () => {
      // Arrange
      vi.setSystemTime(new Date(2026, 11, 29, 12));
      const { execute, rendered } = renderDialog();
      await rendered;
      await openedDialog();
      const user = userEvent.setup();
      await fillTrainerForm(user);
      expect(
        within(weekField()).getByText("Semana del 28 dic 2026 – 3 ene 2027"),
      ).toBeInTheDocument();

      // Act
      await user.click(screen.getByRole("button", { name: "Crear" }));

      // Assert
      await waitFor(() =>
        expect(execute).toHaveBeenCalledWith(
          expect.objectContaining({ workoutDate: "2026-12-30" }),
        ),
      );
    });
  });

  describe("sections", () => {
    const templateSection = () => screen.getByRole("button", { name: "Desde plantilla" });
    const templateList = () => screen.getByRole("list", { name: "Plantillas" });
    const templateRow = (name: string) =>
      within(templateList())
        .getAllByRole("button")
        .find((row) => within(row).queryByText(name) !== null)!;

    it("opens on Desde cero, with both sections on the left and today's form", async () => {
      // Arrange & Act
      const { getTemplates, rendered } = renderDialog();
      await rendered;

      // Assert
      const dialog = await openedDialog();
      const sections = within(dialog).getByRole("list", { name: "Origen del entrenamiento" });
      expect(
        within(sections)
          .getAllByRole("button")
          .map((section) => section.textContent),
      ).toEqual(["Desde cero", "Desde plantilla"]);
      expect(within(sections).getByRole("button", { name: "Desde cero" })).toHaveAttribute(
        "aria-current",
        "true",
      );
      expect(screen.getByRole("textbox", { name: "Nombre" })).toBeInTheDocument();
      expect(getTemplates).not.toHaveBeenCalled();
    });

    it("lists a trainer's templates with their description and exercise count, without a name field", async () => {
      // Arrange
      const { getTemplates, rendered } = renderDialog();
      await rendered;
      await openedDialog();
      const user = userEvent.setup();

      // Act
      await user.click(templateSection());

      // Assert
      await screen.findByText("Pierna A");
      expect(getTemplates).toHaveBeenCalledTimes(1);
      expect(getTemplates).toHaveBeenCalledWith({});
      expect(within(templateRow("Pierna A")).getByText("Fuerza")).toBeInTheDocument();
      expect(within(templateRow("Pierna A")).getByText("3 ejercicios")).toBeInTheDocument();
      expect(within(templateRow("Torso")).getByText("1 ejercicio")).toBeInTheDocument();
      expect(screen.queryByRole("textbox", { name: "Nombre" })).not.toBeInTheDocument();
      expect(screen.queryByRole("textbox", { name: "Descripción" })).not.toBeInTheDocument();
      expect(athleteField()).toBeInTheDocument();
      expect(weekField()).toBeInTheDocument();
    });

    it("keeps only one template selected", async () => {
      // Arrange
      await renderDialog().rendered;
      await openedDialog();
      const user = userEvent.setup();
      await user.click(templateSection());
      await screen.findByText("Pierna A");
      await user.click(templateRow("Pierna A"));

      // Act
      await user.click(templateRow("Torso"));

      // Assert
      expect(templateRow("Torso")).toHaveAttribute("aria-pressed", "true");
      expect(templateRow("Pierna A")).toHaveAttribute("aria-pressed", "false");
    });

    it("asks an admin to choose a trainer first, then reads that trainer's templates", async () => {
      // Arrange
      const { getTemplates, rendered } = renderDialog({ profile: adminProfile });
      await rendered;
      await openedDialog();
      const user = userEvent.setup();
      await user.click(templateSection());
      expect(
        await screen.findByText("Elige un entrenador para ver sus plantillas."),
      ).toBeInTheDocument();
      expect(getTemplates).not.toHaveBeenCalled();

      // Act
      await choose(user, trainerField, luis);

      // Assert
      await screen.findByText("Pierna A");
      expect(getTemplates).toHaveBeenCalledTimes(1);
      expect(getTemplates).toHaveBeenCalledWith({ trainerId });
    });

    it("shows the empty message when there are no templates", async () => {
      // Arrange
      await renderDialog({ getTemplates: async () => ok([]) }).rendered;
      await openedDialog();
      const user = userEvent.setup();

      // Act
      await user.click(templateSection());

      // Assert
      expect(await screen.findByText("No hay plantillas disponibles.")).toBeInTheDocument();
    });

    it("shows a failed read and reads the templates again on Reintentar", async () => {
      // Arrange
      const getTemplates = vi
        .fn<() => Promise<Result<WorkoutTemplate[], DomainError>>>()
        .mockResolvedValueOnce(fail(new DomainError("NETWORK", "")))
        .mockResolvedValueOnce(ok(templates));
      const { getTemplates: execute, rendered } = renderDialog({ getTemplates });
      await rendered;
      await openedDialog();
      const user = userEvent.setup();
      await user.click(templateSection());
      await screen.findByText("No hay conexión con el servidor.");

      // Act
      await user.click(screen.getByRole("button", { name: "Reintentar" }));

      // Assert
      expect(await screen.findByText("Pierna A")).toBeInTheDocument();
      expect(execute).toHaveBeenCalledTimes(2);
    });

    it("asks for a template and an athlete and sends nothing without them", async () => {
      // Arrange
      const { createFromTemplate, rendered } = renderDialog();
      await rendered;
      await openedDialog();
      const user = userEvent.setup();
      await user.click(templateSection());
      await screen.findByText("Pierna A");

      // Act
      await user.click(screen.getByRole("button", { name: "Crear" }));

      // Assert
      expect(screen.getByText("Elige una plantilla.")).toBeInTheDocument();
      expect(screen.getByText("Selecciona un atleta.")).toBeInTheDocument();
      expect(createFromTemplate).not.toHaveBeenCalled();
    });

    it("creates the workout from the template once, for the athlete and the week, and opens it", async () => {
      // Arrange
      const { execute, createFromTemplate, rendered } = renderDialog();
      await rendered;
      await openedDialog();
      const user = userEvent.setup();
      await user.click(templateSection());
      await screen.findByText("Pierna A");
      await user.click(templateRow("Pierna A"));
      await choose(user, athleteField, ana);

      // Act
      await user.click(screen.getByRole("button", { name: "Crear" }));

      // Assert
      await waitFor(() => expect(push).toHaveBeenCalledWith("/workout?id=w-9"));
      expect(createFromTemplate).toHaveBeenCalledTimes(1);
      expect(createFromTemplate).toHaveBeenCalledWith({
        templateId: "wt-1",
        userId: athleteId,
        workoutDate: "2026-10-28",
      });
      expect(execute).not.toHaveBeenCalled();
    });

    it("keeps the dialog and the selection when the creation from a template is refused", async () => {
      // Arrange
      await renderDialog({
        createFromTemplate: async () => fail(new DomainError("FORBIDDEN", "")),
      }).rendered;
      await openedDialog();
      const user = userEvent.setup();
      await user.click(templateSection());
      await screen.findByText("Pierna A");
      await user.click(templateRow("Pierna A"));
      await choose(user, athleteField, ana);

      // Act
      await user.click(screen.getByRole("button", { name: "Crear" }));

      // Assert
      expect(await screen.findByText(/permiso/i)).toBeInTheDocument();
      expect(templateRow("Pierna A")).toHaveAttribute("aria-pressed", "true");
      expect(push).not.toHaveBeenCalled();
    });

    it("names the sections in English", async () => {
      // Arrange
      await renderDialog().rendered;
      await openedDialog();

      // Act
      await act(() => i18n.changeLanguage("en"));

      // Assert
      expect(screen.getByRole("button", { name: "From scratch" })).toBeInTheDocument();
      expect(screen.getByRole("button", { name: "From template" })).toBeInTheDocument();
    });
  });
});
