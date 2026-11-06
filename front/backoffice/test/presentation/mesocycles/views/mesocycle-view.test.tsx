import { act, fireEvent, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { DomainError, fail, ok } from "@weight-tracker/core/domain";
import type {
  CreateMesocycleRequest,
  CreateWorkoutRequest,
  DefineMesocycleWeekInput,
  Mesocycle,
  RedefineMesocycleWeekInput,
  Result,
  TrainerAthlete,
  UpdateMesocycleInput,
  UserProfile,
  UserSummary,
  Workout,
  WorkoutListItem,
} from "@weight-tracker/core/domain";
import i18n from "@/infrastructure/i18n";
import MesocycleView from "@/presentation/mesocycles/views/mesocycle-view";
import renderWithProviders, { completeProfile } from "../../../support/render-with-providers";

const replace = vi.fn();
const push = vi.fn();
const navigation = vi.hoisted(() => ({ search: "" }));

vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace, push }),
  usePathname: () => "/mesocycle",
  useSearchParams: () => new URLSearchParams(navigation.search),
}));

const athleteId = "5f1c2f0e-9d5b-4a53-8c7e-0d6f7c1a2b3c";
const trainerId = "6a2d3f1e-0e6c-4b64-9d8f-1e7a8d2b3c4d";
const adminProfile: UserProfile = { ...completeProfile, role: "ADMIN" };

const ana = {
  id: athleteId,
  email: "ana@example.com",
  name: "Ana",
  surname: "García",
  role: "USER",
};
const luis = {
  id: trainerId,
  email: "luis@example.com",
  name: "Luis",
  surname: "Pérez",
  role: "TRAINER",
};

const stored: Mesocycle = {
  id: "m-1",
  trainerId,
  athleteId,
  name: "Hipertrofia",
  objective: "Ganar masa muscular",
  startDate: "2026-11-03",
  durationInWeeks: 4,
  weeks: [
    { weekNumber: 3, trainingWeekType: "ACCUMULATION", objective: "Subir volumen" },
    { weekNumber: 1, trainingWeekType: "ADAPTATION", objective: "Cargas iniciales" },
  ],
  trainer: luis,
  athlete: ana,
};

function athleteOf(id: string, name: string, surname: string, email: string): TrainerAthlete {
  return {
    id: `r-${email}`,
    athleteId: id,
    email,
    name,
    surname,
    trainerName: "Luis",
    trainerSurname: "Pérez",
    startedAtUtc: "2026-01-01T00:00:00Z",
  };
}

function userOf(
  id: string,
  role: string,
  name: string,
  surname: string,
  email: string,
): UserSummary {
  return { id, email, name, surname, role, isActive: true, deactivatedAtUtc: null, trainer: null };
}

const myAthletes = [
  athleteOf(athleteId, "Ana", "García", "ana@example.com"),
  athleteOf("7b3e4a2f-1f7d-4c75-8e90-2f8b9e3c4d5e", "Bea", "López", "bea@example.com"),
];

const users = [
  userOf(athleteId, "USER", "Ana", "García", "ana@example.com"),
  userOf(trainerId, "TRAINER", "Luis", "Pérez", "luis@example.com"),
];

/** A workout of mesocycle `m-1`, as listed, in the given week. */
function weekWorkoutOf(id: string, name: string, week: number): WorkoutListItem {
  return {
    id,
    name,
    description: null,
    workoutDate: "2026-11-04",
    userId: athleteId,
    trainerId,
    status: "DRAFT",
    exercises: [],
    athlete: ana,
    trainer: luis,
    mesocycleId: "m-1",
    mesocycleWeekNumber: week,
  };
}

/** One exercise of a listed workout; only how many there are is shown. */
const exercise = {
  id: "0a1b2c3d-4e5f-4a6b-8c7d-9e0f1a2b3c4d",
  exerciseId: "1f2e3d4c-5b6a-4978-8695-a4b3c2d1e0f9",
  translations: [],
  muscle: "QUADRICEPS",
  bodyPart: "LEGS",
  type: "STRENGTH",
  effort: "HIGH",
  perSide: false,
  restMinutes: null,
  sets: [],
  superSeriesId: null,
} satisfies WorkoutListItem["exercises"][number];

const legs: WorkoutListItem = {
  ...weekWorkoutOf("w-1", "Pierna", 1),
  exercises: [exercise, { ...exercise, id: "e-2" }, { ...exercise, id: "e-3" }],
};
const pushDay = weekWorkoutOf("w-2", "Empuje", 1);

/** What the API answers to a workout creation: the workout, placed as asked. */
function createdWorkout(request: CreateWorkoutRequest): Workout {
  return {
    id: "w-9",
    name: request.name ?? "",
    description: request.description ?? null,
    workoutDate: "2026-11-11",
    userId: request.userId ?? athleteId,
    trainerId: request.trainerId ?? trainerId,
    status: "DRAFT",
    exercises: [],
    mesocycleId: request.mesocycleId ?? null,
    mesocycleWeekNumber: request.mesocycleWeekNumber ?? null,
  };
}

type Answer<T> = Promise<Result<T, DomainError>>;

