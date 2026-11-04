import { act, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { DomainError, fail, ok } from "@weight-tracker/core/domain";
import type {
  CreateExerciseRequest,
  Exercise,
  ExerciseFavorite,
  Result,
  UserProfile,
} from "@weight-tracker/core/domain";
import i18n from "@/infrastructure/i18n";
import ExerciseCatalogView from "@/presentation/workouts/views/exercise-catalog-view";
import renderWithProviders, { completeProfile } from "../../../support/render-with-providers";

const replace = vi.fn();
const push = vi.fn();

vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace, push }),
  usePathname: () => "/exercises",
}));

const adminProfile: UserProfile = {
  ...completeProfile,
  id: "9c8b7a6d-5e4f-4a3b-8c2d-1e0f9a8b7c6d",
  email: "admin@example.com",
  role: "ADMIN",
};

function catalogExercise(
  id: string,
  muscle: string,
  bodyPart: string,
  [es, en]: [string, string],
  ownerId: string | null = null,
  videoUrl: string | null = null,
): Exercise {
  return {
    id,
    freeExerciseDbId: null,
    videoUrl,
    muscle,
    bodyPart,
    type: "STRENGTH",
    translations: [
      { languageCode: "es", name: es },
      { languageCode: "en", name: en },
    ],
    ownerId,
    archivedAtUtc: null,
  };
}

const otherUserId = "6a7b8c9d-0e1f-4a2b-8c3d-4e5f6a7b8c9d";
const squat = catalogExercise("ex-squat", "QUADRICEPS", "LEGS", ["Sentadilla", "Squat"]);
const bench = catalogExercise("ex-bench", "CHEST", "CHEST", ["Press de banca", "Bench press"]);
const mine = catalogExercise(
  "ex-mine",
  "BICEPS",
  "ARMS",
  ["Mi curl", "My curl"],
  completeProfile.id,
);
const theirs = catalogExercise(
  "ex-theirs",
  "TRICEPS",
  "ARMS",
  ["Fondos de otro", "Their dips"],
  otherUserId,
);
const catalog = [squat, bench, mine, theirs];

function favoriteOf(exerciseId: string): ExerciseFavorite {
  return { exerciseId, createdAtUtc: "2026-11-03T09:30:00Z" };
}

const favoritesOf =
  (...ids: string[]) =>
  async () =>
    ok(ids.map(favoriteOf));

/** The exercise the API would answer a creation with: owned by the signed-in user, with the sent translation. */
function createdFrom(request: CreateExerciseRequest, ownerId: string): Exercise {
  return {
    id: "ex-new",
    freeExerciseDbId: null,
    videoUrl: request.videoUrl ?? null,
    muscle: request.muscle ?? "",
    bodyPart: "BACK",
    type: request.type ?? "",
    translations: [{ languageCode: request.languageCode ?? "es", name: request.name ?? "" }],
    ownerId,
    archivedAtUtc: null,
  };
}

const rowRequest: CreateExerciseRequest = {
  languageCode: "es",
  name: "Remo",
  muscle: "MIDDLE_BACK",
  type: "STRENGTH",
};

function renderCatalog({
  getExercises = async () => ok(catalog),
  getFavorites = favoritesOf(),
  markFavorite = async (exerciseId: string) => ok(favoriteOf(exerciseId)),
  unmarkFavorite = async () => ok(undefined as void),
  profile = completeProfile,
  createExercise = async (request: CreateExerciseRequest) => ok(createdFrom(request, profile.id)),
}: {
  getExercises?: () => Promise<Result<Exercise[], DomainError>>;
  getFavorites?: () => Promise<Result<ExerciseFavorite[], DomainError>>;
  markFavorite?: (exerciseId: string) => Promise<Result<ExerciseFavorite, DomainError>>;
  unmarkFavorite?: (exerciseId: string) => Promise<Result<void, DomainError>>;
  profile?: UserProfile;
  createExercise?: (request: CreateExerciseRequest) => Promise<Result<Exercise, DomainError>>;
} = {}) {
  const calls = {
    catalog: vi.fn(getExercises),
    favorites: vi.fn(getFavorites),
    mark: vi.fn(markFavorite),
    unmark: vi.fn(unmarkFavorite),
    create: vi.fn(createExercise),
  };

  return {
    ...calls,
    rendered: renderWithProviders(<ExerciseCatalogView />, {
      getSessionUseCase: () => ({
        execute: async () => ok({ accessToken: "a", refreshToken: "r" }),
      }),
      getMyProfileUseCase: () => ({ execute: async () => ok(profile) }),
      getExercisesUseCase: () => ({ execute: calls.catalog }),
      getExerciseFavoritesUseCase: () => ({ execute: calls.favorites }),
      markExerciseFavoriteUseCase: () => ({ execute: calls.mark }),
      unmarkExerciseFavoriteUseCase: () => ({ execute: calls.unmark }),
      createExerciseUseCase: () => ({ execute: calls.create }),
    }),
  };
}

