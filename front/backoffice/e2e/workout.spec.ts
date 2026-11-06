import { expect, type Page, test } from "@playwright/test";
import { completeProfile, mockUsersMe } from "./support/users-me";

const api = "http://localhost:5199";
const cors = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, content-type",
  "Access-Control-Allow-Methods": "GET, POST, PUT, DELETE, OPTIONS",
};

const athleteId = "5f1c2f0e-9d5b-4a53-8c7e-0d6f7c1a2b3c";

/** The athlete of every stored workout: listed beside each one, and offered by the create dialog. */
const athlete = {
  id: athleteId,
  email: "bea@example.com",
  name: "Bea",
  surname: "López",
  role: "USER",
};

interface StoredWorkout {
  id: string;
  name: string;
  description: string | null;
  workoutDate: string;
  userId: string;
  trainerId: string | null;
  status: string;
  exercises: unknown[];
}

const existing: StoredWorkout = {
  id: "7b0c1f7e-3a54-4d4e-9d0e-1a2b3c4d5e6f",
  name: "Empuje",
  description: "Día 2",
  workoutDate: "2026-11-03",
  userId: athleteId,
  trainerId: null,
  status: "DRAFT",
  exercises: [],
};

const catalog = [
  {
    id: "1f2e3d4c-5b6a-4978-8695-a4b3c2d1e0f9",
    freeExerciseDbId: null,
    videoUrl: null,
    muscle: "QUADRICEPS",
    bodyPart: "LEGS",
    type: "STRENGTH",
    translations: [
      { languageCode: "es", name: "Sentadilla" },
      { languageCode: "en", name: "Squat" },
    ],
    ownerId: null,
    archivedAtUtc: null,
  },
];

const benchPress = {
  id: "2a3b4c5d-6e7f-4809-9a1b-2c3d4e5f6a7b",
  freeExerciseDbId: null,
  videoUrl: null,
  muscle: "CHEST",
  bodyPart: "CHEST",
  type: "STRENGTH",
  translations: [
    { languageCode: "es", name: "Press de banca" },
    { languageCode: "en", name: "Bench press" },
  ],
  ownerId: null,
  archivedAtUtc: null,
};

function entryFor(id: string, exerciseId: string) {
  return {
    id,
    exerciseId,
    effort: "MID",
    perSide: false,
    restMinutes: null,
    superSeriesId: null,
    sets: [{ count: 3, target: { type: "Reps", value: 10, maxValue: null } }],
  };
}

/** A workout whose two saved exercises can be reordered: Sentadilla, then Press de banca. */
const reorderable: StoredWorkout = {
  ...existing,
  id: "6e5d4c3b-2a19-4807-b6a5-948372615049",
  name: "Fuerza",
  exercises: [
    entryFor("0a1b2c3d-4e5f-4a6b-8c7d-9e0f1a2b3c4d", catalog[0]!.id),
    entryFor("1b2c3d4e-5f6a-4b7c-8d9e-0f1a2b3c4d5e", benchPress.id),
  ],
};

/** Three more catalog exercises, so a workout can hold five and fill more than one row. */
const moreExercises = ["Remo", "Dominadas", "Fondos"].map((name, index) => ({
  ...benchPress,
  id: `3c4d5e6f-7a8b-4c9d-8e0f-1a2b3c4d5e6${index}`,
  translations: [
    { languageCode: "es", name },
    { languageCode: "en", name },
  ],
}));

/** A workout with five exercises: three fill the first row and the other two start the next one. */
const crowded: StoredWorkout = {
  ...existing,
  id: "5d4c3b2a-1908-4f6e-a5d4-c3b2a1908f7e",
  name: "Completo",
  exercises: [catalog[0]!, benchPress, ...moreExercises].map((exercise, index) =>
    entryFor(`4e5f6a7b-8c9d-4e0f-9a1b-2c3d4e5f6a7${index}`, exercise.id),
  ),
};

/** Thursday 29 October 2026, local noon: the current week is 26 Oct – 1 Nov 2026, in the current year. */
const today = new Date(2026, 9, 29, 12);

function json(body: unknown, status = 200) {
  return { status, headers: cors, contentType: "application/json", body: JSON.stringify(body) };
}

interface MockOptions {
  /** The catalog `GET /exercises` answers with. */
  exercises?: unknown[];
  /** The status `PUT /workouts/{id}/exercises/order` answers with; on 200 it stores the new order. */
  orderStatus?: number;
  /** The exercises that are already the user's favorites. */
  favoriteIds?: string[];
}

/** The bodies of the exercise requests the screen sent, in the order they were sent. */
interface SentRequests {
  additions: unknown[];
  updates: unknown[];
  reorders: unknown[];
  /** The exercise ids marked and unmarked as favorites. */
  marks: string[];
  unmarks: string[];
  /** The bodies of the `POST /exercises` requests. */
  creations: SentNewExercise[];
  /** The bodies of the `POST /workouts/{id}/superseries` requests. */
  groupings: unknown[];
  /** The paths of the `DELETE /workouts/{id}/superseries/{superSeriesId}` requests. */
  breaks: string[];
}

interface SentNewExercise {
  languageCode: string;
  name: string;
  muscle: string;
  type: string;
  videoUrl?: string;
}

interface SentSet {
  count: number;
  targetType: string;
  target?: number;
  maxTarget?: number;
  targetMinutes?: number;
}

interface SentExercise {
  exerciseId: string;
  effort: string;
  sets: SentSet[];
  perSide?: boolean;
  restMinutes?: number;
}

/** An entry as the API answers it, with the sets, rest and per side it received. */
function entryOf(
  id: string,
  exerciseId: string,
  { effort, sets, perSide, restMinutes }: SentExercise,
) {
  return {
    id,
    exerciseId,
    effort,
    perSide: perSide ?? false,
    restMinutes: restMinutes ?? null,
    superSeriesId: null,
    sets: sets.map((set) => ({
      count: set.count,
      target:
        set.targetType === "Minutes"
          ? { type: set.targetType, minutes: set.targetMinutes ?? null }
          : { type: set.targetType, value: set.target ?? null, maxValue: set.maxTarget ?? null },
    })),
  };
}

