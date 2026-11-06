import { expect, type Page, test } from "@playwright/test";
import { completeProfile, mockUsersMe } from "./support/users-me";

const api = "http://localhost:5199";
const cors = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, content-type",
  "Access-Control-Allow-Methods": "GET, POST, OPTIONS",
};

const adminProfile = { ...completeProfile, email: "admin@example.com", role: "ADMIN" };

const anaId = "5f1c2f0e-9d5b-4a53-8c7e-0d6f7c1a2b3c";
const beaId = "6a2d3f1e-0e6c-4b64-9d8f-1e7a8d2b3c4d";
const luisId = "7b3e4a2f-1f7d-4c75-8e90-2f8b9e3c4d5e";

const person = (id: string, email: string, name: string, surname: string, role: string) => ({
  id,
  email,
  name,
  surname,
  role,
});

const ana = person(anaId, "ana@example.com", "Ana", "García", "USER");
const bea = person(beaId, "bea@example.com", "Bea", "López", "USER");
const luis = person(luisId, "luis@example.com", "Luis", "Pérez", "TRAINER");

const workouts = [
  {
    id: "7b0c1f7e-3a54-4d4e-9d0e-1a2b3c4d5e6f",
    name: "Pierna",
    description: null,
    workoutDate: "2026-10-27",
    userId: ana.id,
    trainerId: luis.id,
    status: "DRAFT",
    exercises: [],
    athlete: ana,
    trainer: luis,
  },
  {
    id: "8c1d2e3f-4a5b-4c6d-8e7f-9a0b1c2d3e4f",
    name: "Empuje",
    description: null,
    workoutDate: "2026-11-03",
    userId: bea.id,
    trainerId: null,
    status: "DRAFT",
    exercises: [],
    athlete: bea,
    trainer: null,
  },
];

const summary = (user: ReturnType<typeof person>) => ({
  ...user,
  isActive: true,
  deactivatedAtUtc: null,
  trainer: null,
});

const trainerAthlete = (user: ReturnType<typeof person>) => ({
  id: `rel-${user.id}`,
  athleteId: user.id,
  email: user.email,
  name: user.name,
  surname: user.surname,
  trainerName: "Ana",
  trainerSurname: "García",
  startedAtUtc: "2026-10-01T09:00:00Z",
});

const json = (body: unknown) => ({
  status: 200,
  headers: cors,
  contentType: "application/json",
  body: JSON.stringify(body),
});

/** Intercepts everything the screen and its create dialog read, and records what is sent to `POST /workouts`. */
async function mockAssignedWorkouts(page: Page, profile = completeProfile) {
  const created: unknown[] = [];

  await mockUsersMe(page, profile);
  // The list is read as `?standalone=true`, so the query string is part of the match.
  await page.route(new RegExp(`^${api}/workouts(\\?.*)?$`), (route) => {
    const request = route.request();

    if (request.method() === "OPTIONS") {
      return route.fulfill({ status: 204, headers: cors });
    }
    if (request.method() === "POST") {
      created.push(request.postDataJSON());
      return route.fulfill({ ...json(workouts[0]), status: 201 });
    }
    return route.fulfill(json(workouts));
  });
  await page.route(`${api}/trainer-athletes`, (route) =>
    route.request().method() === "OPTIONS"
      ? route.fulfill({ status: 204, headers: cors })
      : route.fulfill(json([ana, bea].map(trainerAthlete))),
  );
  await page.route(`${api}/users`, (route) =>
    route.request().method() === "OPTIONS"
      ? route.fulfill({ status: 204, headers: cors })
      : route.fulfill(json([summary(ana), summary(bea), summary(luis)])),
  );
  // The workout screen the dialog opens afterwards: this test only cares that it is reached.
  await page.route(`${api}/workouts/*`, (route) =>
    route.request().method() === "OPTIONS"
      ? route.fulfill({ status: 204, headers: cors })
      : route.fulfill({ status: 404, headers: cors, contentType: "application/json", body: "{}" }),
  );
  await page.addInitScript(() => {
    window.localStorage.setItem("auth.accessToken", "access");
    window.localStorage.setItem("auth.refreshToken", "refresh");
  });

  return created;
}

