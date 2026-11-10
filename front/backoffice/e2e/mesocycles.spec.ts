import { expect, test } from "@playwright/test";
import {
  mockMesocyclesApi,
  mockWeekWorkoutsApi,
  type StoredMesocycle,
  type StoredWorkout,
} from "./support/mesocycles-api";
import { completeProfile, mockUsersMe } from "./support/users-me";

const api = "http://localhost:5199";
const cors = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, content-type",
  "Access-Control-Allow-Methods": "GET, OPTIONS",
};

const beaId = "6a2d3f1e-0e6c-4b64-9d8f-1e7a8d2b3c4d";
const newId = "9d2e3f4a-5b6c-4d7e-8f9a-0b1c2d3e4f5a";

/** The signed-in trainer, as the API returns them inside a mesocycle. */
const trainer = { ...completeProfile };
const bea = { id: beaId, email: "bea@example.com", name: "Bea", surname: "López", role: "USER" };

test("creates a mesocycle, then defines and redefines one of its weeks", async ({ page }) => {
  // Arrange
  await mockUsersMe(page);
  const sent = await mockMesocyclesApi(page, { initial: [], people: [bea], trainer, newId });
  await page.route(`${api}/trainer-athletes`, (route) =>
    route.request().method() === "OPTIONS"
      ? route.fulfill({ status: 204, headers: cors })
      : route.fulfill({
          status: 200,
          headers: cors,
          contentType: "application/json",
          body: JSON.stringify([
            {
              id: `rel-${beaId}`,
              athleteId: beaId,
              email: bea.email,
              name: bea.name,
              surname: bea.surname,
              trainerName: trainer.name,
              trainerSurname: trainer.surname,
              startedAtUtc: "2026-10-01T09:00:00Z",
            },
          ]),
        }),
  );
  await page.addInitScript(() => {
    window.localStorage.setItem("auth.accessToken", "access");
    window.localStorage.setItem("auth.refreshToken", "refresh");
  });
  await page.goto("/");
  await page.getByRole("link", { name: "Mesociclos" }).click();
  await expect(page.getByText("Todavía no hay mesociclos.")).toBeVisible();
  await page.getByRole("link", { name: "Nuevo mesociclo" }).click();
  await expect(page.getByRole("heading", { level: 1, name: "Crear mesociclo" })).toBeVisible();
  await page.getByRole("combobox", { name: "Atleta" }).click();
  await page.getByRole("option", { name: "Bea López · bea@example.com" }).click();
  await page.getByRole("textbox", { name: "Nombre" }).fill("Hipertrofia");
  await page.getByLabel(/Fecha de inicio/).fill("2026-11-03");
  await page.getByRole("textbox", { name: "Objetivo general" }).fill("Ganar masa muscular");

  // Act
  await page.getByRole("button", { name: "Crear mesociclo" }).click();
  await expect(page).toHaveURL(new RegExp(`/mesocycle/?\\?id=${newId}$`));
  await expect(page.getByRole("heading", { level: 1, name: "Editar mesociclo" })).toBeVisible();
  await page.getByRole("button", { name: "Añadir semana" }).click();
  const define = page.getByRole("dialog", { name: "Semana 1" });
  await define.getByRole("combobox", { name: "Tipo de semana" }).click();
  await page.getByRole("option", { name: "Adaptación" }).click();
  await define.getByRole("textbox", { name: "Objetivo" }).fill("Cargas iniciales");
  await define.getByRole("button", { name: "Guardar" }).click();
  await expect(define).toBeHidden();
  await page.getByRole("button", { name: "Editar semana 1" }).click();
  const redefine = page.getByRole("dialog", { name: "Semana 1" });
  await redefine.getByRole("combobox", { name: "Tipo de semana" }).click();
  await page.getByRole("option", { name: "Descarga" }).click();
  await redefine.getByRole("button", { name: "Guardar" }).click();

  // Assert
  await expect(redefine).toBeHidden();
  const weeks = page.getByRole("region", { name: "Planificación semanal" });
  await expect(weeks.getByRole("listitem")).toHaveCount(1);
  await expect(weeks.getByRole("listitem")).toContainText("Descarga");
  await expect(weeks.getByRole("listitem")).toContainText("Cargas iniciales");
  expect(sent).toEqual([
    {
      method: "POST",
      path: "/mesocycles",
      body: {
        athleteId: beaId,
        name: "Hipertrofia",
        objective: "Ganar masa muscular",
        startDate: "2026-11-03",
        durationInWeeks: 4,
      },
    },
    {
      method: "POST",
      path: `/mesocycles/${newId}/weeks`,
      body: { weekNumber: 1, trainingWeekType: "ADAPTATION", objective: "Cargas iniciales" },
    },
    {
      method: "PUT",
      path: `/mesocycles/${newId}/weeks/1`,
      body: { trainingWeekType: "DELOAD", objective: "Cargas iniciales" },
    },
  ]);
});