/**
 * Intercepts the workout and catalog endpoints. A `POST /workouts` stores what it receives as a new workout, a
 * `PUT /workouts/{id}` replaces that workout's data, a `POST` or `PUT` under `/workouts/{id}/exercises` adds or
 * replaces an exercise, a `PUT /workouts/{id}/exercises/order` reorders them, and `POST` and `DELETE` under
 * `/workouts/{id}/superseries` group and ungroup them. Resolves to the bodies of the exercise requests.
 */
async function mockWorkoutsApi(
  page: Page,
  { exercises = catalog, orderStatus = 200, favoriteIds = [] }: MockOptions,
) {
  const workouts = new Map<string, StoredWorkout>([
    [existing.id, existing],
    [reorderable.id, reorderable],
    [crowded.id, crowded],
  ]);
  const sent: SentRequests = {
    additions: [],
    updates: [],
    reorders: [],
    marks: [],
    unmarks: [],
    creations: [],
    groupings: [],
    breaks: [],
  };
  // The catalog the API holds: creating an exercise adds one, owned by the signed-in user.
  const stored = [...exercises];
  const favorites = new Map<string, string>(favoriteIds.map((id) => [id, "2026-11-01T09:00:00Z"]));

  await page.route(`${api}/trainer-athletes`, (route) =>
    route.request().method() === "OPTIONS"
      ? route.fulfill({ status: 204, headers: cors })
      : route.fulfill(
          json([
            {
              id: "rel-1",
              athleteId: athlete.id,
              email: athlete.email,
              name: athlete.name,
              surname: athlete.surname,
              trainerName: "Ana",
              trainerSurname: "García",
              startedAtUtc: "2026-10-01T09:00:00Z",
            },
          ]),
        ),
  );
  // The list is read as `?standalone=true`, so the query string is part of the match.
  await page.route(new RegExp(`^${api}/workouts(\\?.*)?$`), (route) => {
    const request = route.request();

    if (request.method() === "OPTIONS") {
      return route.fulfill({ status: 204, headers: cors });
    }
    if (request.method() === "POST") {
      const body = request.postDataJSON() as Partial<StoredWorkout>;
      const created = {
        ...existing,
        ...body,
        id: "8c1d2e3f-4a5b-4c6d-8e7f-9a0b1c2d3e4f",
        description: body.description ?? null,
      };
      workouts.set(created.id, created);
      return route.fulfill(json(created, 201));
    }
    return route.fulfill(
      json([...workouts.values()].map((workout) => ({ ...workout, athlete, trainer: null }))),
    );
  });
  await page.route(`${api}/workouts/*`, (route) => {
    if (route.request().method() === "OPTIONS") {
      return route.fulfill({ status: 204, headers: cors });
    }
    const id = new URL(route.request().url()).pathname.split("/").pop() ?? "";
    const workout = workouts.get(id);
    if (!workout) {
      return route.fulfill(json({ status: 404 }, 404));
    }
    if (route.request().method() === "PUT") {
      const body = route.request().postDataJSON() as Partial<StoredWorkout>;
      const updated = { ...workout, ...body, description: body.description ?? null };
      workouts.set(id, updated);
      return route.fulfill(json(updated));
    }
    // The read by id carries the athlete and the trainer, as the list does.
    return route.fulfill(json({ ...workout, athlete, trainer: null }));
  });
  await page.route(`${api}/workouts/*/exercises`, (route) => {
    const request = route.request();
    if (request.method() === "OPTIONS") {
      return route.fulfill({ status: 204, headers: cors });
    }
    const workoutId = new URL(request.url()).pathname.split("/")[2] ?? "";
    const workout = workouts.get(workoutId);
    if (!workout) {
      return route.fulfill(json({ status: 404 }, 404));
    }
    const body = request.postDataJSON() as SentExercise;
    sent.additions.push(body);
    const entry = entryOf("9d8c7b6a-5f4e-4d3c-8b2a-1f0e9d8c7b6a", body.exerciseId, body);
    workouts.set(workoutId, { ...workout, exercises: [...workout.exercises, entry] });
    return route.fulfill(json(entry, 201));
  });
  await page.route(`${api}/workouts/*/exercises/*`, (route) => {
    const request = route.request();
    if (request.method() === "OPTIONS") {
      return route.fulfill({ status: 204, headers: cors });
    }
    const [, , workoutId = "", , exerciseId = ""] = new URL(request.url()).pathname.split("/");
    const workout = workouts.get(workoutId);
    const current = workout?.exercises.find(
      (entry) => (entry as { exerciseId: string }).exerciseId === exerciseId,
    ) as { id: string } | undefined;
    if (!workout || !current) {
      return route.fulfill(json({ status: 404 }, 404));
    }
    if (request.method() === "PUT") {
      const body = request.postDataJSON() as SentExercise;
      sent.updates.push(body);
      const entry = entryOf(current.id, exerciseId, body);
      workouts.set(workoutId, {
        ...workout,
        exercises: workout.exercises.map((other) => (other === current ? entry : other)),
      });
      return route.fulfill(json(entry));
    }
    workouts.set(workoutId, {
      ...workout,
      exercises: workout.exercises.filter(
        (entry) => (entry as { exerciseId: string }).exerciseId !== exerciseId,
      ),
    });
    return route.fulfill({ status: 204, headers: cors });
  });
  // Registered after `exercises/*`, so it answers the `order` path instead.
  await page.route(`${api}/workouts/*/exercises/order`, (route) => {
    const request = route.request();
    if (request.method() === "OPTIONS") {
      return route.fulfill({ status: 204, headers: cors });
    }
    const workoutId = new URL(request.url()).pathname.split("/")[2] ?? "";
    const workout = workouts.get(workoutId);
    const body = request.postDataJSON() as { exerciseIds: string[] };
    sent.reorders.push(body);
    if (!workout) {
      return route.fulfill(json({ status: 404 }, 404));
    }
    if (orderStatus !== 200) {
      return route.fulfill(json({ status: orderStatus, detail: "Refused" }, orderStatus));
    }
    const byExercise = new Map(
      workout.exercises.map((entry) => [(entry as { exerciseId: string }).exerciseId, entry]),
    );
    const reordered = { ...workout, exercises: body.exerciseIds.map((id) => byExercise.get(id)) };
    workouts.set(workoutId, reordered);
    return route.fulfill(json(reordered));
  });
  await page.route(`${api}/workouts/*/superseries`, (route) => {
    const request = route.request();
    if (request.method() === "OPTIONS") {
      return route.fulfill({ status: 204, headers: cors });
    }
    const workoutId = new URL(request.url()).pathname.split("/")[2] ?? "";
    const workout = workouts.get(workoutId);
    const body = request.postDataJSON() as { exerciseIds: string[] };
    sent.groupings.push(body);
    if (!workout) {
      return route.fulfill(json({ status: 404 }, 404));
    }
    const id = 1;
    workouts.set(workoutId, {
      ...workout,
      exercises: workout.exercises.map((entry) =>
        body.exerciseIds.includes((entry as { id: string }).id)
          ? { ...(entry as object), superSeriesId: id }
          : entry,
      ),
    });
    return route.fulfill(json({ id }, 201));
  });
  await page.route(`${api}/workouts/*/superseries/*`, (route) => {
    const request = route.request();
    if (request.method() === "OPTIONS") {
      return route.fulfill({ status: 204, headers: cors });
    }
    const path = new URL(request.url()).pathname;
    const [, , workoutId = "", , superSeriesId = ""] = path.split("/");
    const workout = workouts.get(workoutId);
    sent.breaks.push(path);
    if (!workout) {
      return route.fulfill(json({ status: 404 }, 404));
    }
    workouts.set(workoutId, {
      ...workout,
      exercises: workout.exercises.map((entry) =>
        String((entry as { superSeriesId: unknown }).superSeriesId) === superSeriesId
          ? { ...(entry as object), superSeriesId: null }
          : entry,
      ),
    });
    return route.fulfill({ status: 204, headers: cors });
  });
  await page.route(`${api}/exercises`, (route) => {
    const request = route.request();
    if (request.method() === "OPTIONS") {
      return route.fulfill({ status: 204, headers: cors });
    }
    if (request.method() === "POST") {
      const body = request.postDataJSON() as SentNewExercise;
      sent.creations.push(body);
      const created = {
        id: `9e8d7c6b-5a49-4837-9625-14033f2e1d0c`,
        freeExerciseDbId: null,
        videoUrl: body.videoUrl ?? null,
        muscle: body.muscle,
        bodyPart: "BACK",
        type: body.type,
        translations: [{ languageCode: body.languageCode, name: body.name }],
        ownerId: completeProfile.id,
        archivedAtUtc: null,
      };
      stored.push(created);
      return route.fulfill(json(created, 201));
    }
    return route.fulfill(json(stored));
  });
  await page.route(`${api}/exercise-favorites`, (route) =>
    route.request().method() === "OPTIONS"
      ? route.fulfill({ status: 204, headers: cors })
      : route.fulfill(
          json([...favorites].map(([exerciseId, createdAtUtc]) => ({ exerciseId, createdAtUtc }))),
        ),
  );
  // Marking keeps the first date and unmarking what is not a favorite succeeds too, as the API does.
  await page.route(`${api}/exercise-favorites/*`, (route) => {
    const request = route.request();
    if (request.method() === "OPTIONS") {
      return route.fulfill({ status: 204, headers: cors });
    }
    const exerciseId = new URL(request.url()).pathname.split("/").pop() ?? "";
    if (request.method() === "PUT") {
      sent.marks.push(exerciseId);
      const createdAtUtc = favorites.get(exerciseId) ?? "2026-11-03T09:30:00Z";
      favorites.set(exerciseId, createdAtUtc);
      return route.fulfill(json({ exerciseId, createdAtUtc }));
    }
    sent.unmarks.push(exerciseId);
    favorites.delete(exerciseId);
    return route.fulfill({ status: 204, headers: cors });
  });
  return sent;
}

