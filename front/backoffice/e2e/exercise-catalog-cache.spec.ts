import { expect, type Page, type Route, test } from "@playwright/test";
import { completeProfile, mockUsersMe } from "./support/users-me";

const api = "http://localhost:5199";
const cors = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, content-type",
  "Access-Control-Allow-Methods": "GET, POST, PUT, DELETE, OPTIONS",
};

const athleteId = "2c3d4e5f-6a7b-4c8d-9e0f-1a2b3c4d5e6f";
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

const workout = {
  id: "7b0c1f7e-3a54-4d4e-9d0e-1a2b3c4d5e6f",
  name: "Empuje",
  description: "Día 2",
  workoutDate: "2026-11-04",
  userId: athleteId,
  trainerId: completeProfile.id,
  status: "DRAFT",
  mesocycleId: null,
  mesocycleWeekNumber: null,
  completedAt: null,
  reviewedAt: null,
  reviewedBy: null,
  resultsUpdatedAt: null,
  exercises: [],
  athlete,
  trainer: completeProfile,
  mesocycle: null,
};

const template = {
  id: "8d7c6b5a-4f3e-4d2c-9b1a-0f9e8d7c6b5a",
  name: "Pierna A",
  description: "Fuerza",
  trainerId: completeProfile.id,
  exercises: [],
};

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

/**
 * Signs a trainer in over an intercepted API: a workout, a template, and a catalog that stores what `POST /exercises`
 * creates. Resolves to the number of catalog reads sent so far.
 */
async function signIn(page: Page) {
  const stored: unknown[] = [squat];
  let catalogReads = 0;

  await mockUsersMe(page);
  await page.route(new RegExp(`^${api}/workouts(\\?.*)?$`), async (route) => {
    if (await preflight(route)) {
      return;
    }
    return route.fulfill(json([workout]));
  });
  await page.route(`${api}/workouts/*`, async (route) => {
    if (await preflight(route)) {
      return;
    }
    return route.fulfill(json(workout));
  });
  await page.route(new RegExp(`^${api}/workout-templates(\\?.*)?$`), async (route) => {
    if (await preflight(route)) {
      return;
    }
    return route.fulfill(json([template]));
  });
  await page.route(`${api}/workout-templates/*`, async (route) => {
    if (await preflight(route)) {
      return;
    }
    return route.fulfill(json(template));
  });
  await page.route(`${api}/trainer-athletes`, async (route) => {
    if (await preflight(route)) {
      return;
    }
    return route.fulfill(json([]));
  });
  await page.route(`${api}/exercise-favorites`, async (route) => {
    if (await preflight(route)) {
      return;
    }
    return route.fulfill(json([]));
  });
  await page.route(`${api}/exercises`, async (route) => {
    if (await preflight(route)) {
      return;
    }
    const request = route.request();
    if (request.method() === "POST") {
      const body = request.postDataJSON() as {
        languageCode: string;
        name: string;
        muscle: string;
        type: string;
      };
      const created = {
        id: "9e8d7c6b-5a49-4837-9625-14033f2e1d0c",
        freeExerciseDbId: null,
        videoUrl: null,
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
    catalogReads += 1;
    return route.fulfill(json(stored));
  });
  await page.addInitScript(() => {
    window.localStorage.setItem("auth.accessToken", "access");
    window.localStorage.setItem("auth.refreshToken", "refresh");
  });

  return () => catalogReads;
}

test("reads the catalog once after sign-in and not again across the screens that use it", async ({
  page,
}) => {
  // Arrange
  const catalogReads = await signIn(page);
  await page.goto("/");
  await expect(page.getByText("Ana García")).toBeVisible();
  await expect.poll(catalogReads).toBe(1);

  // Act
  await page.getByRole("link", { name: "Catálogo de ejercicios" }).click();
  await expect(page.getByRole("list", { name: "Catálogo de ejercicios" })).toContainText(
    "Sentadilla",
  );
  await page.getByRole("link", { name: "Entrenamientos", exact: true }).click();
  await page.getByRole("link", { name: "Editar Empuje" }).click();
  await expect(page.getByRole("heading", { level: 1, name: "Editar entrenamiento" })).toBeVisible();
  await page.getByRole("link", { name: "Plantillas de entrenamiento" }).click();
  await page.getByRole("link", { name: "Editar Pierna A" }).click();
  await expect(page.getByRole("heading", { level: 1, name: "Editar plantilla" })).toBeVisible();

  // Assert
  expect(catalogReads()).toBe(1);
});

test("reads the catalog once more after the trainer creates an exercise", async ({ page }) => {
  // Arrange
  const catalogReads = await signIn(page);
  await page.goto("/");
  await page.getByRole("link", { name: "Catálogo de ejercicios" }).click();
  const cards = page.getByRole("list", { name: "Catálogo de ejercicios" }).getByRole("listitem");
  await expect(cards).toHaveCount(1);
  await expect.poll(catalogReads).toBe(1);
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
  await expect(cards).toHaveCount(2);
  await expect.poll(catalogReads).toBe(2);
  await expect(cards).toHaveCount(2);
});