function renderMesocycle({
  search = "",
  profile = completeProfile,
  getMesocycle = async () => ok(stored),
  createMesocycle = async () => ok({ ...stored, weeks: [] }),
  updateMesocycle = async (input: UpdateMesocycleInput) =>
    ok({
      ...stored,
      name: input.name ?? "",
      objective: input.objective ?? "",
      startDate: input.startDate ?? "",
      durationInWeeks: input.durationInWeeks ?? 0,
    }),
  defineWeek = async (input: DefineMesocycleWeekInput) =>
    ok({
      ...stored,
      weeks: [
        ...stored.weeks,
        {
          weekNumber: input.weekNumber ?? 0,
          trainingWeekType: input.trainingWeekType ?? "",
          objective: input.objective ?? "",
        },
      ],
    }),
  redefineWeek = async (input: RedefineMesocycleWeekInput) =>
    ok({
      ...stored,
      weeks: stored.weeks.map((week) =>
        week.weekNumber === input.weekNumber
          ? {
              ...week,
              trainingWeekType: input.trainingWeekType ?? "",
              objective: input.objective ?? "",
            }
          : week,
      ),
    }),
  getMyAthletes = async () => ok(myAthletes),
  getUsers = async () => ok(users),
  getWorkouts = async () => ok([]),
  createWorkout = async (request: CreateWorkoutRequest) => ok(createdWorkout(request)),
}: {
  search?: string;
  profile?: UserProfile;
  getMesocycle?: () => Answer<Mesocycle>;
  createMesocycle?: (request: CreateMesocycleRequest) => Answer<Mesocycle>;
  updateMesocycle?: (input: UpdateMesocycleInput) => Answer<Mesocycle>;
  defineWeek?: (input: DefineMesocycleWeekInput) => Answer<Mesocycle>;
  redefineWeek?: (input: RedefineMesocycleWeekInput) => Answer<Mesocycle>;
  getMyAthletes?: () => Answer<TrainerAthlete[]>;
  getUsers?: () => Answer<UserSummary[]>;
  getWorkouts?: () => Answer<WorkoutListItem[]>;
  createWorkout?: (request: CreateWorkoutRequest) => Answer<Workout>;
} = {}) {
  navigation.search = search;
  const fakes = {
    getMesocycle: vi.fn(getMesocycle),
    createMesocycle: vi.fn(createMesocycle),
    updateMesocycle: vi.fn(updateMesocycle),
    defineWeek: vi.fn(defineWeek),
    redefineWeek: vi.fn(redefineWeek),
    getMyAthletes: vi.fn(getMyAthletes),
    getUsers: vi.fn(getUsers),
    getWorkouts: vi.fn(getWorkouts),
    createWorkout: vi.fn(createWorkout),
  };

  return {
    ...fakes,
    rendered: renderWithProviders(<MesocycleView />, {
      getSessionUseCase: () => ({
        execute: async () => ok({ accessToken: "a", refreshToken: "r" }),
      }),
      getMyProfileUseCase: () => ({ execute: async () => ok(profile) }),
      getMesocycleByIdUseCase: () => ({ execute: fakes.getMesocycle }),
      createMesocycleUseCase: () => ({ execute: fakes.createMesocycle }),
      updateMesocycleUseCase: () => ({ execute: fakes.updateMesocycle }),
      defineMesocycleWeekUseCase: () => ({ execute: fakes.defineWeek }),
      redefineMesocycleWeekUseCase: () => ({ execute: fakes.redefineWeek }),
      getMyAthletesUseCase: () => ({ execute: fakes.getMyAthletes }),
      getUsersUseCase: () => ({ execute: fakes.getUsers }),
      getMesocycleWorkoutsUseCase: () => ({ execute: fakes.getWorkouts }),
      createWorkoutUseCase: () => ({ execute: fakes.createWorkout }),
    }),
  };
}

const athleteField = () => screen.getByRole("combobox", { name: "Atleta" });
const trainerField = () => screen.getByRole("combobox", { name: "Entrenador" });
const nameField = () => screen.getByRole("textbox", { name: "Nombre" });
const objectiveField = () => screen.getByRole("textbox", { name: "Objetivo general" });
const durationField = () => screen.getByRole("spinbutton", { name: "Duración (semanas)" });
const startDateField = () => screen.getByLabelText(/Fecha de inicio/);

/** Waits until the profile and the people are read, so the create form is settled. */
async function openedCreateForm() {
  await screen.findByRole("heading", { level: 1, name: "Crear mesociclo" });
  await waitFor(() => expect(athleteField()).toBeEnabled());
}

/** Waits until the stored mesocycle is shown. */
async function openedEditForm() {
  await screen.findByRole("heading", { level: 1, name: "Editar mesociclo" });
}

async function type(user: ReturnType<typeof userEvent.setup>, field: HTMLElement, value: string) {
  await user.clear(field);
  // Pasting keeps long values fast; the field still receives a real change event.
  await user.click(field);
  await user.paste(value);
}

async function choose(
  user: ReturnType<typeof userEvent.setup>,
  field: () => HTMLElement,
  option: string,
) {
  await user.click(field());
  await user.click(await screen.findByRole("option", { name: option }));
}

/** Fills every field a trainer needs to create a mesocycle. */
async function fillTrainerForm(user: ReturnType<typeof userEvent.setup>) {
  await choose(user, athleteField, "Ana García · ana@example.com");
  await type(user, nameField(), " Hipertrofia ");
  fireEvent.change(startDateField(), { target: { value: "2026-11-03" } });
  await type(user, objectiveField(), "Ganar masa");
}

const weeksCard = () => screen.getByRole("region", { name: "Planificación semanal" });
const weekRows = () => within(weeksCard()).getAllByRole("listitem");