async function signIn(page: Page, options: MockOptions = {}) {
  await mockUsersMe(page);
  const sent = await mockWorkoutsApi(page, options);
  await page.addInitScript(() => {
    window.localStorage.setItem("auth.accessToken", "access");
    window.localStorage.setItem("auth.refreshToken", "refresh");
  });
  return sent;
}

/** Opens the catalog dialog, chooses `name` in it and resolves to the configuration dialog that opens for it. */
async function chooseFromCatalog(page: Page, name: string) {
  await page.getByRole("button", { name: "Añadir ejercicio" }).click();
  const catalogDialog = page.getByRole("dialog", { name: "Catálogo de ejercicios" });
  await catalogDialog
    .getByRole("listitem")
    .filter({ hasText: name })
    .getByRole("button", { name: "Elegir ejercicio" })
    .click();
  return page.getByRole("dialog", { name: "Configuración del ejercicio" });
}

/**
 * An arrow key first scrolls the page when the next row sits below the middle of the viewport, and the keyboard sensor
 * cancels a drag on resize, so reorder tests open the screen in a viewport tall enough for both rows from the start.
 */
const tallViewport = { width: 1280, height: 1600 };

/** Picks up the first workout exercise by its handle and drops it one card to the right, with the keyboard. */
async function moveFirstExerciseRight(page: Page) {
  const exercisesCard = page.getByRole("region", { name: "Ejercicios" });
  const handle = exercisesCard.getByRole("button", { name: "Reordenar ejercicio" }).first();
  // dnd-kit listens for the moving keys only once the pick-up is handled, so each key waits for the previous one.
  await handle.focus();
  await page.keyboard.press("Space");
  await expect(handle).toHaveAttribute("aria-pressed", "true");
  // Announced once the rows are measured; before that an arrow key has nowhere to move to.
  await expect(page.getByRole("status")).toContainText("en la posición 1");
  // Under load Chromium sometimes drops the first arrow key of a drag; with two rows a repeated key cannot overshoot.
  await expect(async () => {
    await page.keyboard.press("ArrowRight");
    await expect(page.getByRole("status")).toContainText("en la posición 2", { timeout: 500 });
  }).toPass();
  await page.keyboard.press("Space");
}

