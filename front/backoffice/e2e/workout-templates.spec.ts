import { expect, type Page, type Route, test } from "@playwright/test";
import { mockUsersMe } from "./support/users-me";

const api = "http://localhost:5199";
const cors = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, content-type",
  "Access-Control-Allow-Methods": "GET, POST, PUT, DELETE, OPTIONS",
};

const athleteId = "5f1c2f0e-9d5b-4a53-8c7e-0d6f7c1a2b3c";
const trainerId = "6a2d3f1e-0e6c-4b64-9d8f-1e7a8d2b3c4d";

const athlete = {
  id: athleteId,
  email: "bea@example.com",
  name: "Bea",
  surname: "López",
  role: "USER",
};

const squat = {
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
};

interface StoredTemplate {
  id: string;
  name: string;
  description: string | null;
  trainerId: string;
  exercises: unknown[];
}

const legs: StoredTemplate = {
  id: "8d7c6b5a-4f3e-4d2c-9b1a-0f9e8d7c6b5a",
  name: "Pierna A",
  description: "Fuerza",
  trainerId,
  exercises: [],
};

const workout = {
  id: "7b0c1f7e-3a54-4d4e-9d0e-1a2b3c4d5e6f",
  name: "Empuje",
  description: "Día 2",
  workoutDate: "2026-11-04",
  userId: athleteId,
  trainerId,
  status: "DRAFT",
  mesocycleId: null,
  mesocycleWeekNumber: null,
  exercises: [],
};

const createdTemplateId = "9a8b7c6d-5e4f-4a3b-8c2d-1e0f9a8b7c6d";
const today = new Date(2026, 9, 29, 12);

function json(body: unknown, status = 200) {
  return { status, headers: cors, contentType: "application/json", body: JSON.stringify(body) };
}

/** Answers the CORS preflight; resolves to `true` when the request was one. */
async function preflight(route: Route) {
  if (route.request().method() !== "OPTIONS") {
    return false;
  }
  await route.fulfill({ status: 204, headers: cors });
  return true;
}

interface SentRequests {
  templateCreations: unknown[];
  templateExercises: unknown[];
  workoutsFromTemplate: { templateId: string; body: unknown }[];
  templatesFromWorkout: string[];
}

/**
 * Intercepts the templates API with an in-memory store, the workout list, its read by id and the catalog. Resolves
 * to the bodies sent to every endpoint the flows assert on.
 */
async function signIn(page: Page) {
  const templates = new Map<string, StoredTemplate>([[legs.id, legs]]);
  const sent: SentRequests = {
    templateCreations: [],
    templateExercises: [],
    workoutsFromTemplate: [],
    templatesFromWorkout: [],
  };

  await mockUsersMe(page);
  await page.route(new RegExp(`^${api}/workout-templates(\\?.*)?$`), async (route) => {
    if (await preflight(route)) {
      return;
    }
    const request = route.request();
    if (request.method() === "POST") {
      const body = request.postDataJSON() as Partial<StoredTemplate>;
      sent.templateCreations.push(body);
      const created: StoredTemplate = {
        id: createdTemplateId,
        name: body.name ?? "",
        description: body.description ?? null,
        trainerId,
        exercises: [],
      };
      templates.set(created.id, created);
      return route.fulfill(json(created, 201));
    }
    return route.fulfill(json([...templates.values()]));
  });
  await page.route(`${api}/workout-templates/*`, async (route) => {
    if (await preflight(route)) {
      return;
    }
    const id = new URL(route.request().url()).pathname.split("/").pop() ?? "";
    const template = templates.get(id);
    return route.fulfill(template ? json(template) : json({ status: 404 }, 404));
  });
  await page.route(`${api}/workout-templates/*/exercises`, async (route) => {
    if (await preflight(route)) {
      return;
    }
    const templateId = new URL(route.request().url()).pathname.split("/")[2] ?? "";
    const template = templates.get(templateId)!;
    const body = route.request().postDataJSON() as {
      exerciseId: string;
      effort: string;
      sets: { count: number; targetType: string; target?: number }[];
    };
    sent.templateExercises.push(body);
    const entry = {
      id: "0a1b2c3d-4e5f-4a6b-8c7d-9e0f1a2b3c4d",
      exerciseId: body.exerciseId,
      effort: body.effort,
      perSide: false,
      restMinutes: null,
      superSeriesId: null,
      sets: body.sets.map((set) => ({
        count: set.count,
        target: { type: set.targetType, value: set.target ?? null, maxValue: null },
      })),
    };
    templates.set(templateId, { ...template, exercises: [...template.exercises, entry] });
    return route.fulfill(json(entry, 201));
  });
  await page.route(`${api}/workout-templates/*/workouts`, async (route) => {
    if (await preflight(route)) {
      return;
    }
    const templateId = new URL(route.request().url()).pathname.split("/")[2] ?? "";
    sent.workoutsFromTemplate.push({ templateId, body: route.request().postDataJSON() });
    return route.fulfill(json({ ...workout, name: legs.name }, 201));
  });
  await page.route(`${api}/workouts/*/templates`, async (route) => {
    if (await preflight(route)) {
      return;
    }
    sent.templatesFromWorkout.push(new URL(route.request().url()).pathname.split("/")[2] ?? "");
    return route.fulfill(json({ ...legs, id: createdTemplateId, name: workout.name }, 201));
  });
  await page.route(new RegExp(`^${api}/workouts(\\?.*)?$`), async (route) => {
    if (await preflight(route)) {
      return;
    }
    return route.fulfill(json([{ ...workout, athlete, trainer: null }]));
  });
  await page.route(`${api}/workouts/*`, async (route) => {
    if (await preflight(route)) {
      return;
    }
    return route.fulfill(json({ ...workout, athlete, trainer: null }));
  });
  await page.route(`${api}/trainer-athletes`, async (route) => {
    if (await preflight(route)) {
      return;
    }
    return route.fulfill(
      json([
        {
          id: "rel-1",
          athleteId,
          email: athlete.email,
          name: athlete.name,
          surname: athlete.surname,
          trainerName: "Ana",
          trainerSurname: "García",
          startedAtUtc: "2026-10-01T09:00:00Z",
        },
      ]),
    );
  });
  await page.route(`${api}/exercises`, async (route) => {
    if (await preflight(route)) {
      return;
    }
    return route.fulfill(json([squat]));
  });
  await page.route(`${api}/exercise-favorites`, async (route) => {
    if (await preflight(route)) {
      return;
    }
    return route.fulfill(json([]));
  });
  await page.addInitScript(() => {
    window.localStorage.setItem("auth.accessToken", "access");
    window.localStorage.setItem("auth.refreshToken", "refresh");
  });
  return sent;
}

