import { act, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { DomainError, fail, ok } from "@weight-tracker/core/domain";
import type {
  AddExerciseToWorkoutTemplateInput,
  Exercise,
  Result,
  UpdateWorkoutTemplateInput,
  WorkoutExerciseDetail,
  WorkoutTemplate,
} from "@weight-tracker/core/domain";
import i18n from "@/infrastructure/i18n";
import WorkoutTemplateView from "@/presentation/workouts/views/workout-template-view";
import LoadExerciseCatalog from "../../../support/load-exercise-catalog";
import renderWithProviders, { completeProfile } from "../../../support/render-with-providers";

const replace = vi.fn();
const push = vi.fn();
const navigation = vi.hoisted(() => ({ search: "id=wt-1" }));

vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace, push }),
  usePathname: () => "/workout-template",
  useSearchParams: () => new URLSearchParams(navigation.search),
}));

function catalogExercise(id: string, [es, en]: [string, string]): Exercise {
  return {
    id,
    freeExerciseDbId: null,
    videoUrl: null,
    muscle: "QUADRICEPS",
    bodyPart: "LEGS",
    type: "STRENGTH",
    translations: [
      { languageCode: "es", name: es },
      { languageCode: "en", name: en },
    ],
    ownerId: null,
    archivedAtUtc: null,
  };
}

const squat = catalogExercise("ex-squat", ["Sentadilla", "Squat"]);
const legPress = catalogExercise("ex-press", ["Prensa de piernas", "Leg press"]);
const lunge = catalogExercise("ex-lunge", ["Zancada", "Lunge"]);
const catalog = [squat, legPress, lunge];

function entry(id: string, exerciseId: string): WorkoutExerciseDetail {
  return {
    id,
    exerciseId,
    effort: "HIGH",
    perSide: false,
    restMinutes: null,
    sets: [{ count: 3, target: { type: "Reps", value: 10, maxValue: null } }],
    superSeriesId: null,
    setRecords: [],
  };
}

const template: WorkoutTemplate = {
  id: "wt-1",
  name: "Pierna A",
  description: "Fuerza",
  trainerId: "6a2d3f1e-0e6c-4b64-9d8f-1e7a8d2b3c4d",
  exercises: [entry("te-1", squat.id), entry("te-2", legPress.id)],
};