test("creates a workout from the dialog and lands on its screen", async ({ page }) => {
  // Arrange
  await page.clock.setFixedTime(today);
  await signIn(page);
  await page.goto("/assigned-workouts");
  await page.getByRole("button", { name: "Nuevo entrenamiento" }).click();
  const dialog = page.getByRole("dialog", { name: "Nuevo entrenamiento" });
  await dialog.getByLabel("Nombre").fill("Pierna");
  await dialog.getByRole("combobox", { name: "Atleta" }).click();
  await page.getByRole("option", { name: "Bea López · bea@example.com" }).click();
  const creation = page.waitForRequest(
    (request) => request.url() === `${api}/workouts` && request.method() === "POST",
  );

  // Act
  await dialog.getByRole("button", { name: "Crear" }).click();

  // Assert
  // The dialog starts on the current week, which is sent as its Wednesday.
  expect((await creation).postDataJSON()).toMatchObject({ workoutDate: "2026-10-28" });
  await expect(page).toHaveURL(/\/workout\/?\?id=8c1d2e3f-4a5b-4c6d-8e7f-9a0b1c2d3e4f$/);
  await expect(page.getByRole("heading", { level: 1, name: "Editar entrenamiento" })).toBeVisible();
  await expect(page.getByRole("textbox", { name: "Nombre" })).toHaveValue("Pierna");
  await expect(
    page
      .getByRole("region", { name: "Ejercicios" })
      .getByText("Este entrenamiento aún no tiene ejercicios."),
  ).toBeVisible();
});

test("opens a workout from the table's edit action", async ({ page }) => {
  // Arrange
  await signIn(page);
  await page.goto("/assigned-workouts");

  // Act
  await page.getByRole("link", { name: "Editar Empuje" }).click();

  // Assert
  await expect(page).toHaveURL(/\/workout\/?\?id=7b0c1f7e-3a54-4d4e-9d0e-1a2b3c4d5e6f$/);
  await expect(page.getByRole("heading", { level: 1, name: "Editar entrenamiento" })).toBeVisible();
  await expect(page.getByRole("textbox", { name: "Nombre" })).toHaveValue("Empuje");
  await expect(page.getByRole("textbox", { name: "Descripción" })).toHaveValue("Día 2");
});

test("sends the Wednesday of the week chosen on the workout screen", async ({ page }) => {
  // Arrange
  await page.clock.setFixedTime(today);
  await signIn(page);
  await page.goto(`/workout?id=${existing.id}`);
  const week = page
    .getByRole("region", { name: "Propiedades del entrenamiento" })
    .getByRole("group", { name: "Semana" });
  await expect(week.getByText("Semana del 2 nov – 8 nov")).toBeVisible();
  await week.getByRole("button", { name: "Semana anterior" }).click();
  await expect(week.getByText("Semana del 26 oct – 1 nov")).toBeVisible();
  const update = page.waitForRequest(
    (request) => request.url() === `${api}/workouts/${existing.id}` && request.method() === "PUT",
  );

  // Act
  await page.getByRole("button", { name: "Guardar", exact: true }).click();

  // Assert
  expect((await update).postDataJSON()).toMatchObject({ workoutDate: "2026-10-28" });
  await page.reload();
  await expect(week.getByText("Semana del 26 oct – 1 nov")).toBeVisible();
});

test("saves a workout's data and shows it after a reload", async ({ page }) => {
  // Arrange
  await signIn(page);
  await page.goto(`/workout?id=${existing.id}`);
  const name = page.getByRole("textbox", { name: "Nombre" });
  await expect(name).toHaveValue("Empuje");
  await name.fill("Empuje B");

  // Act
  await page.getByRole("button", { name: "Guardar", exact: true }).click();

  // Assert
  await expect(page.getByText("Entrenamiento guardado.")).toBeVisible();
  await page.reload();
  await expect(page.getByRole("textbox", { name: "Nombre" })).toHaveValue("Empuje B");
});

test("adds an exercise through the catalog and configuration dialogs and keeps it after a reload", async ({
  page,
}) => {
  // Arrange
  const sent = await signIn(page);
  await page.goto(`/workout?id=${existing.id}`);
  const dialog = await chooseFromCatalog(page, "Sentadilla");
  await dialog.getByRole("combobox", { name: "Esfuerzo" }).click();
  await page.getByRole("option", { name: "RIR 0", exact: true }).click();
  const form = dialog.getByRole("group", { name: "Añadir serie" });
  await form.getByRole("spinbutton", { name: "Series" }).fill("2");
  await form.getByRole("spinbutton", { name: "Valor" }).fill("15");
  await dialog.getByRole("button", { name: "Añadir serie" }).click();
  await form.getByRole("combobox", { name: "Tipo" }).click();
  await page.getByRole("option", { name: "Máximas repeticiones" }).click();
  await form.getByRole("spinbutton", { name: "Series" }).fill("1");
  await dialog.getByRole("button", { name: "Añadir serie" }).click();

  // Act
  await dialog.getByRole("button", { name: "Guardar" }).click();

  // Assert
  await expect(dialog).toBeHidden();
  expect(sent.additions).toEqual([
    {
      exerciseId: catalog[0]!.id,
      effort: "HIGH",
      sets: [
        { count: 2, targetType: "Reps", target: 15 },
        { count: 1, targetType: "MaxReps" },
      ],
      perSide: false,
    },
  ]);
  await page.reload();
  const exercisesCard = page.getByRole("region", { name: "Ejercicios" });
  await expect(exercisesCard.getByText("Sentadilla")).toBeVisible();
  await expect(exercisesCard.getByText("2 × 15, 1 × máx", { exact: true })).toBeVisible();
  await expect(exercisesCard.getByText("Esfuerzo: RIR 0", { exact: true })).toBeVisible();
  await exercisesCard.getByRole("button", { name: "Editar ejercicio" }).click();
  const details = page.getByRole("dialog", { name: "Configuración del ejercicio" });
  await expect(details.getByText("2 × 15 repeticiones", { exact: true })).toBeVisible();
  await expect(details.getByText("1 × máximo", { exact: true })).toBeVisible();
});