test("opens the templates from the drawer, creates one and adds an exercise to it", async ({
  page,
}) => {
  // Arrange
  const sent = await signIn(page);
  await page.goto("/");
  await expect(page.getByText("Ana García")).toBeVisible();
  await page.getByRole("link", { name: "Plantillas de entrenamiento" }).click();
  await expect(page).toHaveURL(/\/workout-templates\/?$/);
  await expect(page.getByRole("row", { name: /Pierna A.*Fuerza/ })).toBeVisible();
  await page.getByRole("button", { name: "Nueva plantilla" }).click();
  const createDialog = page.getByRole("dialog", { name: "Nueva plantilla" });
  await createDialog.getByRole("textbox", { name: "Nombre" }).fill("  Torso  ");
  await createDialog.getByRole("button", { name: "Crear" }).click();
  await expect(page).toHaveURL(new RegExp(`/workout-template/?\\?id=${createdTemplateId}`));
  await expect(page.getByRole("heading", { level: 1, name: "Editar plantilla" })).toBeVisible();
  await page.getByRole("button", { name: "Añadir ejercicio" }).click();
  await page
    .getByRole("dialog", { name: "Catálogo de ejercicios" })
    .getByRole("listitem")
    .filter({ hasText: "Sentadilla" })
    .getByRole("button", { name: "Elegir ejercicio" })
    .click();
  const configuration = page.getByRole("dialog", { name: "Configuración del ejercicio" });
  await configuration.getByRole("combobox", { name: "Esfuerzo" }).click();
  await page.getByRole("option", { name: "RIR 0", exact: true }).click();
  const form = configuration.getByRole("group", { name: "Añadir serie" });
  await form.getByRole("spinbutton", { name: "Series" }).fill("3");
  await form.getByRole("spinbutton", { name: "Valor" }).fill("10");
  await configuration.getByRole("button", { name: "Añadir serie" }).click();

  // Act
  await configuration.getByRole("button", { name: "Guardar" }).click();

  // Assert
  await expect(
    page.getByRole("region", { name: "Ejercicios" }).getByText("Sentadilla"),
  ).toBeVisible();
  expect(sent.templateCreations).toEqual([{ name: "Torso" }]);
  expect(sent.templateExercises).toEqual([
    expect.objectContaining({
      exerciseId: squat.id,
      effort: "HIGH",
      sets: [{ count: 3, targetType: "Reps", target: 10 }],
    }),
  ]);
});

test("creates a standalone workout from a template and lands on it", async ({ page }) => {
  // Arrange
  await page.clock.setFixedTime(today);
  const sent = await signIn(page);
  await page.goto("/assigned-workouts/");
  await page.getByRole("button", { name: "Nuevo entrenamiento" }).click();
  const dialog = page.getByRole("dialog", { name: "Nuevo entrenamiento" });
  await dialog.getByRole("button", { name: "Desde plantilla" }).click();
  await dialog.getByRole("button", { name: /Pierna A/ }).click();
  await dialog.getByRole("combobox", { name: "Atleta" }).click();
  await page.getByRole("option", { name: "Bea López · bea@example.com" }).click();

  // Act
  await dialog.getByRole("button", { name: "Crear" }).click();

  // Assert
  await expect(page).toHaveURL(new RegExp(`/workout/?\\?id=${workout.id}`));
  expect(sent.workoutsFromTemplate).toEqual([
    { templateId: legs.id, body: { userId: athleteId, workoutDate: "2026-10-28" } },
  ]);
});

test("saves a workout as a template and links to it", async ({ page }) => {
  // Arrange
  const sent = await signIn(page);
  await page.goto(`/workout?id=${workout.id}`);
  const properties = page.getByRole("region", { name: "Propiedades del entrenamiento" });

  // Act
  await properties.getByRole("button", { name: "Guardar como plantilla" }).click();

  // Assert
  await expect(page.getByText("Plantilla creada.")).toBeVisible();
  await expect(page.getByRole("link", { name: "Ver plantilla" })).toHaveAttribute(
    "href",
    new RegExp(`/workout-template/?\\?id=${createdTemplateId}`),
  );
  expect(sent.templatesFromWorkout).toEqual([workout.id]);
});
