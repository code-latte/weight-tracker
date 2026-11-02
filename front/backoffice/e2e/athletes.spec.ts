import { expect, test } from "@playwright/test";
import { completeProfile, mockUsersMe } from "./support/users-me";

const athletesUrl = "http://localhost:5199/trainer-athletes";
const cors = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, content-type",
  "Access-Control-Allow-Methods": "GET, PUT, OPTIONS",
};

const athlete = (id: string, email: string, name: string, surname: string) => ({
  id,
  athleteId: `${id}-athlete`,
  email,
  name,
  surname,
  trainerName: "Ana",
  trainerSurname: "García",
  startedAtUtc: "2026-10-03T08:00:00Z",
});

const athletes = [
  athlete("a1", "luis@example.com", "Luis", "Pérez"),
  athlete("a2", "marta@example.com", "Marta", "Anaya"),
];

test("a trainer opens Todos los atletas from the drawer and searches by name", async ({ page }) => {
  // Arrange
  await mockUsersMe(page);
  await page.route(athletesUrl, (route) =>
    route.request().method() === "OPTIONS"
      ? route.fulfill({ status: 204, headers: cors })
      : route.fulfill({
          status: 200,
          headers: cors,
          contentType: "application/json",
          body: JSON.stringify(athletes),
        }),
  );
  await page.addInitScript(() => {
    window.localStorage.setItem("auth.accessToken", "access");
    window.localStorage.setItem("auth.refreshToken", "refresh");
  });
  await page.goto("/");
  await page.getByRole("link", { name: "Todos los atletas" }).click();
  await expect(page).toHaveURL(/\/athletes\/?$/);
  await expect(page.getByRole("row", { name: /Luis Pérez/ })).toBeVisible();
  await expect(page.getByRole("row", { name: /Marta Anaya/ })).toBeVisible();

  // Act
  await page.getByRole("searchbox", { name: "Buscar por nombre o email" }).fill("marta");

  // Assert
  await expect(page.getByRole("row", { name: /Marta Anaya/ })).toBeVisible();
  await expect(page.getByRole("row", { name: /Luis Pérez/ })).toHaveCount(0);
});

test("a trainer ends the relationship with an athlete from Todos los atletas", async ({ page }) => {
  // Arrange
  let list = athletes;
  const ended: unknown[] = [];
  await mockUsersMe(page);
  await page.route(athletesUrl, (route) =>
    route.request().method() === "OPTIONS"
      ? route.fulfill({ status: 204, headers: cors })
      : route.fulfill({
          status: 200,
          headers: cors,
          contentType: "application/json",
          body: JSON.stringify(list),
        }),
  );
  await page.route(`${athletesUrl}/${completeProfile.id}/end`, (route) => {
    const request = route.request();
    if (request.method() === "OPTIONS") {
      return route.fulfill({ status: 204, headers: cors });
    }
    const body = request.postDataJSON() as { athleteId: string };
    ended.push(body);
    const athlete = list.find((item) => item.athleteId === body.athleteId)!;
    list = list.filter((item) => item.athleteId !== body.athleteId);
    return route.fulfill({
      status: 200,
      headers: cors,
      contentType: "application/json",
      body: JSON.stringify({
        id: athlete.id,
        trainerId: completeProfile.id,
        trainerName: "Ana",
        trainerSurname: "García",
        athleteId: athlete.athleteId,
        athleteName: athlete.name,
        athleteSurname: athlete.surname,
        status: "ENDED",
        startedAtUtc: athlete.startedAtUtc,
        finishedAtUtc: "2026-11-02T09:00:00Z",
      }),
    });
  });
  await page.addInitScript(() => {
    window.localStorage.setItem("auth.accessToken", "access");
    window.localStorage.setItem("auth.refreshToken", "refresh");
  });
  await page.goto("/athletes");
  await page.getByRole("button", { name: "Acciones de Luis Pérez" }).click();
  await page.getByRole("menuitem", { name: "Finalizar relación" }).click();

  // Act
  await page
    .getByRole("dialog", { name: "¿Finalizar la relación con Luis Pérez?" })
    .getByRole("button", { name: "Finalizar" })
    .click();

  // Assert
  await expect(page.getByText("Relación finalizada.")).toBeVisible();
  await expect(page.getByRole("row", { name: /Luis Pérez/ })).toHaveCount(0);
  await expect(page.getByRole("row", { name: /Marta Anaya/ })).toBeVisible();
  expect(ended).toEqual([{ athleteId: "a1-athlete" }]);
});