test("prescribes a minutes set, rest and per side and keeps them after a reload", async ({
  page,
}) => {
  // Arrange
  const sent = await signIn(page);
  await page.goto(`/workout?id=${existing.id}`);
  const dialog = await chooseFromCatalog(page, "Sentadilla");
  await dialog.getByRole("combobox", { name: "Esfuerzo" }).click();
  await page.getByRole("option", { name: "RIR 3", exact: true }).click();
  await dialog.getByRole("textbox", { name: "Descanso entre series" }).fill("2");
  await dialog.getByRole("switch", { name: "Por lado" }).check();
  const form = dialog.getByRole("group", { name: "Añadir serie" });
  await form.getByRole("combobox", { name: "Tipo" }).click();
  await page.getByRole("option", { name: "Minutos" }).click();
  await form.getByRole("spinbutton", { name: "Series" }).fill("3");
  await form.getByRole("textbox", { name: "Valor" }).fill("1:30");
  await dialog.getByRole("button", { name: "Añadir serie" }).click();

  // Act
  await dialog.getByRole("button", { name: "Guardar" }).click();

  // Assert
  await expect(dialog).toBeHidden();
  expect(sent.additions).toEqual([
    {
      exerciseId: catalog[0]!.id,
      effort: "MID",
      sets: [{ count: 3, targetType: "Minutes", targetMinutes: 1.5 }],
      perSide: true,
      restMinutes: 2,
    },
  ]);
  await page.reload();
  const exercisesCard = page.getByRole("region", { name: "Ejercicios" });
  await expect(exercisesCard.getByText("3 × 1:30 min · por lado", { exact: true })).toBeVisible();
  await expect(exercisesCard.getByText("Descanso: 2 min", { exact: true })).toBeVisible();
});

test("filters the catalog by type and prescribes a strength exercise in RIR", async ({ page }) => {
  // Arrange
  const sent = await signIn(page);
  await page.goto(`/workout?id=${existing.id}`);
  await page.getByRole("button", { name: "Añadir ejercicio" }).click();
  const catalogDialog = page.getByRole("dialog", { name: "Catálogo de ejercicios" });
  await catalogDialog
    .getByRole("group", { name: "Tipo" })
    .getByRole("button", { name: "Fuerza" })
    .click();
  await catalogDialog
    .getByRole("listitem")
    .filter({ hasText: "Sentadilla" })
    .getByRole("button", { name: "Elegir ejercicio" })
    .click();
  const dialog = page.getByRole("dialog", { name: "Configuración del ejercicio" });
  await dialog.getByRole("combobox", { name: "Esfuerzo" }).click();
  await expect(page.getByRole("option")).toHaveText([
    "RIR 5+",
    "RIR 4",
    "RIR 3",
    "RIR 1-2",
    "RIR 0",
  ]);
  await page.getByRole("option", { name: "RIR 0", exact: true }).click();
  const form = dialog.getByRole("group", { name: "Añadir serie" });
  await form.getByRole("spinbutton", { name: "Series" }).fill("3");
  await form.getByRole("spinbutton", { name: "Valor" }).fill("8");
  await dialog.getByRole("button", { name: "Añadir serie" }).click();

  // Act
  await dialog.getByRole("button", { name: "Guardar" }).click();

  // Assert
  await expect(dialog).toBeHidden();
  expect(sent.additions).toEqual([
    expect.objectContaining({ exerciseId: catalog[0]!.id, effort: "HIGH" }),
  ]);
  await expect(
    page.getByRole("region", { name: "Ejercicios" }).getByText("Esfuerzo: RIR 0", { exact: true }),
  ).toBeVisible();
});

test("edits an exercise's sets through the pencil and keeps them after a reload", async ({
  page,
}) => {
  // Arrange
  const sent = await signIn(page, { exercises: [...catalog, benchPress] });
  await page.goto(`/workout?id=${reorderable.id}`);
  const exercisesCard = page.getByRole("region", { name: "Ejercicios" });
  await exercisesCard.getByRole("button", { name: "Editar ejercicio" }).first().click();
  const dialog = page.getByRole("dialog", { name: "Configuración del ejercicio" });
  const form = dialog.getByRole("group", { name: "Añadir serie" });
  await form.getByRole("spinbutton", { name: "Series" }).fill("2");
  await form.getByRole("spinbutton", { name: "Valor" }).fill("15");
  await dialog.getByRole("button", { name: "Añadir serie" }).click();

  // Act
  await dialog.getByRole("button", { name: "Guardar" }).click();

  // Assert
  await expect(dialog).toBeHidden();
  expect(sent.updates).toHaveLength(1);
  expect(sent.updates[0]).toMatchObject({
    effort: "MID",
    sets: [
      { count: 3, targetType: "Reps", target: 10 },
      { count: 2, targetType: "Reps", target: 15 },
    ],
  });
  expect(sent.additions).toHaveLength(0);
  await page.reload();
  await expect(exercisesCard.getByText("3 × 10, 2 × 15", { exact: true })).toBeVisible();
});

test("shows the exercise beside its configuration in the dialog", async ({ page }) => {
  // Arrange
  await signIn(page, { exercises: [...catalog, benchPress] });
  await page.goto(`/workout?id=${reorderable.id}`);

  // Act
  await page
    .getByRole("region", { name: "Ejercicios" })
    .getByRole("button", { name: "Editar ejercicio" })
    .first()
    .click();

  // Assert
  const dialog = page.getByRole("dialog", { name: "Configuración del ejercicio" });
  const name = (await dialog.getByRole("heading", { level: 3, name: "Sentadilla" }).boundingBox())!;
  const effort = (await dialog.getByRole("combobox", { name: "Esfuerzo" }).boundingBox())!;
  expect(name.x + name.width).toBeLessThanOrEqual(effort.x);
  expect(effort.y).toBeLessThan(name.y + name.height + 200);
});