type User = ReturnType<typeof userEvent.setup>;

const heading = () => screen.getByRole("heading", { name: "Catálogo de ejercicios", level: 1 });
const addButton = () => screen.queryByRole("button", { name: "Añadir ejercicio" });
const sectionList = () => screen.getByRole("list", { name: "Secciones" });
const section = (name: string) => within(sectionList()).getByRole("button", { name });
const cardList = () =>
  screen.queryByRole("list", { name: /^(Catálogo de ejercicios|Exercise catalog)$/ });
const cards = () => (cardList() ? within(cardList()!).queryAllByRole("listitem") : []);
const names = () => cards().map((card) => within(card).getAllByText(/./)[0]?.textContent);
const card = (name: string) => cards().find((item) => within(item).queryByText(name) !== null)!;
const createDialog = () => screen.getByRole("dialog", { name: "Nuevo ejercicio" });

async function loaded() {
  await screen.findByRole("list", { name: "Secciones" });
}

async function openCreate(user: User) {
  await user.click(screen.getByRole("button", { name: "Añadir ejercicio" }));
  await screen.findByRole("dialog", { name: "Nuevo ejercicio" });
}

/** Fills the open dialog; the link is left alone unless one is given. */
async function fillExercise(user: User, name: string, muscle: string, type: string, link = "") {
  const dialog = createDialog();

  await user.type(within(dialog).getByRole("textbox", { name: "Nombre" }), name);
  await user.click(within(dialog).getByRole("combobox", { name: "Músculo" }));
  await user.click(await screen.findByRole("option", { name: muscle }));
  await user.click(within(dialog).getByRole("combobox", { name: "Tipo" }));
  await user.click(await screen.findByRole("option", { name: type }));
  if (link) {
    await user.type(within(dialog).getByRole("textbox", { name: "Enlace de vídeo" }), link);
  }
}

const submit = (user: User) =>
  user.click(within(createDialog()).getByRole("button", { name: "Crear" }));

