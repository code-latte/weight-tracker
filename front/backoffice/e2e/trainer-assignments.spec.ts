import { expect, type Page, test } from "@playwright/test";
import { completeProfile, mockUsersMe } from "./support/users-me";

const usersUrl = "http://localhost:5199/users";
const trainerAthletesUrl = "http://localhost:5199/trainer-athletes";
const cors = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, content-type",
  "Access-Control-Allow-Methods": "GET, POST, PUT, OPTIONS",
};

const adminProfile = { ...completeProfile, email: "admin@example.com", role: "ADMIN" };

const person = (id: string, email: string, name: string, surname: string, role: string) => ({
  id,
  email,
  name,
  surname,
  role,
  isActive: true,
  deactivatedAtUtc: null,
  trainer: null as null | {
    id: string;
    email: string;
    name: string;
    surname: string;
    role: string;
  },
});

const ana = person("trainer-1", "ana@example.com", "Ana", "García", "TRAINER");
const luis = {
  ...person("athlete-1", "luis@example.com", "Luis", "Pérez", "USER"),
  trainer: { id: ana.id, email: ana.email, name: ana.name, surname: ana.surname, role: ana.role },
};
const marta = person("athlete-2", "marta@example.com", "Marta", "Anaya", "USER");
const adminUser = person(adminProfile.id, adminProfile.email, "Eva", "Martín", "ADMIN");

const relationship = (trainerId: string, athleteId: string, status: string) => ({
  id: `${trainerId}-${athleteId}`,
  trainerId,
  trainerName: "Ana",
  trainerSurname: "García",
  athleteId,
  athleteName: "",
  athleteSurname: "",
  status,
  startedAtUtc: "2026-10-01T09:00:00Z",
  finishedAtUtc: status === "ENDED" ? "2026-11-02T09:00:00Z" : null,
});

/** Intercepts `GET /users` and the trainer–athlete link and end routes, keeping the list in step with them. */
async function mockAssignments(page: Page) {
  let list = [adminUser, luis, ana, marta];
  const linked: unknown[] = [];
  const ended: unknown[] = [];
  const trainerOf = (id: string) => {
    const trainer = list.find((item) => item.id === id)!;
    return {
      id: trainer.id,
      email: trainer.email,
      name: trainer.name,
      surname: trainer.surname,
      role: trainer.role,
    };
  };

  await page.route(usersUrl, (route) =>
    route.request().method() === "OPTIONS"
      ? route.fulfill({ status: 204, headers: cors })
      : route.fulfill({
          status: 200,
          headers: cors,
          contentType: "application/json",
          body: JSON.stringify(list),
        }),
  );
  await page.route(`${trainerAthletesUrl}/*`, (route) => {
    const request = route.request();
    if (request.method() === "OPTIONS") {
      return route.fulfill({ status: 204, headers: cors });
    }
    const trainerId = new URL(request.url()).pathname.split("/")[2]!;
    const body = request.postDataJSON() as { athleteId: string };
    linked.push({ trainerId, ...body });
    list = list.map((item) =>
      item.id === body.athleteId ? { ...item, trainer: trainerOf(trainerId) } : item,
    );
    return route.fulfill({
      status: 201,
      headers: cors,
      contentType: "application/json",
      body: JSON.stringify(relationship(trainerId, body.athleteId, "ACTIVE")),
    });
  });
  await page.route(`${trainerAthletesUrl}/*/end`, (route) => {
    const request = route.request();
    if (request.method() === "OPTIONS") {
      return route.fulfill({ status: 204, headers: cors });
    }
    const trainerId = new URL(request.url()).pathname.split("/")[2]!;
    const body = request.postDataJSON() as { athleteId: string };
    ended.push({ trainerId, ...body });
    list = list.map((item) => (item.id === body.athleteId ? { ...item, trainer: null } : item));
    return route.fulfill({
      status: 200,
      headers: cors,
      contentType: "application/json",
      body: JSON.stringify(relationship(trainerId, body.athleteId, "ENDED")),
    });
  });
  return { linked, ended };
}

async function openFromDrawer(page: Page) {
  await page.addInitScript(() => {
    window.localStorage.setItem("auth.accessToken", "access");
    window.localStorage.setItem("auth.refreshToken", "refresh");
  });
  await page.goto("/");
  await page.getByRole("link", { name: "Asignación de entrenadores" }).click();
  await expect(page).toHaveURL(/\/trainer-assignments\/?$/);
}

test("an admin assigns a trainer to an athlete without one", async ({ page }) => {
  // Arrange
  await mockUsersMe(page, adminProfile);
  const { linked } = await mockAssignments(page);
  await openFromDrawer(page);
  await expect(page.getByRole("row", { name: /Marta Anaya.*Sin entrenador/ })).toBeVisible();
  await page.getByRole("button", { name: "Acciones de Marta Anaya" }).click();
  await page.getByRole("menuitem", { name: "Asignar entrenador" }).click();
  const dialog = page.getByRole("dialog", { name: "Asignar entrenador a Marta Anaya" });
  await dialog.getByRole("combobox", { name: "Entrenador" }).fill("garcia");
  await page.getByRole("option", { name: "Ana García · ana@example.com" }).click();

  // Act
  await dialog.getByRole("button", { name: "Asignar" }).click();

  // Assert
  await expect(page.getByText("Entrenador asignado.")).toBeVisible();
  await expect(page.getByRole("row", { name: /Marta Anaya.*Ana García/ })).toBeVisible();
  expect(linked).toEqual([{ trainerId: ana.id, athleteId: marta.id }]);
});

test("an admin ends the relationship of an athlete with a trainer", async ({ page }) => {
  // Arrange
  await mockUsersMe(page, adminProfile);
  const { ended } = await mockAssignments(page);
  await openFromDrawer(page);
  await page.getByRole("button", { name: "Acciones de Luis Pérez" }).click();
  await page.getByRole("menuitem", { name: "Finalizar relación" }).click();

  // Act
  await page
    .getByRole("dialog", { name: "¿Finalizar la relación con Luis Pérez?" })
    .getByRole("button", { name: "Finalizar" })
    .click();

  // Assert
  await expect(page.getByText("Relación finalizada.")).toBeVisible();
  await expect(page.getByRole("row", { name: /Luis Pérez.*Sin entrenador/ })).toBeVisible();
  expect(ended).toEqual([{ trainerId: ana.id, athleteId: luis.id }]);
});