test("removes a saved exercise and lists it in the catalog again", async ({ page }) => {
  // Arrange
  await signIn(page);
  await page.goto(`/workout?id=${existing.id}`);
  const dialog = await chooseFromCatalog(page, "Sentadilla");
  await dialog.getByRole("combobox", { name: "Esfuerzo" }).click();
  await page.getByRole("option", { name: "RIR 3", exact: true }).click();
  const form = dialog.getByRole("group", { name: "Añadir serie" });
  await form.getByRole("spinbutton", { name: "Series" }).fill("3");
  await form.getByRole("spinbutton", { name: "Valor" }).fill("10");
  await dialog.getByRole("button", { name: "Añadir serie" }).click();
  await dialog.getByRole("button", { name: "Guardar" }).click();
  await expect(dialog).toBeHidden();
  const exercisesCard = page.getByRole("region", { name: "Ejercicios" });

  // Act
  await exercisesCard.getByRole("button", { name: "Quitar ejercicio" }).click();

  // Assert
  await expect(
    exercisesCard.getByText("Este entrenamiento aún no tiene ejercicios."),
  ).toBeVisible();
  await page.reload();
  await expect(
    exercisesCard.getByText("Este entrenamiento aún no tiene ejercicios."),
  ).toBeVisible();
  await exercisesCard.getByRole("button", { name: "Añadir ejercicio" }).click();
  await expect(
    page.getByRole("dialog", { name: "Catálogo de ejercicios" }).getByText("Sentadilla"),
  ).toBeVisible();
});

test("stacks the cards and lays the exercises out in rows of at most three", async ({ page }) => {
  // Arrange
  await signIn(page, { exercises: [...catalog, benchPress, ...moreExercises] });
  await page.setViewportSize(tallViewport);

  // Act
  await page.goto(`/workout?id=${crowded.id}`);

  // Assert
  const exercisesCard = page.getByRole("region", { name: "Ejercicios" });
  const items = exercisesCard.getByRole("listitem");
  await expect(items).toHaveCount(5);
  const properties = (await page
    .getByRole("region", { name: "Propiedades del entrenamiento" })
    .boundingBox())!;
  const exercises = (await exercisesCard.boundingBox())!;
  expect(properties.y + properties.height).toBeLessThanOrEqual(exercises.y);
  expect(Math.round(properties.width)).toBe(Math.round(exercises.width));
  const boxes = await Promise.all(
    [0, 1, 2, 3, 4].map(async (index) => (await items.nth(index).boundingBox())!),
  );
  for (const index of [1, 2]) {
    expect(boxes[index]!.y).toBe(boxes[0]!.y);
    expect(boxes[index]!.x).toBeGreaterThan(boxes[index - 1]!.x);
  }
  expect(boxes[3]!.y).toBeGreaterThan(boxes[0]!.y);
  expect(boxes[3]!.x).toBe(boxes[0]!.x);
  expect(boxes[4]!.y).toBe(boxes[3]!.y);
});

test("shows the catalog's exercises as cards in rows of at most three", async ({ page }) => {
  // Arrange
  await signIn(page, { exercises: [...catalog, benchPress, ...moreExercises] });
  await page.setViewportSize(tallViewport);
  await page.goto(`/workout?id=${existing.id}`);

  // Act
  await page.getByRole("button", { name: "Añadir ejercicio" }).click();

  // Assert
  const items = page
    .getByRole("dialog", { name: "Catálogo de ejercicios" })
    .getByRole("list", { name: "Catálogo de ejercicios" })
    .getByRole("listitem");
  await expect(items).toHaveCount(5);
  const boxes = await Promise.all(
    [0, 1, 2, 3].map(async (index) => (await items.nth(index).boundingBox())!),
  );
  for (const index of [1, 2]) {
    expect(boxes[index]!.y).toBe(boxes[0]!.y);
    expect(boxes[index]!.x).toBeGreaterThan(boxes[index - 1]!.x);
  }
  expect(boxes[3]!.y).toBeGreaterThan(boxes[0]!.y);
  expect(boxes[3]!.x).toBe(boxes[0]!.x);
});

test("reorders the workout's exercises by dragging and keeps the order after a reload", async ({
  page,
}) => {
  // Arrange
  const { reorders } = await signIn(page, { exercises: [...catalog, benchPress] });
  await page.setViewportSize(tallViewport);
  await page.goto(`/workout?id=${reorderable.id}`);
  const exercisesCard = page.getByRole("region", { name: "Ejercicios" });
  const items = exercisesCard.getByRole("listitem");
  await expect(items.first()).toContainText("Sentadilla");

  // Act
  await moveFirstExerciseRight(page);

  // Assert
  await expect(items.nth(0)).toContainText("Press de banca");
  await expect(items.nth(1)).toContainText("Sentadilla");
  await expect.poll(() => reorders).toEqual([{ exerciseIds: [benchPress.id, catalog[0]!.id] }]);
  await page.reload();
  await expect(items.nth(0)).toContainText("Press de banca");
  await expect(items.nth(1)).toContainText("Sentadilla");
});

test("puts the previous order back and shows the error when the reorder is refused", async ({
  page,
}) => {
  // Arrange
  const { reorders } = await signIn(page, {
    exercises: [...catalog, benchPress],
    orderStatus: 409,
  });
  await page.setViewportSize(tallViewport);
  await page.goto(`/workout?id=${reorderable.id}`);
  const exercisesCard = page.getByRole("region", { name: "Ejercicios" });
  const items = exercisesCard.getByRole("listitem");
  await expect(items.first()).toContainText("Sentadilla");

  // Act
  await moveFirstExerciseRight(page);

  // Assert
  await expect(
    exercisesCard.getByText("La operación entra en conflicto con el estado actual."),
  ).toBeVisible();
  await expect(items.nth(0)).toContainText("Sentadilla");
  await expect(items.nth(1)).toContainText("Press de banca");
  expect(reorders).toHaveLength(1);
});