test("opens Entrenamientos from the drawer and lists the workouts with names", async ({ page }) => {
  // Arrange
  await mockAssignedWorkouts(page);
  await page.goto("/");
  await expect(page.getByText("Ana García")).toBeVisible();

  const list = page.waitForRequest(
    (request) => request.url().startsWith(`${api}/workouts`) && request.method() === "GET",
  );

  // Act
  await page.getByRole("link", { name: "Entrenamientos" }).click();

  // Assert
  await expect(page).toHaveURL(/\/assigned-workouts\/?$/);
  await expect(page.getByRole("heading", { level: 1, name: "Entrenamientos" })).toBeVisible();
  await expect(page.getByRole("row", { name: /Pierna.*Ana García/ })).toBeVisible();
  await expect(page.getByRole("row", { name: /Empuje.*Bea López/ })).toBeVisible();
  await expect(page.getByRole("columnheader", { name: "Entrenador" })).toHaveCount(0);
  await expect(page.getByText(anaId)).toHaveCount(0);
  expect((await list).url()).toBe(`${api}/workouts?standalone=true`);
});

test("shows an admin the trainer of each workout, or Sin entrenador", async ({ page }) => {
  // Arrange
  await mockAssignedWorkouts(page, adminProfile);

  // Act
  await page.goto("/assigned-workouts/");

  // Assert
  await expect(page.getByRole("columnheader", { name: "Entrenador" })).toBeVisible();
  await expect(page.getByRole("row", { name: /Pierna.*Ana García.*Luis Pérez/ })).toBeVisible();
  await expect(page.getByRole("row", { name: /Empuje.*Bea López.*Sin entrenador/ })).toBeVisible();
});

test("lets a trainer create a workout by choosing one of their athletes", async ({ page }) => {
  // Arrange
  const created = await mockAssignedWorkouts(page);
  await page.goto("/assigned-workouts/");
  await page.getByRole("button", { name: "Nuevo entrenamiento" }).click();
  const dialog = page.getByRole("dialog", { name: "Nuevo entrenamiento" });
  await dialog.getByRole("textbox", { name: "Nombre" }).fill("Torso");

  // Act
  await dialog.getByRole("combobox", { name: "Atleta" }).fill("bea@");
  await page.getByRole("option", { name: "Bea López · bea@example.com" }).click();
  await dialog.getByRole("button", { name: "Crear" }).click();

  // Assert
  await expect(page).toHaveURL(/\/workout\/?\?id=/);
  expect(created).toHaveLength(1);
  expect(created[0]).toMatchObject({ name: "Torso", userId: beaId });
  expect(created[0]).not.toHaveProperty("trainerId");
});

test("lets an admin create a workout choosing an athlete and a trainer", async ({ page }) => {
  // Arrange
  const created = await mockAssignedWorkouts(page, adminProfile);
  await page.goto("/assigned-workouts/");
  await page.getByRole("button", { name: "Nuevo entrenamiento" }).click();
  const dialog = page.getByRole("dialog", { name: "Nuevo entrenamiento" });
  await dialog.getByRole("textbox", { name: "Nombre" }).fill("Torso");
  await dialog.getByRole("combobox", { name: "Atleta" }).click();
  await page.getByRole("option", { name: "Ana García · ana@example.com" }).click();

  // Act
  await dialog.getByRole("combobox", { name: "Entrenador" }).fill("perez");
  await page.getByRole("option", { name: "Luis Pérez · luis@example.com" }).click();
  await dialog.getByRole("button", { name: "Crear" }).click();

  // Assert
  await expect(page).toHaveURL(/\/workout\/?\?id=/);
  expect(created).toHaveLength(1);
  expect(created[0]).toMatchObject({ name: "Torso", userId: anaId, trainerId: luisId });
});