function renderTemplate({
  getTemplate = async () => ok(template),
  updateTemplate = async (input: UpdateWorkoutTemplateInput) =>
    ok({ ...template, name: input.name ?? template.name }),
  deleteTemplate = async () => ok(undefined),
  catalogLoadedFirst = false,
}: {
  getTemplate?: () => Promise<Result<WorkoutTemplate, DomainError>>;
  updateTemplate?: (
    input: UpdateWorkoutTemplateInput,
  ) => Promise<Result<WorkoutTemplate, DomainError>>;
  deleteTemplate?: () => Promise<Result<void, DomainError>>;
  /** Opens the screen after the session's catalog was read, as after sign-in. */
  catalogLoadedFirst?: boolean;
} = {}) {
  const readTemplate = vi.fn(getTemplate);
  const readCatalog = vi.fn(async () => ok(catalog));
  const readFavorites = vi.fn(async () => ok([]));
  const mutations = {
    update: vi.fn(updateTemplate),
    delete: vi.fn(deleteTemplate),
    addExercise: vi.fn(async (input: AddExerciseToWorkoutTemplateInput) =>
      ok({ ...entry("te-new", input.exerciseId ?? ""), effort: input.effort ?? "" }),
    ),
    updateExercise: vi.fn(async () => ok(entry("te-1", squat.id))),
    removeExercise: vi.fn(async () => ok(undefined)),
    reorderExercises: vi.fn(async () => ok(template)),
    createSuperSeries: vi.fn(async () => ok({ id: 7 })),
    breakSuperSeries: vi.fn(async () => ok(undefined)),
  };
  // Every workout use case the shared editor could reach: none may be called on a template.
  const workoutMutations = {
    update: vi.fn(),
    addExercise: vi.fn(),
    updateExercise: vi.fn(),
    removeExercise: vi.fn(),
    reorderExercises: vi.fn(),
    createSuperSeries: vi.fn(),
    breakSuperSeries: vi.fn(),
  };

  return {
    readTemplate,
    readCatalog,
    readFavorites,
    mutations,
    workoutMutations,
    rendered: renderWithProviders(
      catalogLoadedFirst ? (
        <LoadExerciseCatalog>
          <WorkoutTemplateView />
        </LoadExerciseCatalog>
      ) : (
        <WorkoutTemplateView />
      ),
      {
        getSessionUseCase: () => ({
          execute: async () => ok({ accessToken: "a", refreshToken: "r" }),
        }),
        getMyProfileUseCase: () => ({ execute: async () => ok(completeProfile) }),
        getWorkoutTemplateByIdUseCase: () => ({ execute: readTemplate }),
        getExercisesUseCase: () => ({ execute: readCatalog }),
        getExerciseFavoritesUseCase: () => ({ execute: readFavorites }),
        updateWorkoutTemplateUseCase: () => ({ execute: mutations.update }),
        deleteWorkoutTemplateUseCase: () => ({ execute: mutations.delete }),
        addExerciseToWorkoutTemplateUseCase: () => ({ execute: mutations.addExercise }),
        updateWorkoutTemplateExerciseUseCase: () => ({ execute: mutations.updateExercise }),
        removeExerciseFromWorkoutTemplateUseCase: () => ({ execute: mutations.removeExercise }),
        reorderWorkoutTemplateExercisesUseCase: () => ({ execute: mutations.reorderExercises }),
        createWorkoutTemplateSuperSeriesUseCase: () => ({ execute: mutations.createSuperSeries }),
        breakWorkoutTemplateSuperSeriesUseCase: () => ({ execute: mutations.breakSuperSeries }),
        updateWorkoutUseCase: () => ({ execute: workoutMutations.update }),
        addExerciseToWorkoutUseCase: () => ({ execute: workoutMutations.addExercise }),
        updateWorkoutExerciseUseCase: () => ({ execute: workoutMutations.updateExercise }),
        removeExerciseFromWorkoutUseCase: () => ({ execute: workoutMutations.removeExercise }),
        reorderWorkoutExercisesUseCase: () => ({ execute: workoutMutations.reorderExercises }),
        createSuperSeriesUseCase: () => ({ execute: workoutMutations.createSuperSeries }),
        breakSuperSeriesUseCase: () => ({ execute: workoutMutations.breakSuperSeries }),
      },
    ),
  };
}

const propertiesSection = () => screen.getByRole("region", { name: "Propiedades de la plantilla" });
const exercisesSection = () => screen.getByRole("region", { name: "Ejercicios" });
const configurationDialog = () =>
  screen.getByRole("dialog", { name: "Configuración del ejercicio" });
const deleteDialog = () => screen.queryByRole("dialog", { name: "¿Eliminar esta plantilla?" });

async function loaded() {
  await screen.findByRole("region", { name: "Ejercicios" });
}

function workoutButtons(name: string) {
  return within(exercisesSection()).getAllByRole("button", { name });
}

function exerciseItem(name: string) {
  return within(exercisesSection())
    .getAllByRole("listitem")
    .find(
      (item) =>
        within(item).queryByRole("list") === null && within(item).queryByText(name) !== null,
    )!;
}

/** jsdom has no layout: each list item gets its own cell, in rows of three, so the keyboard sensor can move. */
function stubListItemRects() {
  return vi.spyOn(HTMLElement.prototype, "getBoundingClientRect").mockImplementation(function (
    this: HTMLElement,
  ) {
    const index =
      this.tagName === "LI" && this.parentElement
        ? Array.from(this.parentElement.children).indexOf(this)
        : 0;
    const left = (index % 3) * 310;
    const top = Math.floor(index / 3) * 160;
    return {
      x: left,
      y: top,
      top,
      left,
      right: left + 300,
      bottom: top + 150,
      width: 300,
      height: 150,
      toJSON: () => ({}),
    };
  });
}

function workoutCalls(workoutMutations: ReturnType<typeof renderTemplate>["workoutMutations"]) {
  return Object.values(workoutMutations).reduce((total, mock) => total + mock.mock.calls.length, 0);
}