test("groups two exercises in a superseries, brings them together and breaks the group", async ({
  page,
}) => {
  // Arrange
  const { groupings, reorders, breaks } = await signIn(page, {
    exercises: [...catalog, benchPress, ...moreExercises],
  });
  await page.goto(`/workout?id=${crowded.id}`);
  const exercisesCard = page.getByRole("region", { name: "Ejercicios" });
  const exerciseCard = (name: string) =>
    exercisesCard.getByRole("listitem").filter({ hasText: name }).last();
  await expect(exerciseCard("Sentadilla")).toBeVisible();
  await exerciseCard("Sentadilla")
    .getByRole("checkbox", { name: "Seleccionar para superserie" })
    .check();
  await exerciseCard("Remo").getByRole("checkbox", { name: "Seleccionar para superserie" }).check();

  // Act
  await exercisesCard.getByRole("button", { name: "Agrupar en superserie" }).click();
  const group = exercisesCard.getByRole("list", { name: "superserie Sentadilla, Remo" });
  await expect(group).toBeVisible();
  await exercisesCard.getByRole("button", { name: "Romper superserie" }).click();
  await page
    .getByRole("dialog", { name: "¿Romper superserie?" })
    .getByRole("button", { name: "Romper" })
    .click();

  // Assert
  await expect(exercisesCard.getByRole("button", { name: "Romper superserie" })).toHaveCount(0);
  const [squatEntry, , rowEntry] = crowded.exercises as { id: string }[];
  expect(groupings).toEqual([{ exerciseIds: [squatEntry!.id, rowEntry!.id] }]);
  expect(reorders).toEqual([
    {
      exerciseIds: [
        catalog[0]!.id,
        moreExercises[0]!.id,
        benchPress.id,
        moreExercises[1]!.id,
        moreExercises[2]!.id,
      ],
    },
  ]);
  expect(breaks).toEqual([`/workouts/${crowded.id}/superseries/1`]);
  // Broken in place: Remo keeps the second position it took when grouped.
  await expect(exercisesCard.getByRole("listitem").nth(1)).toContainText("Remo");
});

/** Opens the catalog dialog from the exercises card and resolves to it. */
async function openCatalog(page: Page) {
  await page.getByRole("button", { name: "Añadir ejercicio" }).click();
  return page.getByRole("dialog", { name: "Catálogo de ejercicios" });
}

test("marks an exercise as a favorite, finds it under Favoritos and adds it to the workout", async ({
  page,
}) => {
  // Arrange
  const sent = await signIn(page, { exercises: [...catalog, benchPress] });
  await page.goto(`/workout?id=${existing.id}`);
  const catalogDialog = await openCatalog(page);
  const card = catalogDialog.getByRole("listitem").filter({ hasText: "Press de banca" });
  await card.getByRole("button", { name: "Añadir a favoritos" }).click();
  await expect(card.getByRole("button", { name: "Quitar de favoritos" })).toBeVisible();
  await catalogDialog.getByRole("button", { name: "Favoritos", exact: true }).click();
  await expect(
    catalogDialog.getByRole("list", { name: "Catálogo de ejercicios" }).getByRole("listitem"),
  ).toHaveCount(1);
  await card.getByRole("button", { name: "Elegir ejercicio" }).click();
  const dialog = page.getByRole("dialog", { name: "Configuración del ejercicio" });
  await dialog.getByRole("combobox", { name: "Esfuerzo" }).click();
  await page.getByRole("option", { name: "RIR 0", exact: true }).click();
  const form = dialog.getByRole("group", { name: "Añadir serie" });
  await form.getByRole("spinbutton", { name: "Series" }).fill("2");
  await form.getByRole("spinbutton", { name: "Valor" }).fill("15");
  await dialog.getByRole("button", { name: "Añadir serie" }).click();

  // Act
  await dialog.getByRole("button", { name: "Guardar" }).click();

  // Assert
  await expect(dialog).toBeHidden();
  expect(sent.marks).toEqual([benchPress.id]);
  expect(sent.additions).toEqual([
    {
      exerciseId: benchPress.id,
      effort: "HIGH",
      sets: [{ count: 2, targetType: "Reps", target: 15 }],
      perSide: false,
    },
  ]);
  await expect(
    page.getByRole("region", { name: "Ejercicios" }).getByText("Press de banca"),
  ).toBeVisible();
});

test("keeps a favorite after a reload and lists it under Favoritos", async ({ page }) => {
  // Arrange
  await signIn(page, { exercises: [...catalog, benchPress] });
  await page.goto(`/workout?id=${existing.id}`);
  const before = await openCatalog(page);
  await before
    .getByRole("listitem")
    .filter({ hasText: "Press de banca" })
    .getByRole("button", { name: "Añadir a favoritos" })
    .click();
  await expect(before.getByRole("button", { name: "Quitar de favoritos" })).toBeVisible();

  // Act
  await page.reload();

  // Assert
  const catalogDialog = await openCatalog(page);
  await catalogDialog.getByRole("button", { name: "Favoritos", exact: true }).click();
  const cards = catalogDialog
    .getByRole("list", { name: "Catálogo de ejercicios" })
    .getByRole("listitem");
  await expect(cards).toHaveCount(1);
  await expect(cards.first()).toContainText("Press de banca");
  await expect(cards.first().getByRole("button", { name: "Quitar de favoritos" })).toBeVisible();
});

test("unmarks a favorite and leaves it out of Favoritos", async ({ page }) => {
  // Arrange
  const sent = await signIn(page, {
    exercises: [...catalog, benchPress],
    favoriteIds: [benchPress.id],
  });
  await page.goto(`/workout?id=${existing.id}`);
  const catalogDialog = await openCatalog(page);
  await catalogDialog.getByRole("button", { name: "Favoritos", exact: true }).click();
  const card = catalogDialog.getByRole("listitem").filter({ hasText: "Press de banca" });

  // Act
  await card.getByRole("button", { name: "Quitar de favoritos" }).click();

  // Assert
  await expect(catalogDialog.getByText("Ningún ejercicio coincide con el filtro.")).toBeVisible();
  expect(sent.unmarks).toEqual([benchPress.id]);
  expect(sent.marks).toHaveLength(0);
});

test("opens the catalog at 90% of a wide viewport, with the sections beside the filters", async ({
  page,
}) => {
  // Arrange
  await signIn(page);
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto(`/workout?id=${existing.id}`);

  // Act
  const catalogDialog = await openCatalog(page);

  // Assert
  const dialogBox = (await catalogDialog.boundingBox())!;
  expect(dialogBox.width).toBeGreaterThanOrEqual(1440 * 0.9 - 1);
  const sections = (await catalogDialog.getByRole("list", { name: "Secciones" }).boundingBox())!;
  const filter = (await catalogDialog
    .getByRole("searchbox", { name: "Buscar ejercicio" })
    .boundingBox())!;
  expect(sections.x + sections.width).toBeLessThanOrEqual(filter.x);
});