describe("MesocycleView", () => {
  beforeEach(() => {
    replace.mockClear();
    push.mockClear();
  });

  describe("create mode", () => {
    it("shows the header, the two cards in order and the create form", async () => {
      // Arrange
      const { getMesocycle, rendered } = renderMesocycle();

      // Act
      await rendered;
      await openedCreateForm();

      // Assert
      expect(
        screen.getByText("Define el objetivo del mesociclo y planifica sus semanas"),
      ).toBeInTheDocument();
      expect(screen.getByRole("link", { name: "Volver" })).toHaveAttribute(
        "href",
        "/assigned-plans",
      );
      const [details, weeks] = screen.getAllByRole("region");
      expect(details).toHaveAccessibleName("Información general");
      expect(weeks).toHaveAccessibleName("Planificación semanal");
      expect(nameField()).toHaveValue("");
      expect(startDateField()).toHaveValue("");
      expect(durationField()).toHaveValue(4);
      expect(objectiveField()).toHaveValue("");
      expect(screen.queryByRole("region", { name: "Personas del mesociclo" })).toBeNull();
      expect(screen.getByRole("button", { name: "Crear mesociclo" })).toBeEnabled();
      expect(getMesocycle).not.toHaveBeenCalled();
    });

    it("counts the typed characters against the maximum", async () => {
      // Arrange
      await renderMesocycle().rendered;
      await openedCreateForm();
      const user = userEvent.setup();

      // Act
      await type(user, nameField(), "Hipertrofia");

      // Assert
      expect(screen.getByText("11 / 256")).toBeInTheDocument();
    });

    it("takes a typed duration, with no stepper buttons", async () => {
      // Arrange
      await renderMesocycle().rendered;
      await openedCreateForm();
      const user = userEvent.setup();

      // Act
      await type(user, durationField(), "6");

      // Assert
      expect(durationField()).toHaveValue(6);
      expect(screen.queryByRole("button", { name: "Reducir duración" })).toBeNull();
      expect(screen.queryByRole("button", { name: "Aumentar duración" })).toBeNull();
    });

    it("reports a duration out of range and sends nothing", async () => {
      // Arrange
      const { createMesocycle, rendered } = renderMesocycle();
      await rendered;
      await openedCreateForm();
      const user = userEvent.setup();
      await fillTrainerForm(user);
      await type(user, durationField(), "53");

      // Act
      await user.click(screen.getByRole("button", { name: "Crear mesociclo" }));

      // Assert
      expect(screen.getByText("Debe estar entre 1 y 52.")).toBeInTheDocument();
      expect(createMesocycle).not.toHaveBeenCalled();
    });

    it("reports a blank duration and sends nothing", async () => {
      // Arrange
      const { createMesocycle, rendered } = renderMesocycle();
      await rendered;
      await openedCreateForm();
      const user = userEvent.setup();
      await fillTrainerForm(user);
      await user.clear(durationField());

      // Act
      await user.click(screen.getByRole("button", { name: "Crear mesociclo" }));

      // Assert
      expect(screen.getByText("Este campo es obligatorio.")).toBeInTheDocument();
      expect(createMesocycle).not.toHaveBeenCalled();
    });

    it("locks the weekly planning until the mesocycle exists", async () => {
      // Arrange & Act
      await renderMesocycle().rendered;
      await openedCreateForm();

      // Assert
      expect(
        within(weeksCard()).getByText("Guarda el mesociclo para planificar sus semanas."),
      ).toBeInTheDocument();
      expect(within(weeksCard()).getByRole("button", { name: "Añadir semana" })).toBeDisabled();
    });

    it("offers a trainer their athletes and no trainer picker", async () => {
      // Arrange
      const { getUsers, rendered } = renderMesocycle();
      await rendered;
      await openedCreateForm();
      const user = userEvent.setup();

      // Act
      await user.click(athleteField());

      // Assert
      expect(screen.getAllByRole("option").map((option) => option.textContent)).toEqual([
        "Ana García · ana@example.com",
        "Bea López · bea@example.com",
      ]);
      expect(screen.queryByRole("combobox", { name: "Entrenador" })).not.toBeInTheDocument();
      expect(getUsers).not.toHaveBeenCalled();
    });

    it("sends a trainer's creation without a trainer and opens the new mesocycle", async () => {
      // Arrange
      const { createMesocycle, rendered } = renderMesocycle();
      await rendered;
      await openedCreateForm();
      const user = userEvent.setup();
      await fillTrainerForm(user);

      // Act
      await user.click(screen.getByRole("button", { name: "Crear mesociclo" }));

      // Assert
      await waitFor(() => expect(replace).toHaveBeenCalledWith("/mesocycle?id=m-1"));
      expect(createMesocycle).toHaveBeenCalledTimes(1);
      expect(createMesocycle).toHaveBeenCalledWith({
        athleteId,
        name: "Hipertrofia",
        objective: "Ganar masa",
        startDate: "2026-11-03",
        durationInWeeks: 4,
      });
      expect(push).not.toHaveBeenCalled();
    });

    it("asks an admin for a trainer after the athlete and sends it", async () => {
      // Arrange
      const { createMesocycle, getMyAthletes, rendered } = renderMesocycle({
        profile: adminProfile,
      });
      await rendered;
      await openedCreateForm();
      const user = userEvent.setup();
      await fillTrainerForm(user);

      // Act
      await user.click(screen.getByRole("button", { name: "Crear mesociclo" }));

      // Assert
      expect(await screen.findByText("Selecciona un entrenador.")).toBeInTheDocument();
      expect(createMesocycle).not.toHaveBeenCalled();
      expect(screen.getAllByRole("combobox")).toEqual([athleteField(), trainerField()]);
      await choose(user, trainerField, "Luis Pérez · luis@example.com");
      await user.click(screen.getByRole("button", { name: "Crear mesociclo" }));
      await waitFor(() => expect(createMesocycle).toHaveBeenCalledTimes(1));
      expect(createMesocycle.mock.calls[0]![0]).toMatchObject({ athleteId, trainerId });
      expect(getMyAthletes).not.toHaveBeenCalled();
    });

    it("reports every blank required field and sends nothing", async () => {
      // Arrange
      const { createMesocycle, rendered } = renderMesocycle();
      await rendered;
      await openedCreateForm();
      const user = userEvent.setup();

      // Act
      await user.click(screen.getByRole("button", { name: "Crear mesociclo" }));

      // Assert
      expect(screen.getAllByText("Este campo es obligatorio.")).toHaveLength(3);
      expect(screen.getByText("Selecciona un atleta.")).toBeInTheDocument();
      expect(createMesocycle).not.toHaveBeenCalled();
    });

    it("reports texts longer than their maximum and sends nothing", async () => {
      // Arrange
      const { createMesocycle, rendered } = renderMesocycle();
      await rendered;
      await openedCreateForm();
      const user = userEvent.setup();
      await fillTrainerForm(user);
      await type(user, nameField(), "a".repeat(257));
      await type(user, objectiveField(), "a".repeat(4001));

      // Act
      await user.click(screen.getByRole("button", { name: "Crear mesociclo" }));

      // Assert
      expect(screen.getByText("No puede superar los 256 caracteres.")).toBeInTheDocument();
      expect(screen.getByText("No puede superar los 4000 caracteres.")).toBeInTheDocument();
      expect(createMesocycle).not.toHaveBeenCalled();
    });

    it("disables the create action while the creation is in progress", async () => {
      // Arrange
      const { rendered } = renderMesocycle({ createMesocycle: () => new Promise(() => {}) });
      await rendered;
      await openedCreateForm();
      const user = userEvent.setup();
      await fillTrainerForm(user);

      // Act
      await user.click(screen.getByRole("button", { name: "Crear mesociclo" }));

      // Assert
      expect(screen.getByRole("button", { name: "Crear mesociclo" })).toBeDisabled();
    });

    it("keeps the form and shows the error when the creation is refused", async () => {
      // Arrange
      const { rendered } = renderMesocycle({
        createMesocycle: async () => fail(new DomainError("FORBIDDEN", "")),
      });
      await rendered;
      await openedCreateForm();
      const user = userEvent.setup();
      await fillTrainerForm(user);

      // Act
      await user.click(screen.getByRole("button", { name: "Crear mesociclo" }));

      // Assert
      expect(
        await screen.findByText("No tienes permiso para realizar esta acción."),
      ).toBeInTheDocument();
      expect(nameField()).toHaveValue(" Hipertrofia ");
      expect(
        screen.getByRole("heading", { level: 1, name: "Crear mesociclo" }),
      ).toBeInTheDocument();
      expect(replace).not.toHaveBeenCalled();
    });

    it("shows a people failure with Reintentar and waits to create until they load", async () => {
      // Arrange
      const getMyAthletes = vi
        .fn<() => Answer<TrainerAthlete[]>>()
        .mockResolvedValueOnce(fail(new DomainError("NETWORK", "")))
        .mockResolvedValueOnce(ok(myAthletes));
      const { rendered } = renderMesocycle({ getMyAthletes });
      await rendered;
      await screen.findByText("No hay conexión con el servidor.");
      const user = userEvent.setup();
      expect(screen.getByRole("button", { name: "Crear mesociclo" })).toBeDisabled();

      // Act
      await user.click(screen.getByRole("button", { name: "Reintentar" }));

      // Assert
      await waitFor(() => expect(athleteField()).toBeEnabled());
      expect(screen.getByRole("button", { name: "Crear mesociclo" })).toBeEnabled();
      expect(getMyAthletes).toHaveBeenCalledTimes(2);
    });
  });

  describe("edit mode", () => {
    it("reads the mesocycle once and shows its data to a trainer without the trainer", async () => {
      // Arrange
      const { getMesocycle, rendered } = renderMesocycle({ search: "id=m-1" });

      // Act
      await rendered;
      await openedEditForm();

      // Assert
      expect(getMesocycle).toHaveBeenCalledTimes(1);
      expect(getMesocycle).toHaveBeenCalledWith({ id: "m-1" });
      expect(nameField()).toHaveValue("Hipertrofia");
      expect(startDateField()).toHaveValue("2026-11-03");
      expect(durationField()).toHaveValue(4);
      expect(objectiveField()).toHaveValue("Ganar masa muscular");
      const people = screen.getByRole("region", { name: "Personas del mesociclo" });
      expect(within(people).getByText("AG")).toBeInTheDocument();
      expect(within(people).getByText("Ana García")).toBeInTheDocument();
      expect(within(people).getByText("Atleta")).toBeInTheDocument();
      expect(
        people.compareDocumentPosition(
          screen.getByRole("region", { name: "Información general" }),
        ) & Node.DOCUMENT_POSITION_FOLLOWING,
      ).toBeTruthy();
      const details = screen.getByRole("region", { name: "Información general" });
      expect(within(details).queryByText("Ana García")).not.toBeInTheDocument();
      // The header's user menu names the signed-in trainer's own role, so only the strip is checked.
      expect(within(people).queryByText("Entrenador")).not.toBeInTheDocument();
      expect(screen.queryByText("Luis Pérez")).not.toBeInTheDocument();
      expect(screen.queryByRole("combobox")).not.toBeInTheDocument();
      expect(screen.getByRole("button", { name: "Guardar" })).toBeInTheDocument();
    });

    it("shows the trainer to an admin", async () => {
      // Arrange & Act
      await renderMesocycle({ search: "id=m-1", profile: adminProfile }).rendered;
      await openedEditForm();

      // Assert
      const people = screen.getByRole("region", { name: "Personas del mesociclo" });
      expect(within(people).getByText("LP")).toBeInTheDocument();
      expect(within(people).getByText("Luis Pérez")).toBeInTheDocument();
      expect(within(people).getByText("Entrenador")).toBeInTheDocument();
      const details = screen.getByRole("region", { name: "Información general" });
      expect(within(details).queryByText("Luis Pérez")).not.toBeInTheDocument();
    });

    it("shows the athlete's email when they have no name", async () => {
      // Arrange
      const nameless = { ...stored, athlete: { ...stored.athlete, name: "", surname: "" } };

      // Act
      await renderMesocycle({ search: "id=m-1", getMesocycle: async () => ok(nameless) }).rendered;
      await openedEditForm();

      // Assert
      const people = screen.getByRole("region", { name: "Personas del mesociclo" });
      expect(within(people).getByText("A")).toBeInTheDocument();
      expect(within(people).getByText(stored.athlete.email)).toBeInTheDocument();
    });

    it("sends every general field on Guardar and confirms the save", async () => {
      // Arrange
      const { updateMesocycle, rendered } = renderMesocycle({ search: "id=m-1" });
      await rendered;
      await openedEditForm();
      const user = userEvent.setup();
      await type(user, durationField(), "6");

      // Act
      await user.click(screen.getByRole("button", { name: "Guardar" }));

      // Assert
      expect(await screen.findByText("Mesociclo guardado.")).toBeInTheDocument();
      expect(updateMesocycle).toHaveBeenCalledTimes(1);
      expect(updateMesocycle).toHaveBeenCalledWith({
        id: "m-1",
        name: "Hipertrofia",
        objective: "Ganar masa muscular",
        startDate: "2026-11-03",
        durationInWeeks: 6,
      });
      await type(user, nameField(), "Fuerza");
      expect(screen.queryByText("Mesociclo guardado.")).not.toBeInTheDocument();
    });

    it("keeps the typed duration when the API refuses it", async () => {
      // Arrange
      const { rendered } = renderMesocycle({
        search: "id=m-1",
        updateMesocycle: async () => fail(new DomainError("CONFLICT", "")),
      });
      await rendered;
      await openedEditForm();
      const user = userEvent.setup();
      await type(user, durationField(), "2");

      // Act
      await user.click(screen.getByRole("button", { name: "Guardar" }));

      // Assert
      expect(
        await screen.findByText("La operación entra en conflicto con el estado actual."),
      ).toBeInTheDocument();
      expect(durationField()).toHaveValue(2);
    });

    it("lists the weeks in order with their type, title and objective", async () => {
      // Arrange & Act
      await renderMesocycle({ search: "id=m-1" }).rendered;
      await openedEditForm();

      // Assert
      const [first, second] = weekRows();
      expect(first).toHaveTextContent("Adaptación");
      expect(first).toHaveTextContent("Semana 1");
      expect(first).toHaveTextContent("Cargas iniciales");
      expect(second).toHaveTextContent("Acumulación");
      expect(second).toHaveTextContent("Semana 3");
      expect(within(first).getByRole("button", { name: "Editar semana 1" })).toBeInTheDocument();
    });

    it("shows the empty message when no week is defined", async () => {
      // Arrange & Act
      await renderMesocycle({
        search: "id=m-1",
        getMesocycle: async () => ok({ ...stored, weeks: [] }),
      }).rendered;
      await openedEditForm();

      // Assert
      expect(
        within(weeksCard()).getByText("Todavía no hay semanas definidas."),
      ).toBeInTheDocument();
    });

    it("disables Añadir semana when every week is defined", async () => {
      // Arrange & Act
      await renderMesocycle({
        search: "id=m-1",
        getMesocycle: async () =>
          ok({
            ...stored,
            durationInWeeks: 2,
            weeks: [stored.weeks[1]!, { ...stored.weeks[0]!, weekNumber: 2 }],
          }),
      }).rendered;
      await openedEditForm();

      // Assert
      expect(within(weeksCard()).getByRole("button", { name: "Añadir semana" })).toBeDisabled();
    });

    it("defines the lowest free week and lists it", async () => {
      // Arrange
      const { defineWeek, rendered } = renderMesocycle({ search: "id=m-1" });
      await rendered;
      await openedEditForm();
      const user = userEvent.setup();
      await user.click(screen.getByRole("button", { name: "Añadir semana" }));
      const dialog = await screen.findByRole("dialog", { name: "Semana 2" });
      await user.click(within(dialog).getByRole("combobox", { name: "Tipo de semana" }));
      await user.click(await screen.findByRole("option", { name: "Acumulación" }));
      await type(
        user,
        within(dialog).getByRole("textbox", { name: "Objetivo" }),
        " Subir volumen ",
      );

      // Act
      await user.click(within(dialog).getByRole("button", { name: "Guardar" }));

      // Assert
      await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
      expect(defineWeek).toHaveBeenCalledTimes(1);
      expect(defineWeek).toHaveBeenCalledWith({
        mesocycleId: "m-1",
        weekNumber: 2,
        trainingWeekType: "ACCUMULATION",
        objective: "Subir volumen",
      });
      expect(weekRows().map((row) => within(row).getByText(/^Semana \d$/).textContent)).toEqual([
        "Semana 1",
        "Semana 2",
        "Semana 3",
      ]);
    });

    it("reports a missing week type and sends nothing", async () => {
      // Arrange
      const { defineWeek, rendered } = renderMesocycle({ search: "id=m-1" });
      await rendered;
      await openedEditForm();
      const user = userEvent.setup();
      await user.click(screen.getByRole("button", { name: "Añadir semana" }));
      const dialog = await screen.findByRole("dialog", { name: "Semana 2" });
      await type(user, within(dialog).getByRole("textbox", { name: "Objetivo" }), "Subir volumen");

      // Act
      await user.click(within(dialog).getByRole("button", { name: "Guardar" }));

      // Assert
      expect(within(dialog).getByText("Este campo es obligatorio.")).toBeInTheDocument();
      expect(defineWeek).not.toHaveBeenCalled();
    });

    it("redefines a week from its edit action", async () => {
      // Arrange
      const { redefineWeek, rendered } = renderMesocycle({ search: "id=m-1" });
      await rendered;
      await openedEditForm();
      const user = userEvent.setup();
      await user.click(screen.getByRole("button", { name: "Editar semana 1" }));
      const dialog = await screen.findByRole("dialog", { name: "Semana 1" });
      expect(within(dialog).getByRole("textbox", { name: "Objetivo" })).toHaveValue(
        "Cargas iniciales",
      );
      await user.click(within(dialog).getByRole("combobox", { name: "Tipo de semana" }));
      await user.click(await screen.findByRole("option", { name: "Descarga" }));

      // Act
      await user.click(within(dialog).getByRole("button", { name: "Guardar" }));

      // Assert
      await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
      expect(redefineWeek).toHaveBeenCalledWith({
        mesocycleId: "m-1",
        weekNumber: 1,
        trainingWeekType: "DELOAD",
        objective: "Cargas iniciales",
      });
      expect(weekRows()[0]).toHaveTextContent("Descarga");
    });

    it("keeps the week dialog with the error when the save is refused", async () => {
      // Arrange
      const { rendered } = renderMesocycle({
        search: "id=m-1",
        defineWeek: async () => fail(new DomainError("CONFLICT", "")),
      });
      await rendered;
      await openedEditForm();
      const user = userEvent.setup();
      await user.click(screen.getByRole("button", { name: "Añadir semana" }));
      const dialog = await screen.findByRole("dialog", { name: "Semana 2" });
      await user.click(within(dialog).getByRole("combobox", { name: "Tipo de semana" }));
      await user.click(await screen.findByRole("option", { name: "Adaptación" }));
      await type(user, within(dialog).getByRole("textbox", { name: "Objetivo" }), "Repetir");

      // Act
      await user.click(within(dialog).getByRole("button", { name: "Guardar" }));

      // Assert
      expect(
        await within(dialog).findByText("La operación entra en conflicto con el estado actual."),
      ).toBeInTheDocument();
      expect(within(dialog).getByRole("textbox", { name: "Objetivo" })).toHaveValue("Repetir");
    });

    it("closes the week dialog on Cancelar without a request", async () => {
      // Arrange
      const { defineWeek, rendered } = renderMesocycle({ search: "id=m-1" });
      await rendered;
      await openedEditForm();
      const user = userEvent.setup();
      await user.click(screen.getByRole("button", { name: "Añadir semana" }));
      const dialog = await screen.findByRole("dialog", { name: "Semana 2" });

      // Act
      await user.click(within(dialog).getByRole("button", { name: "Cancelar" }));

      // Assert
      await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
      expect(defineWeek).not.toHaveBeenCalled();
    });

    it("shows the not-found state for an unknown mesocycle", async () => {
      // Arrange & Act
      await renderMesocycle({
        search: "id=m-9",
        getMesocycle: async () => fail(new DomainError("NOT_FOUND", "")),
      }).rendered;

      // Assert
      expect(await screen.findByText("No hemos encontrado este mesociclo.")).toBeInTheDocument();
      expect(screen.getByRole("link", { name: "Volver a mesociclos" })).toHaveAttribute(
        "href",
        "/assigned-plans",
      );
    });

    it("reads the mesocycle again when Reintentar is pressed after a failure", async () => {
      // Arrange
      const getMesocycle = vi
        .fn<() => Answer<Mesocycle>>()
        .mockResolvedValueOnce(fail(new DomainError("NETWORK", "")))
        .mockResolvedValueOnce(ok(stored));
      const { rendered } = renderMesocycle({ search: "id=m-1", getMesocycle });
      await rendered;
      await screen.findByText("No hay conexión con el servidor.");
      const user = userEvent.setup();

      // Act
      await user.click(screen.getByRole("button", { name: "Reintentar" }));

      // Assert
      await openedEditForm();
      expect(getMesocycle).toHaveBeenCalledTimes(2);
    });
  });

  describe("week workouts", () => {
    /** The row of a defined week; its nested workout items never read "Semana <n>". */
    const weekRow = (number: number) =>
      weekRows().find((row) => within(row).queryByText(`Semana ${number}`)) as HTMLElement;

    /** Opens "Nuevo entrenamiento" in the given week and answers its dialog. */
    async function openWorkoutDialog(user: ReturnType<typeof userEvent.setup>, number: number) {
      await user.click(
        within(weekRow(number)).getByRole("button", { name: "Nuevo entrenamiento" }),
      );
      return screen.findByRole("dialog", { name: `Nuevo entrenamiento · Semana ${number}` });
    }

    it("reads the mesocycle's workouts once in edit mode", async () => {
      // Arrange
      const { getWorkouts, rendered } = renderMesocycle({ search: "id=m-1" });

      // Act
      await rendered;
      await openedEditForm();

      // Assert
      await waitFor(() => expect(getWorkouts).toHaveBeenCalledTimes(1));
      expect(getWorkouts).toHaveBeenCalledWith({ mesocycleId: "m-1" });
    });

    it("reads no workouts in create mode", async () => {
      // Arrange
      const { getWorkouts, rendered } = renderMesocycle();

      // Act
      await rendered;
      await openedCreateForm();

      // Assert
      expect(getWorkouts).not.toHaveBeenCalled();
    });

    it("collapses a week with workouts, showing how many it holds", async () => {
      // Arrange
      const { rendered } = renderMesocycle({
        search: "id=m-1",
        getWorkouts: async () => ok([legs, pushDay]),
      });

      // Act
      await rendered;
      await openedEditForm();

      // Assert
      const row = await waitFor(() => {
        expect(within(weekRow(1)).getByText("2 entrenamientos")).toBeInTheDocument();
        return weekRow(1);
      });
      expect(within(row).queryByText("Pierna")).not.toBeInTheDocument();
      expect(
        within(row).getByRole("button", { name: "Mostrar entrenamientos de la semana 1" }),
      ).toHaveAttribute("aria-expanded", "false");
    });

    it("expands a week to the create action above a table of its workouts", async () => {
      // Arrange
      const { rendered } = renderMesocycle({
        search: "id=m-1",
        getWorkouts: async () => ok([legs, pushDay]),
      });
      await rendered;
      await openedEditForm();
      const user = userEvent.setup();
      const toggle = await within(weeksCard()).findByRole("button", {
        name: "Mostrar entrenamientos de la semana 1",
      });

      // Act
      await user.click(toggle);

      // Assert
      const row = weekRow(1);
      const table = within(row).getByRole("table");
      const [header, legsRow, pushRow] = within(table).getAllByRole("row");
      expect(header).toHaveTextContent("NombreEjercicios");
      expect(
        within(legsRow!)
          .getAllByRole("cell")
          .map((cell) => cell.textContent),
      ).toEqual(["Pierna", "3", ""]);
      expect(
        within(pushRow!)
          .getAllByRole("cell")
          .map((cell) => cell.textContent),
      ).toEqual(["Empuje", "0", ""]);
      expect(within(row).queryByRole("link", { name: "Pierna" })).toBeNull();
      expect(within(row).getByRole("link", { name: "Editar Pierna" })).toHaveAttribute(
        "href",
        "/workout?id=w-1",
      );
      expect(within(row).getByRole("link", { name: "Editar Empuje" })).toHaveAttribute(
        "href",
        "/workout?id=w-2",
      );
      const create = within(row).getByRole("button", { name: "Nuevo entrenamiento" });
      expect(create.compareDocumentPosition(table) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
      expect(
        within(row).getByRole("button", { name: "Ocultar entrenamientos de la semana 1" }),
      ).toHaveAttribute("aria-expanded", "true");
    });

    it("opens the week dialog from a collapsed week without expanding it", async () => {
      // Arrange
      const { rendered } = renderMesocycle({
        search: "id=m-1",
        getWorkouts: async () => ok([legs]),
      });
      await rendered;
      await openedEditForm();
      await within(weeksCard()).findByText("1 entrenamiento");
      const user = userEvent.setup();

      // Act
      await user.click(within(weekRow(1)).getByRole("button", { name: "Editar semana 1" }));

      // Assert
      expect(await screen.findByRole("dialog", { name: "Semana 1" })).toBeInTheDocument();
      // The open dialog hides the page from assistive technology; `hidden` still reaches the row.
      expect(
        screen.getByRole("button", { name: "Mostrar entrenamientos de la semana 1", hidden: true }),
      ).toHaveAttribute("aria-expanded", "false");
    });

    it("shows the create action in the row of a week without workouts, which does not expand", async () => {
      // Arrange
      const { rendered } = renderMesocycle({
        search: "id=m-1",
        getWorkouts: async () => ok([legs]),
      });

      // Act
      await rendered;
      await openedEditForm();
      await within(weeksCard()).findByText("1 entrenamiento");

      // Assert
      const row = weekRow(3);
      expect(within(row).getByRole("button", { name: "Nuevo entrenamiento" })).toBeInTheDocument();
      expect(within(row).queryByRole("button", { name: /entrenamientos de la semana/ })).toBeNull();
      expect(within(row).queryByText(/^\d+ entrenamientos?$/)).toBeNull();
    });

    it("asks only for a name and a description", async () => {
      // Arrange
      const { rendered } = renderMesocycle({ search: "id=m-1" });
      await rendered;
      await openedEditForm();
      const user = userEvent.setup();
      await within(weekRow(3)).findByRole("button", { name: "Nuevo entrenamiento" });

      // Act
      const dialog = await openWorkoutDialog(user, 3);

      // Assert
      expect(within(dialog).getByRole("textbox", { name: "Nombre" })).toBeInTheDocument();
      expect(within(dialog).getByRole("textbox", { name: "Descripción" })).toBeInTheDocument();
      expect(within(dialog).queryByRole("combobox")).toBeNull();
      expect(within(dialog).queryByText(/Semana del/)).toBeNull();
    });

    it("reports a blank or too long name and sends nothing", async () => {
      // Arrange
      const { createWorkout, rendered } = renderMesocycle({ search: "id=m-1" });
      await rendered;
      await openedEditForm();
      const user = userEvent.setup();
      await within(weekRow(3)).findByRole("button", { name: "Nuevo entrenamiento" });
      const dialog = await openWorkoutDialog(user, 3);
      await user.click(within(dialog).getByRole("button", { name: "Crear" }));
      expect(within(dialog).getByText("Este campo es obligatorio.")).toBeInTheDocument();
      await type(user, within(dialog).getByRole("textbox", { name: "Nombre" }), "a".repeat(257));

      // Act
      await user.click(within(dialog).getByRole("button", { name: "Crear" }));

      // Assert
      expect(within(dialog).getByText("No puede superar los 256 caracteres.")).toBeInTheDocument();
      expect(createWorkout).not.toHaveBeenCalled();
    });

    it("sends a trainer's workout placed in its week, without a trainer or date, and opens it", async () => {
      // Arrange
      const { createWorkout, rendered } = renderMesocycle({ search: "id=m-1" });
      await rendered;
      await openedEditForm();
      const user = userEvent.setup();
      await within(weekRow(3)).findByRole("button", { name: "Nuevo entrenamiento" });
      const dialog = await openWorkoutDialog(user, 3);
      await type(user, within(dialog).getByRole("textbox", { name: "Nombre" }), " Pierna ");

      // Act
      await user.click(within(dialog).getByRole("button", { name: "Crear" }));

      // Assert
      await waitFor(() => expect(push).toHaveBeenCalledWith("/workout?id=w-9"));
      expect(createWorkout).toHaveBeenCalledTimes(1);
      expect(createWorkout).toHaveBeenCalledWith({
        name: "Pierna",
        userId: athleteId,
        mesocycleId: "m-1",
        mesocycleWeekNumber: 3,
      });
    });

    it("sends the mesocycle's trainer when an admin creates a week workout", async () => {
      // Arrange
      const { createWorkout, rendered } = renderMesocycle({
        search: "id=m-1",
        profile: adminProfile,
      });
      await rendered;
      await openedEditForm();
      const user = userEvent.setup();
      await within(weekRow(1)).findByRole("button", { name: "Nuevo entrenamiento" });
      const dialog = await openWorkoutDialog(user, 1);
      await type(user, within(dialog).getByRole("textbox", { name: "Nombre" }), "Empuje");
      await type(user, within(dialog).getByRole("textbox", { name: "Descripción" }), " Día 2 ");

      // Act
      await user.click(within(dialog).getByRole("button", { name: "Crear" }));

      // Assert
      await waitFor(() => expect(createWorkout).toHaveBeenCalledTimes(1));
      expect(createWorkout).toHaveBeenCalledWith({
        name: "Empuje",
        description: "Día 2",
        userId: athleteId,
        trainerId,
        mesocycleId: "m-1",
        mesocycleWeekNumber: 1,
      });
    });

    it("disables Crear while the creation is in progress", async () => {
      // Arrange
      const { rendered } = renderMesocycle({
        search: "id=m-1",
        createWorkout: () => new Promise(() => {}),
      });
      await rendered;
      await openedEditForm();
      const user = userEvent.setup();
      await within(weekRow(3)).findByRole("button", { name: "Nuevo entrenamiento" });
      const dialog = await openWorkoutDialog(user, 3);
      await type(user, within(dialog).getByRole("textbox", { name: "Nombre" }), "Pierna");

      // Act
      await user.click(within(dialog).getByRole("button", { name: "Crear" }));

      // Assert
      expect(within(dialog).getByRole("button", { name: "Crear" })).toBeDisabled();
    });

    it("keeps the dialog with the error when the creation is refused", async () => {
      // Arrange
      const { rendered } = renderMesocycle({
        search: "id=m-1",
        createWorkout: async () => fail(new DomainError("CONFLICT", "")),
      });
      await rendered;
      await openedEditForm();
      const user = userEvent.setup();
      await within(weekRow(3)).findByRole("button", { name: "Nuevo entrenamiento" });
      const dialog = await openWorkoutDialog(user, 3);
      await type(user, within(dialog).getByRole("textbox", { name: "Nombre" }), "Pierna");

      // Act
      await user.click(within(dialog).getByRole("button", { name: "Crear" }));

      // Assert
      expect(
        await within(dialog).findByText("La operación entra en conflicto con el estado actual."),
      ).toBeInTheDocument();
      expect(within(dialog).getByRole("textbox", { name: "Nombre" })).toHaveValue("Pierna");
      expect(push).not.toHaveBeenCalled();
    });

    it("closes the workout dialog on Cancelar without a request", async () => {
      // Arrange
      const { createWorkout, rendered } = renderMesocycle({ search: "id=m-1" });
      await rendered;
      await openedEditForm();
      const user = userEvent.setup();
      await within(weekRow(3)).findByRole("button", { name: "Nuevo entrenamiento" });
      const dialog = await openWorkoutDialog(user, 3);

      // Act
      await user.click(within(dialog).getByRole("button", { name: "Cancelar" }));

      // Assert
      await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
      expect(createWorkout).not.toHaveBeenCalled();
    });

    it("shows a workouts failure with Reintentar, keeps the weeks editable and reads them again", async () => {
      // Arrange
      const getWorkouts = vi
        .fn<() => Answer<WorkoutListItem[]>>()
        .mockResolvedValueOnce(fail(new DomainError("NETWORK", "")))
        .mockResolvedValueOnce(ok([legs]));
      const { rendered } = renderMesocycle({ search: "id=m-1", getWorkouts });
      await rendered;
      await openedEditForm();
      await within(weeksCard()).findByText("No hay conexión con el servidor.");
      expect(within(weeksCard()).queryByRole("button", { name: "Nuevo entrenamiento" })).toBeNull();
      expect(within(weekRow(1)).getByRole("button", { name: "Editar semana 1" })).toBeEnabled();
      const user = userEvent.setup();

      // Act
      await user.click(within(weeksCard()).getByRole("button", { name: "Reintentar" }));

      // Assert
      expect(await within(weekRow(1)).findByText("1 entrenamiento")).toBeInTheDocument();
      expect(getWorkouts).toHaveBeenCalledTimes(2);
    });
  });

  it("follows the active language", async () => {
    // Arrange
    await renderMesocycle().rendered;
    await openedCreateForm();

    // Act
    await act(() => i18n.changeLanguage("en"));

    // Assert
    expect(screen.getByRole("heading", { level: 1, name: "Create mesocycle" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "General information" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Weekly planning" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Add week" })).toBeInTheDocument();
  });

  it("shows the week workouts in the active language", async () => {
    // Arrange
    await renderMesocycle({ search: "id=m-1", getWorkouts: async () => ok([legs]) }).rendered;
    await openedEditForm();
    await within(weeksCard()).findByText("1 entrenamiento");

    // Act
    await act(() => i18n.changeLanguage("en"));

    // Assert
    const card = screen.getByRole("region", { name: "Weekly planning" });
    expect(within(card).getByText("1 workout")).toBeInTheDocument();
    expect(screen.getByRole("region", { name: "Mesocycle people" })).toHaveTextContent("Athlete");
    expect(within(card).getByRole("button", { name: "New workout" })).toBeInTheDocument();
    expect(
      within(card).getByRole("button", { name: "Show the workouts of week 1" }),
    ).toBeInTheDocument();
  });

  it("shows the person cards and the week workouts table in the active language", async () => {
    // Arrange
    const user = userEvent.setup();
    await renderMesocycle({
      search: "id=m-1",
      profile: adminProfile,
      getWorkouts: async () => ok([legs]),
    }).rendered;
    await openedEditForm();
    await user.click(
      await within(weeksCard()).findByRole("button", {
        name: "Mostrar entrenamientos de la semana 1",
      }),
    );

    // Act
    await act(() => i18n.changeLanguage("en"));

    // Assert
    const people = screen.getByRole("region", { name: "Mesocycle people" });
    expect(within(people).getByText("Athlete")).toBeInTheDocument();
    expect(within(people).getByText("Trainer")).toBeInTheDocument();
    const table = screen.getByRole("table");
    expect(within(table).getByRole("columnheader", { name: "Name" })).toBeInTheDocument();
    expect(within(table).getByRole("columnheader", { name: "Exercises" })).toBeInTheDocument();
    expect(within(table).getByRole("link", { name: "Edit Pierna" })).toBeInTheDocument();
  });
});
