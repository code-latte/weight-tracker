import { expect, type Page, test } from "@playwright/test";
import { mockMesocyclesApi, type StoredMesocycle } from "./support/mesocycles-api";
import { completeProfile, mockUsersMe } from "./support/users-me";

const adminProfile = { ...completeProfile, email: "admin@example.com", role: "ADMIN" };

const anaId = "5f1c2f0e-9d5b-4a53-8c7e-0d6f7c1a2b3c";
const luisId = "7b3e4a2f-1f7d-4c75-8e90-2f8b9e3c4d5e";

const ana = { id: anaId, email: "ana@example.com", name: "Ana", surname: "García", role: "USER" };
const luis = {
  id: luisId,
  email: "luis@example.com",
  name: "Luis",
  surname: "Pérez",
  role: "TRAINER",
};

const hypertrophy: StoredMesocycle = {
  id: "7b0c1f7e-3a54-4d4e-9d0e-1a2b3c4d5e6f",
  trainerId: luisId,
  athleteId: anaId,
  name: "Hipertrofia",
  objective: "Ganar masa muscular",
  startDate: "2026-11-03",
  durationInWeeks: 4,
  weeks: [{ weekNumber: 1, trainingWeekType: "ADAPTATION", objective: "Cargas iniciales" }],
  trainer: luis,
  athlete: ana,
};

async function mockAssignedPlans(page: Page, profile = completeProfile) {
  await mockUsersMe(page, profile);
  await mockMesocyclesApi(page, { initial: [hypertrophy], people: [ana, luis], trainer: luis });
  await page.addInitScript(() => {
    window.localStorage.setItem("auth.accessToken", "access");
    window.localStorage.setItem("auth.refreshToken", "refresh");
  });
}

test("opens Mesociclos from the drawer and lists the mesocycles", async ({ page }) => {
  // Arrange
  await mockAssignedPlans(page);
  await page.goto("/");
  await expect(page.getByText("Ana García")).toBeVisible();

  // Act
  await page.getByRole("link", { name: "Mesociclos" }).click();

  // Assert
  await expect(page).toHaveURL(/\/assigned-plans\/?$/);
  await expect(page.getByRole("heading", { level: 1, name: "Mesociclos" })).toBeVisible();
  await expect(
    page.getByRole("row", { name: /Hipertrofia.*Ana García.*3 nov 2026.*4 semanas.*1 \/ 4/ }),
  ).toBeVisible();
  await expect(page.getByRole("columnheader", { name: "Entrenador" })).toHaveCount(0);
  await expect(page.getByText(anaId)).toHaveCount(0);
});

test("shows an admin the trainer of each mesocycle", async ({ page }) => {
  // Arrange
  await mockAssignedPlans(page, adminProfile);

  // Act
  await page.goto("/assigned-plans/");

  // Assert
  await expect(page.getByRole("columnheader", { name: "Entrenador" })).toBeVisible();
  await expect(
    page.getByRole("row", { name: /Hipertrofia.*Ana García.*Luis Pérez/ }),
  ).toBeVisible();
});

test("opens a mesocycle from the table's edit action", async ({ page }) => {
  // Arrange
  await mockAssignedPlans(page);
  await page.goto("/assigned-plans/");

  // Act
  await page.getByRole("link", { name: "Editar Hipertrofia" }).click();

  // Assert
  await expect(page).toHaveURL(new RegExp(`/mesocycle/?\\?id=${hypertrophy.id}$`));
  await expect(page.getByRole("heading", { level: 1, name: "Editar mesociclo" })).toBeVisible();
  await expect(page.getByRole("textbox", { name: "Nombre" })).toHaveValue("Hipertrofia");
});