test("keeps the catalog dialog the same height whatever the filters leave", async ({ page }) => {
  // Arrange
  await signIn(page, { exercises: [...catalog, benchPress] });
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto(`/workout?id=${existing.id}`);
  const catalogDialog = await openCatalog(page);
  const initial = (await catalogDialog.boundingBox())!.height;

  // Act
  await catalogDialog.getByRole("button", { name: "Mis ejercicios", exact: true }).click();
  await expect(catalogDialog.getByText("Ningún ejercicio coincide con el filtro.")).toBeVisible();

  // Assert
  expect((await catalogDialog.boundingBox())!.height).toBe(initial);
});

test("opens the catalog page from the menu, creates an exercise and finds it under Mis ejercicios", async ({
  page,
}) => {
  // Arrange
  const sent = await signIn(page, { exercises: [...catalog, benchPress] });
  await page.goto("/");
  await page.getByRole("link", { name: "Catálogo de ejercicios" }).click();
  await expect(page).toHaveURL(/\/exercises\/?$/);
  await expect(
    page.getByRole("heading", { name: "Catálogo de ejercicios", level: 1 }),
  ).toBeVisible();
  const cards = page.getByRole("list", { name: "Catálogo de ejercicios" }).getByRole("listitem");
  await expect(cards).toHaveCount(2);
  await expect(page.getByRole("button", { name: "Elegir ejercicio" })).toHaveCount(0);
  await page.getByRole("button", { name: "Añadir ejercicio" }).click();
  const dialog = page.getByRole("dialog", { name: "Nuevo ejercicio" });
  await dialog.getByRole("textbox", { name: "Nombre" }).fill("Remo con banda");
  await dialog.getByRole("combobox", { name: "Músculo" }).click();
  await page.getByRole("option", { name: "Espalda media" }).click();
  await dialog.getByRole("combobox", { name: "Tipo" }).click();
  await page.getByRole("option", { name: "Fuerza", exact: true }).click();

  // Act
  await dialog.getByRole("button", { name: "Crear" }).click();

  // Assert
  await expect(dialog).toBeHidden();
  await expect(cards).toHaveCount(3);
  await page.getByRole("button", { name: "Mis ejercicios", exact: true }).click();
  await expect(cards).toHaveCount(1);
  await expect(cards.first()).toContainText("Remo con banda");
  expect(sent.creations).toEqual([
    { languageCode: "es", name: "Remo con banda", muscle: "MIDDLE_BACK", type: "STRENGTH" },
  ]);
});

test("marks a favorite from the catalog page and finds it under Favoritos", async ({ page }) => {
  // Arrange
  const sent = await signIn(page, { exercises: [...catalog, benchPress] });
  await page.goto("/exercises");
  const cards = page.getByRole("list", { name: "Catálogo de ejercicios" }).getByRole("listitem");
  const card = cards.filter({ hasText: "Press de banca" });
  await expect(cards).toHaveCount(2);

  // Act
  await card.getByRole("button", { name: "Añadir a favoritos" }).click();

  // Assert
  await expect(card.getByRole("button", { name: "Quitar de favoritos" })).toBeVisible();
  expect(sent.marks).toEqual([benchPress.id]);
  await page.getByRole("button", { name: "Favoritos", exact: true }).click();
  await expect(cards).toHaveCount(1);
  await expect(cards.first()).toContainText("Press de banca");
});

test("keeps the finder, the pills and the sections fixed on the catalog page, and scrolls only the list", async ({
  page,
}) => {
  // Arrange
  const many = Array.from({ length: 30 }, (_, index) => ({
    ...benchPress,
    id: `4d5e6f7a-8b9c-4d0e-8f1a-2b3c4d5e6f${String(index).padStart(2, "0")}`,
    translations: [
      { languageCode: "es", name: `Ejercicio ${index}` },
      { languageCode: "en", name: `Exercise ${index}` },
    ],
  }));
  await signIn(page, { exercises: many });
  await page.setViewportSize({ width: 1440, height: 800 });
  await page.goto("/exercises");
  const list = page.getByRole("list", { name: "Catálogo de ejercicios" });
  await expect(list.getByRole("listitem")).toHaveCount(30);
  const finder = page.getByRole("searchbox", { name: "Buscar ejercicio" });
  const sections = page.getByRole("list", { name: "Secciones" });
  const before = {
    finder: (await finder.boundingBox())!.y,
    sections: (await sections.boundingBox())!.y,
  };

  // Act
  await list.evaluate((element) => element.scrollTo(0, element.scrollHeight));

  // Assert
  expect(await list.evaluate((element) => element.scrollTop)).toBeGreaterThan(0);
  expect(await page.evaluate(() => window.scrollY)).toBe(0);
  expect((await finder.boundingBox())!.y).toBe(before.finder);
  expect((await sections.boundingBox())!.y).toBe(before.sections);
  await expect(finder).toBeInViewport();
});

test("shows the English name and plays the video from the catalog page, and neither video nor page-only button in the selector", async ({
  page,
}) => {
  // Arrange
  const withVideo = {
    ...catalog[0]!,
    videoUrl: "https://www.youtube.com/watch?v=h_yZV27H684",
  };
  await signIn(page, { exercises: [withVideo, benchPress] });
  await page.goto("/exercises");
  const cards = page.getByRole("list", { name: "Catálogo de ejercicios" }).getByRole("listitem");
  const card = cards.filter({ hasText: "Sentadilla" });
  await expect(card.getByText("Squat")).toBeVisible();
  await expect(
    cards.filter({ hasText: "Press de banca" }).getByRole("button", { name: "Ver vídeo" }),
  ).toHaveCount(0);

  // Act
  await card.getByRole("button", { name: "Ver vídeo" }).click();

  // Assert
  const dialog = page.getByRole("dialog", { name: "Sentadilla" });
  await expect(dialog.locator("iframe")).toHaveAttribute(
    "src",
    "https://www.youtube-nocookie.com/embed/h_yZV27H684",
  );
  await dialog.getByRole("button", { name: "Cerrar" }).click();
  await expect(dialog).toBeHidden();
  await page.goto(`/workout?id=${existing.id}`);
  const selector = await openCatalog(page);
  await expect(selector.getByText("Squat")).toBeVisible();
  await expect(selector.getByRole("button", { name: "Ver vídeo" })).toHaveCount(0);
});