describe("WorkoutTemplateView", () => {
  beforeEach(() => {
    replace.mockClear();
    push.mockClear();
    navigation.search = "id=wt-1";
  });

  it("reads the template, the catalog and the favorites once on opening", async () => {
    // Arrange
    const { readTemplate, readCatalog, readFavorites, rendered } = renderTemplate();

    // Act
    await rendered;
    await loaded();

    // Assert
    expect(readTemplate).toHaveBeenCalledTimes(1);
    expect(readTemplate).toHaveBeenCalledWith({ id: "wt-1" });
    expect(readCatalog).toHaveBeenCalledTimes(1);
    expect(readFavorites).toHaveBeenCalledTimes(1);
  });

  it("uses the session's catalog and does not read it on opening once it was read", async () => {
    // Arrange
    const { readTemplate, readCatalog, readFavorites, rendered } = renderTemplate({
      catalogLoadedFirst: true,
    });

    // Act
    await rendered;
    await loaded();

    // Assert
    expect(readTemplate).toHaveBeenCalledTimes(1);
    expect(readCatalog).toHaveBeenCalledTimes(1);
    expect(readFavorites).toHaveBeenCalledTimes(1);
  });

  it("shows the heading, the way back, the delete action and no workout context", async () => {
    // Arrange & Act
    await renderTemplate().rendered;
    await loaded();

    // Assert
    expect(screen.getByRole("heading", { name: "Editar plantilla" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Volver" })).toHaveAttribute(
      "href",
      "/workout-templates",
    );
    expect(screen.getByRole("button", { name: "Eliminar plantilla" })).toBeInTheDocument();
    expect(screen.queryByRole("group", { name: "Semana" })).not.toBeInTheDocument();
    // The header shows the signed-in user's own role; the page itself shows no person.
    const page = screen.getByRole("main");
    expect(within(page).queryByText("Atleta")).not.toBeInTheDocument();
    expect(within(page).queryByText("Entrenador")).not.toBeInTheDocument();
  });

  it("fills the properties card with the template's name and description and its save", async () => {
    // Arrange & Act
    await renderTemplate().rendered;
    await loaded();

    // Assert
    const properties = propertiesSection();
    expect(within(properties).getByRole("textbox", { name: "Nombre" })).toHaveValue("Pierna A");
    expect(within(properties).getByRole("textbox", { name: "Descripción" })).toHaveValue("Fuerza");
    expect(within(properties).getByRole("button", { name: "Guardar" })).toBeInTheDocument();
  });

  it("reports a blank name and sends nothing", async () => {
    // Arrange
    const { mutations, rendered } = renderTemplate();
    await rendered;
    await loaded();
    const user = userEvent.setup();
    await user.clear(within(propertiesSection()).getByRole("textbox", { name: "Nombre" }));

    // Act
    await user.click(within(propertiesSection()).getByRole("button", { name: "Guardar" }));

    // Assert
    expect(screen.getByText("Este campo es obligatorio.")).toBeInTheDocument();
    expect(mutations.update).not.toHaveBeenCalled();
  });

  it("saves the trimmed name and description once and confirms it", async () => {
    // Arrange
    const { mutations, workoutMutations, rendered } = renderTemplate();
    await rendered;
    await loaded();
    const user = userEvent.setup();
    const name = within(propertiesSection()).getByRole("textbox", { name: "Nombre" });
    await user.clear(name);
    await user.type(name, " Pierna B ");
    await user.clear(within(propertiesSection()).getByRole("textbox", { name: "Descripción" }));

    // Act
    await user.click(within(propertiesSection()).getByRole("button", { name: "Guardar" }));

    // Assert
    expect(await screen.findByText("Plantilla guardada.")).toBeInTheDocument();
    expect(mutations.update).toHaveBeenCalledTimes(1);
    expect(mutations.update).toHaveBeenCalledWith({ id: "wt-1", name: "Pierna B" });
    expect(workoutCalls(workoutMutations)).toBe(0);
  });

  it("shows the error and keeps the typed values when the save is refused", async () => {
    // Arrange
    await renderTemplate({ updateTemplate: async () => fail(new DomainError("FORBIDDEN", "")) })
      .rendered;
    await loaded();
    const user = userEvent.setup();
    const name = within(propertiesSection()).getByRole("textbox", { name: "Nombre" });
    await user.type(name, " B");

    // Act
    await user.click(within(propertiesSection()).getByRole("button", { name: "Guardar" }));

    // Assert
    expect(await screen.findByRole("alert")).toBeInTheDocument();
    expect(name).toHaveValue("Pierna A B");
    expect(screen.queryByText("Plantilla guardada.")).not.toBeInTheDocument();
  });

  it("adds a chosen exercise to the template, not to a workout, and appends it", async () => {
    // Arrange
    const { mutations, workoutMutations, rendered } = renderTemplate();
    await rendered;
    await loaded();
    const user = userEvent.setup();
    await user.click(within(exercisesSection()).getByRole("button", { name: "Añadir ejercicio" }));
    const catalogDialog = await screen.findByRole("dialog", { name: "Catálogo de ejercicios" });
    const lungeCard = within(catalogDialog)
      .getAllByRole("listitem")
      .find((item) => within(item).queryByText("Zancada") !== null)!;
    await user.click(within(lungeCard).getByRole("button", { name: "Elegir ejercicio" }));
    await screen.findByRole("dialog", { name: "Configuración del ejercicio" });
    await user.click(within(configurationDialog()).getByRole("combobox", { name: "Esfuerzo" }));
    await user.click(screen.getByRole("option", { name: "RIR 0" }));
    const form = within(configurationDialog()).getByRole("group", { name: "Añadir serie" });
    await user.type(within(form).getByRole("spinbutton", { name: "Series" }), "2");
    await user.type(within(form).getByRole("spinbutton", { name: "Valor" }), "15");
    await user.click(within(configurationDialog()).getByRole("button", { name: "Añadir serie" }));

    // Act
    await user.click(within(configurationDialog()).getByRole("button", { name: "Guardar" }));

    // Assert
    await waitFor(() => expect(mutations.addExercise).toHaveBeenCalledTimes(1));
    expect(mutations.addExercise).toHaveBeenCalledWith({
      templateId: "wt-1",
      exerciseId: "ex-lunge",
      effort: "HIGH",
      sets: [{ count: 2, targetType: "Reps", target: 15 }],
      perSide: false,
    });
    expect(await within(exercisesSection()).findByText("Zancada")).toBeInTheDocument();
    expect(workoutCalls(workoutMutations)).toBe(0);
  });

  it("removes an exercise from the template", async () => {
    // Arrange
    const { mutations, workoutMutations, rendered } = renderTemplate();
    await rendered;
    await loaded();
    const user = userEvent.setup();

    // Act
    await user.click(workoutButtons("Quitar ejercicio")[0]!);

    // Assert
    await waitFor(() => expect(mutations.removeExercise).toHaveBeenCalledTimes(1));
    expect(mutations.removeExercise).toHaveBeenCalledWith({
      templateId: "wt-1",
      exerciseId: "ex-squat",
    });
    expect(workoutCalls(workoutMutations)).toBe(0);
  });

  describe("reordering", () => {
    let rects: ReturnType<typeof stubListItemRects>;
    beforeEach(() => {
      rects = stubListItemRects();
    });
    afterEach(() => {
      rects.mockRestore();
    });

    it("sends the template's new order with the catalog ids", async () => {
      // Arrange
      const { mutations, workoutMutations, rendered } = renderTemplate();
      await rendered;
      await loaded();
      const user = userEvent.setup();
      workoutButtons("Reordenar ejercicio")[0]!.focus();

      // Act
      await user.keyboard("[Space]");
      await user.keyboard("[ArrowRight]");
      await user.keyboard("[Space]");

      // Assert
      await waitFor(() => expect(mutations.reorderExercises).toHaveBeenCalledTimes(1));
      expect(mutations.reorderExercises).toHaveBeenCalledWith({
        templateId: "wt-1",
        exerciseIds: ["ex-press", "ex-squat"],
      });
      expect(workoutCalls(workoutMutations)).toBe(0);
    });
  });

  it("groups the checked exercises in a superseries of the template", async () => {
    // Arrange
    const { mutations, workoutMutations, rendered } = renderTemplate();
    await rendered;
    await loaded();
    const user = userEvent.setup();
    for (const name of ["Sentadilla", "Prensa de piernas"]) {
      await user.click(
        within(exerciseItem(name)).getByRole("checkbox", { name: "Seleccionar para superserie" }),
      );
    }

    // Act
    await user.click(
      within(exercisesSection()).getByRole("button", { name: "Agrupar en superserie" }),
    );

    // Assert
    await waitFor(() => expect(mutations.createSuperSeries).toHaveBeenCalledTimes(1));
    expect(mutations.createSuperSeries).toHaveBeenCalledWith({
      templateId: "wt-1",
      exerciseIds: ["te-1", "te-2"],
    });
    expect(workoutCalls(workoutMutations)).toBe(0);
  });

  it("breaks a superseries of the template on confirmation", async () => {
    // Arrange
    const grouped: WorkoutTemplate = {
      ...template,
      exercises: template.exercises.map((item) => ({ ...item, superSeriesId: 1 })),
    };
    const { mutations, rendered } = renderTemplate({ getTemplate: async () => ok(grouped) });
    await rendered;
    await loaded();
    const user = userEvent.setup();
    await user.click(workoutButtons("Romper superserie")[0]!);
    const dialog = await screen.findByRole("dialog", { name: "¿Romper superserie?" });

    // Act
    await user.click(within(dialog).getByRole("button", { name: "Romper" }));

    // Assert
    await waitFor(() => expect(mutations.breakSuperSeries).toHaveBeenCalledTimes(1));
    expect(mutations.breakSuperSeries).toHaveBeenCalledWith({
      templateId: "wt-1",
      superSeriesId: 1,
    });
  });

  it("deletes the template on confirmation and goes back to the list", async () => {
    // Arrange
    const { mutations, rendered } = renderTemplate();
    await rendered;
    await loaded();
    const user = userEvent.setup();
    await user.click(screen.getByRole("button", { name: "Eliminar plantilla" }));
    await screen.findByRole("dialog", { name: "¿Eliminar esta plantilla?" });

    // Act
    await user.click(within(deleteDialog()!).getByRole("button", { name: "Eliminar" }));

    // Assert
    await waitFor(() => expect(push).toHaveBeenCalledWith("/workout-templates"));
    expect(mutations.delete).toHaveBeenCalledTimes(1);
    expect(mutations.delete).toHaveBeenCalledWith({ id: "wt-1" });
  });

  it("closes the confirmation on Cancelar without deleting", async () => {
    // Arrange
    const { mutations, rendered } = renderTemplate();
    await rendered;
    await loaded();
    const user = userEvent.setup();
    await user.click(screen.getByRole("button", { name: "Eliminar plantilla" }));
    await screen.findByRole("dialog", { name: "¿Eliminar esta plantilla?" });

    // Act
    await user.click(within(deleteDialog()!).getByRole("button", { name: "Cancelar" }));

    // Assert
    await waitFor(() => expect(deleteDialog()).not.toBeInTheDocument());
    expect(mutations.delete).not.toHaveBeenCalled();
    expect(screen.getByRole("heading", { name: "Editar plantilla" })).toBeInTheDocument();
  });

  it("stays on the template and shows the error when the deletion is refused", async () => {
    // Arrange
    const { rendered } = renderTemplate({
      deleteTemplate: async () => fail(new DomainError("FORBIDDEN", "")),
    });
    await rendered;
    await loaded();
    const user = userEvent.setup();
    await user.click(screen.getByRole("button", { name: "Eliminar plantilla" }));
    await screen.findByRole("dialog", { name: "¿Eliminar esta plantilla?" });

    // Act
    await user.click(within(deleteDialog()!).getByRole("button", { name: "Eliminar" }));

    // Assert
    expect(await screen.findByRole("alert")).toBeInTheDocument();
    await waitFor(() => expect(deleteDialog()).not.toBeInTheDocument());
    expect(push).not.toHaveBeenCalled();
  });

  it("shows the not-found state without reading when there is no id", async () => {
    // Arrange
    navigation.search = "";
    const { readTemplate, rendered } = renderTemplate();

    // Act
    await rendered;

    // Assert
    expect(await screen.findByText("No hemos encontrado esta plantilla.")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Volver a plantillas" })).toHaveAttribute(
      "href",
      "/workout-templates",
    );
    expect(readTemplate).not.toHaveBeenCalled();
  });

  it("shows the not-found state when the API does not find the template", async () => {
    // Arrange & Act
    await renderTemplate({ getTemplate: async () => fail(new DomainError("NOT_FOUND", "")) })
      .rendered;

    // Assert
    expect(await screen.findByText("No hemos encontrado esta plantilla.")).toBeInTheDocument();
  });

  it("shows the skeleton while the template loads", async () => {
    // Arrange & Act
    await renderTemplate({ getTemplate: () => new Promise(() => {}) }).rendered;

    // Assert
    expect(await screen.findByLabelText("Cargando")).toBeInTheDocument();
  });

  it("follows the active language", async () => {
    // Arrange
    await renderTemplate().rendered;
    await loaded();

    // Act
    await act(() => i18n.changeLanguage("en"));

    // Assert
    expect(screen.getByRole("heading", { name: "Edit template" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Delete template" })).toBeInTheDocument();
  });
});