test("creates a workout in a mesocycle week, opens it and goes back to the week", async ({
  page,
}) => {
  // Arrange
  const mesocycleId = "7b0c1f7e-3a54-4d4e-9d0e-1a2b3c4d5e6f";
  const workoutId = "8c1d2e3f-4a5b-4c6d-8e7f-9a0b1c2d3e4f";
  const hypertrophy: StoredMesocycle = {
    id: mesocycleId,
    trainerId: trainer.id,
    athleteId: beaId,
    name: "Hipertrofia",
    objective: "Ganar masa muscular",
    startDate: "2026-11-02",
    durationInWeeks: 4,
    weeks: [
      { weekNumber: 1, trainingWeekType: "ADAPTATION", objective: "Cargas iniciales" },
      { weekNumber: 2, trainingWeekType: "ACCUMULATION", objective: "Subir volumen" },
    ],
    trainer,
    athlete: bea,
  };
  const workouts: StoredWorkout[] = [];
  await mockUsersMe(page);
  await mockMesocyclesApi(page, { initial: [hypertrophy], people: [bea], trainer, workouts });
  const sent = await mockWeekWorkoutsApi(page, {
    workouts,
    people: [bea, trainer],
    newId: workoutId,
    workoutDate: "2026-11-11",
    mesocycles: [hypertrophy],
  });
  await page.addInitScript(() => {
    window.localStorage.setItem("auth.accessToken", "access");
    window.localStorage.setItem("auth.refreshToken", "refresh");
  });
  await page.goto(`/mesocycle?id=${mesocycleId}`);
  const weeks = page.getByRole("region", { name: "Planificación semanal" });
  const secondWeek = weeks.getByRole("listitem").filter({ hasText: "Semana 2" });
  await secondWeek.getByRole("button", { name: "Nuevo entrenamiento" }).click();
  const dialog = page.getByRole("dialog", { name: "Nuevo entrenamiento · Semana 2" });
  await dialog.getByRole("textbox", { name: "Nombre" }).fill("Pierna");

  // Act
  await dialog.getByRole("button", { name: "Crear" }).click();
  await expect(page).toHaveURL(new RegExp(`/workout/?\\?id=${workoutId}$`));
  const context = page.getByRole("region", { name: "Datos del entrenamiento" });
  await expect(context).toContainText("Bea López");
  await expect(context.getByRole("navigation", { name: "Mesociclo y semana" })).toHaveText(
    "Hipertrofia/Semana 2",
  );
  await expect(context.getByRole("link")).toHaveCount(0);
  await page.getByRole("link", { name: "Volver", exact: true }).click();
  await expect(page).toHaveURL(new RegExp(`/mesocycle/?\\?id=${mesocycleId}$`));
  await secondWeek.getByRole("button", { name: "Mostrar entrenamientos de la semana 2" }).click();

  // Assert
  await expect(secondWeek).toContainText("1 entrenamiento");
  await expect(secondWeek.getByRole("link", { name: "Editar Pierna" })).toHaveAttribute(
    "href",
    new RegExp(`/workout/?\\?id=${workoutId}$`),
  );
  expect(sent).toEqual([
    {
      method: "POST",
      path: "/workouts",
      body: { name: "Pierna", userId: beaId, mesocycleId, mesocycleWeekNumber: 2 },
    },
  ]);
});