describe("ExerciseCatalogView", () => {
  beforeEach(() => {
    replace.mockClear();
    push.mockClear();
  });

  afterEach(async () => {
    await act(() => i18n.changeLanguage("es"));
  });

  describe("catalog", () => {
    it("lists the whole catalog under Todos with the heading and the sections", async () => {
      // Arrange & Act
      await renderCatalog().rendered;

      // Assert
      await loaded();
      expect(heading()).toBeInTheDocument();
      expect(
        within(sectionList())
          .getAllByRole("button")
          .map((button) => button.textContent),
      ).toEqual(["Todos", "Mis ejercicios", "Favoritos"]);
      expect(section("Todos")).toHaveAttribute("aria-current", "true");
      expect(names()).toEqual(["Sentadilla", "Press de banca", "Mi curl", "Fondos de otro"]);
    });

    it("reads the catalog and the favorites once", async () => {
      // Arrange & Act
      const { catalog: readCatalog, favorites, rendered } = renderCatalog();
      await rendered;

      // Assert
      await loaded();
      expect(readCatalog).toHaveBeenCalledTimes(1);
      expect(favorites).toHaveBeenCalledTimes(1);
    });

    describe("finder", () => {
      const curlBiceps = catalogExercise("ex-curl-biceps", "BICEPS", "ARMS", [
        "Curl de bíceps",
        "Biceps curl",
      ]);
      const curlPolea = catalogExercise("ex-curl-polea", "BICEPS", "ARMS", [
        "Curl en polea",
        "Cable curl",
      ]);
      const poleaTriceps = catalogExercise("ex-polea-triceps", "TRICEPS", "ARMS", [
        "Extensión de tríceps en polea",
        "Cable pushdown",
      ]);
      const finderCatalog = async () => ok([squat, curlBiceps, curlPolea, poleaTriceps]);

      async function find(text: string, language = "es") {
        await renderCatalog({ getExercises: finderCatalog }).rendered;
        await loaded();
        await act(() => i18n.changeLanguage(language));
        await userEvent.setup().type(
          screen.getByRole("searchbox", {
            name: language === "en" ? "Search exercise" : "Buscar ejercicio",
          }),
          text,
        );
        return names();
      }

      it("finds an exercise when each typed word is in its name, though the name has more words", async () => {
        // Arrange & Act
        const found = await find("Curl bíceps");

        // Assert
        expect(found).toEqual(["Curl de bíceps"]);
      });

      it("lists the exercises that contain all the typed words at the same time", async () => {
        // Arrange & Act
        const found = await find("Curl Polea");

        // Assert
        expect(found).toEqual(["Curl en polea"]);
      });

      it("finds the words in any order, ignoring case, accents and extra spaces", async () => {
        // Arrange & Act
        const found = await find("  POLEA   tríceps ");

        // Assert
        expect(found).toEqual(["Extensión de tríceps en polea"]);
      });

      it("finds a word that is only part of a longer one", async () => {
        // Arrange & Act
        const found = await find("sentad");

        // Assert
        expect(found).toEqual(["Sentadilla"]);
      });

      it("finds the Spanish exercise by its English name when Spanish is selected", async () => {
        // Arrange & Act
        const found = await find("Squat");

        // Assert
        expect(found).toEqual(["Sentadilla"]);
      });

      it("finds across the Spanish and the English name", async () => {
        // Arrange & Act
        const found = await find("sentadilla squat");

        // Assert
        expect(found).toEqual(["Sentadilla"]);
      });

      it("finds by the English name only when English is selected", async () => {
        // Arrange & Act
        const found = await find("Cable curl", "en");

        // Assert
        expect(found).toEqual(["Cable curl"]);
      });

      it("does not find by the Spanish name when English is selected", async () => {
        // Arrange & Act
        const found = await find("Sentadilla", "en");

        // Assert
        expect(found).toEqual([]);
      });

      it("shows every exercise for a text of only spaces", async () => {
        // Arrange & Act
        const found = await find("   ");

        // Assert
        expect(found).toHaveLength(4);
      });
    });

    it("narrows by section and by text without any request", async () => {
      // Arrange
      const { catalog: readCatalog, favorites, rendered } = renderCatalog();
      await rendered;
      await loaded();
      const user = userEvent.setup();

      // Act
      await user.click(section("Mis ejercicios"));
      await user.type(screen.getByRole("searchbox", { name: "Buscar ejercicio" }), "curl");

      // Assert
      expect(names()).toEqual(["Mi curl"]);
      expect(readCatalog).toHaveBeenCalledTimes(1);
      expect(favorites).toHaveBeenCalledTimes(1);
    });

    it("lists only the exercises the user owns under Mis ejercicios", async () => {
      // Arrange
      await renderCatalog().rendered;
      await loaded();

      // Act
      await userEvent.setup().click(section("Mis ejercicios"));

      // Assert
      expect(names()).toEqual(["Mi curl"]);
    });

    it("says so when nothing matches", async () => {
      // Arrange
      await renderCatalog().rendered;
      await loaded();

      // Act
      await userEvent
        .setup()
        .type(screen.getByRole("searchbox", { name: "Buscar ejercicio" }), "zzz");

      // Assert
      expect(screen.getByText("Ningún ejercicio coincide con el filtro.")).toBeInTheDocument();
      expect(cards()).toHaveLength(0);
    });

    it("shows the failure with a retry, and no exercise and no add button", async () => {
      // Arrange & Act
      await renderCatalog({
        getExercises: async () => fail(new DomainError("NETWORK", "")),
      }).rendered;

      // Assert
      expect(await screen.findByText("No hay conexión con el servidor.")).toBeInTheDocument();
      expect(screen.getByRole("button", { name: "Reintentar" })).toBeInTheDocument();
      expect(cards()).toHaveLength(0);
      expect(addButton()).not.toBeInTheDocument();
    });

    it("reads the catalog again on retry", async () => {
      // Arrange
      const getExercises = vi
        .fn<() => Promise<Result<Exercise[], DomainError>>>()
        .mockResolvedValueOnce(fail(new DomainError("NETWORK", "")))
        .mockResolvedValue(ok(catalog));
      await renderCatalog({ getExercises }).rendered;
      await screen.findByText("No hay conexión con el servidor.");

      // Act
      await userEvent.setup().click(screen.getByRole("button", { name: "Reintentar" }));

      // Assert
      await loaded();
      expect(names()).toHaveLength(4);
    });

    it("shows an admin the same sections, filters and cards", async () => {
      // Arrange & Act
      await renderCatalog({ profile: adminProfile }).rendered;

      // Assert
      await loaded();
      expect(heading()).toBeInTheDocument();
      expect(names()).toEqual(["Sentadilla", "Press de banca", "Mi curl", "Fondos de otro"]);
      expect(screen.getByRole("searchbox", { name: "Buscar ejercicio" })).toBeInTheDocument();
    });

    it("lists nothing under Mis ejercicios for an admin, who owns no exercise", async () => {
      // Arrange
      await renderCatalog({ profile: adminProfile }).rendered;
      await loaded();

      // Act
      await userEvent.setup().click(section("Mis ejercicios"));

      // Assert
      expect(screen.getByText("Ningún ejercicio coincide con el filtro.")).toBeInTheDocument();
    });
  });

  describe("english name", () => {
    const onlySpanish: Exercise = {
      ...catalogExercise("ex-remo", "MIDDLE_BACK", "BACK", ["Remo con banda", ""]),
      translations: [{ languageCode: "es", name: "Remo con banda" }],
    };
    const sameName = catalogExercise("ex-plank", "ABDOMINALS", "CORE", ["Plank", "Plank"]);
    const withEnglish = async () => ok([squat, onlySpanish, sameName]);
    const subtitleOf = (name: string) => {
      const texts = within(card(name)).getAllByText(/./);
      return texts.length > 1 ? texts[1]?.textContent : undefined;
    };

    it("shows the English name under the Spanish one", async () => {
      // Arrange & Act
      await renderCatalog({ getExercises: withEnglish }).rendered;

      // Assert
      await loaded();
      expect(within(card("Sentadilla")).getByText("Squat")).toBeInTheDocument();
      expect(subtitleOf("Sentadilla")).toBe("Squat");
    });

    it("shows nothing under an exercise with no English name or with the same name", async () => {
      // Arrange & Act
      await renderCatalog({ getExercises: withEnglish }).rendered;

      // Assert
      await loaded();
      expect(within(card("Remo con banda")).getAllByText("Remo con banda")).toHaveLength(1);
      expect(within(card("Plank")).getAllByText("Plank")).toHaveLength(1);
    });

    it("shows the English name once, as the name, when English is selected", async () => {
      // Arrange
      await renderCatalog({ getExercises: withEnglish }).rendered;
      await loaded();

      // Act
      await act(() => i18n.changeLanguage("en"));

      // Assert
      expect(within(card("Squat")).getAllByText("Squat")).toHaveLength(1);
      expect(screen.queryByText("Sentadilla")).not.toBeInTheDocument();
    });
  });

  describe("video", () => {
    const youTube = "https://www.youtube.com/watch?v=h_yZV27H684";
    const withVideo = catalogExercise(
      "ex-v",
      "QUADRICEPS",
      "LEGS",
      ["Prensa", "Leg press"],
      null,
      youTube,
    );
    const otherVideo = catalogExercise(
      "ex-o",
      "CHEST",
      "CHEST",
      ["Aperturas", "Flyes"],
      null,
      "https://example.com/flyes.mp4",
    );
    const videoCatalog = async () => ok([withVideo, otherVideo, bench]);

    it("shows Ver vídeo only on the exercises that have a video", async () => {
      // Arrange & Act
      await renderCatalog({ getExercises: videoCatalog }).rendered;

      // Assert
      await loaded();
      expect(within(card("Prensa")).getByRole("button", { name: "Ver vídeo" })).toBeVisible();
      expect(within(card("Aperturas")).getByRole("button", { name: "Ver vídeo" })).toBeVisible();
      expect(
        within(card("Press de banca")).queryByRole("button", { name: "Ver vídeo" }),
      ).toBeNull();
    });

    it("opens a dialog with the embedded video, titled with the exercise", async () => {
      // Arrange
      await renderCatalog({ getExercises: videoCatalog }).rendered;
      await loaded();

      // Act
      await userEvent
        .setup()
        .click(within(card("Prensa")).getByRole("button", { name: "Ver vídeo" }));

      // Assert
      const dialog = await screen.findByRole("dialog", { name: "Prensa" });
      expect(within(dialog).getByTitle("Prensa")).toHaveAttribute(
        "src",
        "https://www.youtube-nocookie.com/embed/h_yZV27H684",
      );
    });

    it("offers a link in the dialog for a video that is not on YouTube", async () => {
      // Arrange
      await renderCatalog({ getExercises: videoCatalog }).rendered;
      await loaded();

      // Act
      await userEvent
        .setup()
        .click(within(card("Aperturas")).getByRole("button", { name: "Ver vídeo" }));

      // Assert
      const dialog = await screen.findByRole("dialog", { name: "Aperturas" });
      expect(within(dialog).getByRole("link")).toHaveAttribute(
        "href",
        "https://example.com/flyes.mp4",
      );
    });

    it("closes with Cerrar and removes the video", async () => {
      // Arrange
      await renderCatalog({ getExercises: videoCatalog }).rendered;
      await loaded();
      const user = userEvent.setup();
      await user.click(within(card("Prensa")).getByRole("button", { name: "Ver vídeo" }));
      const dialog = await screen.findByRole("dialog", { name: "Prensa" });

      // Act
      await user.click(within(dialog).getByRole("button", { name: "Cerrar" }));

      // Assert
      await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
      expect(screen.queryByTitle("Prensa")).not.toBeInTheDocument();
    });

    it("shows the dialog's title in the language of the screen", async () => {
      // Arrange
      await renderCatalog({ getExercises: videoCatalog }).rendered;
      await loaded();
      await act(() => i18n.changeLanguage("en"));

      // Act
      await userEvent
        .setup()
        .click(within(card("Leg press")).getByRole("button", { name: "Watch video" }));

      // Assert
      expect(await screen.findByRole("dialog", { name: "Leg press" })).toBeInTheDocument();
    });
  });

  describe("nothing can be chosen", () => {
    it.each([
      ["a trainer", completeProfile],
      ["an admin", adminProfile],
    ])("shows %s no choose button, and a star on every card", async (_role, profile) => {
      // Arrange & Act
      await renderCatalog({ profile }).rendered;

      // Assert
      await loaded();
      expect(screen.queryByRole("button", { name: "Elegir ejercicio" })).not.toBeInTheDocument();
      for (const item of cards()) {
        expect(
          within(item).getByRole("button", { name: /a favoritos|de favoritos/ }),
        ).toBeVisible();
      }
    });
  });

  describe("favorites", () => {
    it("marks an exercise, fills its star and lists it under Favoritos", async () => {
      // Arrange
      const { mark, rendered } = renderCatalog();
      await rendered;
      await loaded();
      await waitFor(() =>
        expect(
          within(card("Press de banca")).getByRole("button", { name: "Añadir a favoritos" }),
        ).toBeEnabled(),
      );
      const user = userEvent.setup();

      // Act
      await user.click(
        within(card("Press de banca")).getByRole("button", { name: "Añadir a favoritos" }),
      );

      // Assert
      expect(
        await within(card("Press de banca")).findByRole("button", { name: "Quitar de favoritos" }),
      ).toBeInTheDocument();
      expect(mark).toHaveBeenCalledTimes(1);
      expect(mark).toHaveBeenCalledWith("ex-bench");
      await user.click(section("Favoritos"));
      expect(names()).toEqual(["Press de banca"]);
    });

    it("drops an unmarked exercise from Favoritos without reading the favorites again", async () => {
      // Arrange
      const { unmark, favorites, rendered } = renderCatalog({
        getFavorites: favoritesOf("ex-bench", "ex-squat"),
      });
      await rendered;
      await loaded();
      const user = userEvent.setup();
      await user.click(section("Favoritos"));
      await waitFor(() =>
        expect(
          within(card("Press de banca")).getByRole("button", { name: "Quitar de favoritos" }),
        ).toBeEnabled(),
      );

      // Act
      await user.click(
        within(card("Press de banca")).getByRole("button", { name: "Quitar de favoritos" }),
      );

      // Assert
      await waitFor(() => expect(names()).toEqual(["Sentadilla"]));
      expect(unmark).toHaveBeenCalledTimes(1);
      expect(favorites).toHaveBeenCalledTimes(1);
    });

    it("keeps the star outlined and shows the message when the API refuses", async () => {
      // Arrange
      await renderCatalog({
        markFavorite: async () => fail(new DomainError("CONFLICT", "")),
      }).rendered;
      await loaded();
      const user = userEvent.setup();
      await waitFor(() =>
        expect(
          within(card("Press de banca")).getByRole("button", { name: "Añadir a favoritos" }),
        ).toBeEnabled(),
      );

      // Act
      await user.click(
        within(card("Press de banca")).getByRole("button", { name: "Añadir a favoritos" }),
      );

      // Assert
      expect(
        await screen.findByText("La operación entra en conflicto con el estado actual."),
      ).toBeInTheDocument();
      expect(
        within(card("Press de banca")).getByRole("button", { name: "Añadir a favoritos" }),
      ).toBeInTheDocument();
    });

    it("keeps the catalog usable when the favorites fail to load", async () => {
      // Arrange & Act
      await renderCatalog({
        getFavorites: async () => fail(new DomainError("NETWORK", "")),
      }).rendered;

      // Assert
      expect(await screen.findByText("No se pudieron cargar los favoritos")).toBeInTheDocument();
      expect(section("Favoritos")).toHaveAttribute("aria-disabled", "true");
      for (const star of screen.getAllByRole("button", { name: "Añadir a favoritos" })) {
        expect(star).toBeDisabled();
      }
      expect(names()).toHaveLength(4);
    });
  });

  describe("adding an exercise", () => {
    it("shows the button to a trainer, beside the heading", async () => {
      // Arrange & Act
      await renderCatalog().rendered;

      // Assert
      await loaded();
      expect(addButton()).toBeVisible();
    });

    it("does not show it to an admin", async () => {
      // Arrange & Act
      await renderCatalog({ profile: adminProfile }).rendered;

      // Assert
      await loaded();
      expect(addButton()).not.toBeInTheDocument();
    });

    it("opens the dialog with empty fields and sends nothing", async () => {
      // Arrange
      const { create, rendered } = renderCatalog();
      await rendered;
      await loaded();

      // Act
      await userEvent.setup().click(screen.getByRole("button", { name: "Añadir ejercicio" }));

      // Assert
      const dialog = await screen.findByRole("dialog", { name: "Nuevo ejercicio" });
      expect(within(dialog).getByRole("textbox", { name: "Nombre" })).toHaveValue("");
      expect(within(dialog).getByRole("combobox", { name: "Músculo" })).toBeInTheDocument();
      expect(within(dialog).getByRole("combobox", { name: "Tipo" })).toBeInTheDocument();
      expect(within(dialog).getByRole("textbox", { name: "Enlace de vídeo" })).toHaveValue("");
      expect(create).not.toHaveBeenCalled();
    });

    it("groups the muscles by body part under headers, each group sorted", async () => {
      // Arrange
      await renderCatalog().rendered;
      await loaded();
      const user = userEvent.setup();
      await openCreate(user);

      // Act
      await user.click(within(createDialog()).getByRole("combobox", { name: "Músculo" }));

      // Assert
      const listbox = await screen.findByRole("listbox");
      expect(Array.from(listbox.children).map((child) => child.textContent)).toEqual([
        "— Brazos —",
        "Antebrazos",
        "Bíceps",
        "Tríceps",
        "— Core —",
        "Abdominales",
        "Zona lumbar",
        "— Cuello —",
        "Cuello",
        "— Espalda —",
        "Dorsales",
        "Espalda media",
        "Trapecios",
        "— Hombros —",
        "Hombros",
        "— Pecho —",
        "Pecho",
        "— Piernas —",
        "Abductores",
        "Aductores",
        "Cuádriceps",
        "Gemelos",
        "Glúteos",
        "Isquiotibiales",
      ]);
    });

    it("cannot choose a body part header", async () => {
      // Arrange
      await renderCatalog().rendered;
      await loaded();
      const user = userEvent.setup();
      await openCreate(user);
      await user.click(within(createDialog()).getByRole("combobox", { name: "Músculo" }));

      // Act
      await user.click(await screen.findByText("— Espalda —"));

      // Assert
      expect(screen.getByRole("listbox")).toBeInTheDocument();
      await user.keyboard("{Escape}");
      expect(within(createDialog()).getByRole("combobox", { name: "Músculo" })).toHaveTextContent(
        /^\u200b?$/,
      );
    });

    it("sends the muscle chosen under its group, not the group", async () => {
      // Arrange
      const { create, rendered } = renderCatalog();
      await rendered;
      await loaded();
      const user = userEvent.setup();
      await openCreate(user);
      await fillExercise(user, "Remo con banda", "Dorsales", "Fuerza");

      // Act
      await submit(user);

      // Assert
      await waitFor(() => expect(create).toHaveBeenCalledTimes(1));
      expect(create).toHaveBeenCalledWith(expect.objectContaining({ muscle: "LATS" }));
    });

    it("checks the empty form and sends nothing", async () => {
      // Arrange
      const { create, rendered } = renderCatalog();
      await rendered;
      await loaded();
      const user = userEvent.setup();
      await openCreate(user);

      // Act
      await submit(user);

      // Assert
      const dialog = createDialog();
      expect(within(dialog).getAllByText("Este campo es obligatorio.")).toHaveLength(3);
      expect(create).not.toHaveBeenCalled();
    });

    it("rejects a name under 3 characters", async () => {
      // Arrange
      const { create, rendered } = renderCatalog();
      await rendered;
      await loaded();
      const user = userEvent.setup();
      await openCreate(user);
      await fillExercise(user, "ab", "Espalda media", "Fuerza");

      // Act
      await submit(user);

      // Assert
      expect(within(createDialog()).getByText("Debe tener al menos 3 caracteres.")).toBeVisible();
      expect(create).not.toHaveBeenCalled();
    });

    it("rejects a link that is not a web address", async () => {
      // Arrange
      const { create, rendered } = renderCatalog();
      await rendered;
      await loaded();
      const user = userEvent.setup();
      await openCreate(user);
      await fillExercise(user, "Remo con banda", "Espalda media", "Fuerza", "no es un enlace");

      // Act
      await submit(user);

      // Assert
      expect(
        within(createDialog()).getByText("Introduce una dirección web válida (http o https)."),
      ).toBeVisible();
      expect(create).not.toHaveBeenCalled();
    });

    it("creates the exercise, closes the dialog and lists it under Todos and Mis ejercicios", async () => {
      // Arrange
      const { create, catalog: readCatalog, rendered } = renderCatalog();
      await rendered;
      await loaded();
      const user = userEvent.setup();
      await openCreate(user);
      await fillExercise(user, "  Remo con banda  ", "Espalda media", "Fuerza");

      // Act
      await submit(user);

      // Assert
      await waitFor(() =>
        expect(screen.queryByRole("dialog", { name: "Nuevo ejercicio" })).not.toBeInTheDocument(),
      );
      expect(create).toHaveBeenCalledTimes(1);
      expect(create).toHaveBeenCalledWith({
        languageCode: "es",
        name: "Remo con banda",
        muscle: "MIDDLE_BACK",
        type: "STRENGTH",
      });
      expect(names()).toContain("Remo con banda");
      await user.click(section("Mis ejercicios"));
      expect(names()).toEqual(["Mi curl", "Remo con banda"]);
      expect(readCatalog).toHaveBeenCalledTimes(1);
    });

    it("keeps the selected section when an exercise is created", async () => {
      // Arrange
      await renderCatalog().rendered;
      await loaded();
      const user = userEvent.setup();
      await user.click(section("Mis ejercicios"));
      await openCreate(user);
      await fillExercise(user, "Remo con banda", "Espalda media", "Fuerza");

      // Act
      await submit(user);

      // Assert
      await waitFor(() => expect(names()).toContain("Remo con banda"));
      expect(section("Mis ejercicios")).toHaveAttribute("aria-current", "true");
    });

    it("sends the trimmed link when one is typed", async () => {
      // Arrange
      const { create, rendered } = renderCatalog();
      await rendered;
      await loaded();
      const user = userEvent.setup();
      await openCreate(user);
      await fillExercise(
        user,
        "Remo con banda",
        "Espalda media",
        "Fuerza",
        "  https://www.youtube.com/watch?v=abc  ",
      );

      // Act
      await submit(user);

      // Assert
      await waitFor(() => expect(create).toHaveBeenCalledTimes(1));
      expect(create).toHaveBeenCalledWith(
        expect.objectContaining({ videoUrl: "https://www.youtube.com/watch?v=abc" }),
      );
    });

    it("creates the exercise in the language the screen is shown in", async () => {
      // Arrange
      const { create, rendered } = renderCatalog();
      await rendered;
      await loaded();
      await act(() => i18n.changeLanguage("en"));
      const user = userEvent.setup();
      await user.click(screen.getByRole("button", { name: "Add exercise" }));
      const dialog = await screen.findByRole("dialog", { name: "New exercise" });
      await user.type(within(dialog).getByRole("textbox", { name: "Name" }), "Band row");
      await user.click(within(dialog).getByRole("combobox", { name: "Muscle" }));
      await user.click(await screen.findByRole("option", { name: "Middle back" }));
      await user.click(within(dialog).getByRole("combobox", { name: "Type" }));
      await user.click(await screen.findByRole("option", { name: "Strength" }));

      // Act
      await user.click(within(dialog).getByRole("button", { name: "Create" }));

      // Assert
      await waitFor(() => expect(create).toHaveBeenCalledTimes(1));
      expect(create).toHaveBeenCalledWith(expect.objectContaining({ languageCode: "en" }));
    });

    it("disables the fields and both buttons while the creation is in progress", async () => {
      // Arrange
      let release!: (result: Result<Exercise, DomainError>) => void;
      const pending = new Promise<Result<Exercise, DomainError>>((resolve) => {
        release = resolve;
      });
      await renderCatalog({ createExercise: () => pending }).rendered;
      await loaded();
      const user = userEvent.setup();
      await openCreate(user);
      await fillExercise(user, "Remo con banda", "Espalda media", "Fuerza");

      // Act
      await submit(user);

      // Assert
      const dialog = createDialog();
      expect(within(dialog).getByRole("textbox", { name: "Nombre" })).toBeDisabled();
      expect(within(dialog).getByRole("button", { name: "Crear" })).toBeDisabled();
      expect(within(dialog).getByRole("button", { name: "Cancelar" })).toBeDisabled();
      await act(async () => release(ok(createdFrom(rowRequest, "x"))));
    });

    it("ignores Escape while the creation is in progress", async () => {
      // Arrange
      let release!: (result: Result<Exercise, DomainError>) => void;
      const pending = new Promise<Result<Exercise, DomainError>>((resolve) => {
        release = resolve;
      });
      await renderCatalog({ createExercise: () => pending }).rendered;
      await loaded();
      const user = userEvent.setup();
      await openCreate(user);
      await fillExercise(user, "Remo con banda", "Espalda media", "Fuerza");
      await submit(user);

      // Act
      await user.keyboard("{Escape}");

      // Assert
      expect(screen.getByRole("dialog", { name: "Nuevo ejercicio" })).toBeInTheDocument();
      await act(async () => release(ok(createdFrom(rowRequest, "x"))));
    });

    it.each([
      ["VALIDATION", "Revisa los datos introducidos."],
      ["NETWORK", "No hay conexión con el servidor."],
      ["FORBIDDEN", "No tienes permiso para realizar esta acción."],
    ] as const)(
      "keeps the dialog and the typed values when the API answers %s",
      async (code, message) => {
        // Arrange
        await renderCatalog({
          createExercise: async () => fail(new DomainError(code, "")),
        }).rendered;
        await loaded();
        const user = userEvent.setup();
        await openCreate(user);
        await fillExercise(user, "Remo con banda", "Espalda media", "Fuerza");

        // Act
        await submit(user);

        // Assert
        expect(await within(createDialog()).findByText(message)).toBeVisible();
        expect(within(createDialog()).getByRole("textbox", { name: "Nombre" })).toHaveValue(
          "Remo con banda",
        );
        expect(names()).not.toContain("Remo con banda");
      },
    );

    it("starts empty and without a message when a refused dialog is closed and opened again", async () => {
      // Arrange
      await renderCatalog({
        createExercise: async () => fail(new DomainError("NETWORK", "")),
      }).rendered;
      await loaded();
      const user = userEvent.setup();
      await openCreate(user);
      await fillExercise(user, "Remo con banda", "Espalda media", "Fuerza");
      await submit(user);
      await within(createDialog()).findByText("No hay conexión con el servidor.");
      await user.click(within(createDialog()).getByRole("button", { name: "Cancelar" }));
      await waitFor(() =>
        expect(screen.queryByRole("dialog", { name: "Nuevo ejercicio" })).not.toBeInTheDocument(),
      );

      // Act
      await openCreate(user);

      // Assert
      expect(within(createDialog()).getByRole("textbox", { name: "Nombre" })).toHaveValue("");
      expect(
        within(createDialog()).queryByText("No hay conexión con el servidor."),
      ).not.toBeInTheDocument();
    });

    it("closes with Cancelar without a request and leaves the list unchanged", async () => {
      // Arrange
      const { create, rendered } = renderCatalog();
      await rendered;
      await loaded();
      const user = userEvent.setup();
      await openCreate(user);
      await user.type(within(createDialog()).getByRole("textbox", { name: "Nombre" }), "Remo");

      // Act
      await user.click(within(createDialog()).getByRole("button", { name: "Cancelar" }));

      // Assert
      await waitFor(() =>
        expect(screen.queryByRole("dialog", { name: "Nuevo ejercicio" })).not.toBeInTheDocument(),
      );
      expect(create).not.toHaveBeenCalled();
      expect(names()).toHaveLength(4);
    });

    it("closes with Escape without a request", async () => {
      // Arrange
      const { create, rendered } = renderCatalog();
      await rendered;
      await loaded();
      const user = userEvent.setup();
      await openCreate(user);

      // Act
      await user.keyboard("{Escape}");

      // Assert
      await waitFor(() =>
        expect(screen.queryByRole("dialog", { name: "Nuevo ejercicio" })).not.toBeInTheDocument(),
      );
      expect(create).not.toHaveBeenCalled();
    });
  });

  describe("translation", () => {
    it("shows the screen in English", async () => {
      // Arrange
      await renderCatalog().rendered;
      await loaded();

      // Act
      await act(() => i18n.changeLanguage("en"));

      // Assert
      expect(screen.getByRole("heading", { name: "Exercise catalog", level: 1 })).toBeVisible();
      expect(screen.getByRole("button", { name: "Add exercise" })).toBeVisible();
      expect(screen.getByRole("list", { name: "Sections" })).toBeVisible();
    });
  });
});
