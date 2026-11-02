import { expect, test } from "@playwright/test";
import { completeProfile, mockUsersMe } from "./support/users-me";

const invitationsUrl = "http://localhost:5199/athlete-invitations";
const cors = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, content-type",
  "Access-Control-Allow-Methods": "GET, POST, PUT, OPTIONS",
};

const adminProfile = { ...completeProfile, email: "admin@example.com", role: "ADMIN" };

const invitation = (
  id: string,
  inviteEmail: string,
  status: string,
  trainer = ["Ana", "García"],
) => ({
  id,
  trainerId: `${trainer[0]}-id`,
  trainerName: trainer[0],
  trainerSurname: trainer[1],
  inviteEmail,
  userId: null,
  userName: null,
  userSurname: null,
  status,
  createdAtUtc: "2026-10-28T10:00:00Z",
  expiresAtUtc: "2026-11-04T10:00:00Z",
  acceptedAtUtc: null,
});

test.beforeEach(async ({ page }) => {
  await page.addInitScript(() => {
    window.localStorage.setItem("auth.accessToken", "access");
    window.localStorage.setItem("auth.refreshToken", "refresh");
  });
});

test("a trainer invites an athlete and revokes a pending invitation", async ({ page }) => {
  // Arrange
  let list = [invitation("i1", "luis@example.com", "PENDING")];
  let inviteBody: unknown = null;
  const revoked: string[] = [];
  await mockUsersMe(page);
  await page.route(invitationsUrl, (route) => {
    const request = route.request();

    if (request.method() === "OPTIONS") {
      return route.fulfill({ status: 204, headers: cors });
    }
    if (request.method() === "POST") {
      inviteBody = request.postDataJSON();
      const created = invitation("i2", "nuevo@example.com", "PENDING");
      list = [created, ...list];
      return route.fulfill({
        status: 201,
        headers: cors,
        contentType: "application/json",
        body: JSON.stringify({ ...created, token: "raw-token" }),
      });
    }
    return route.fulfill({
      status: 200,
      headers: cors,
      contentType: "application/json",
      body: JSON.stringify(list),
    });
  });
  await page.route(`${invitationsUrl}/*/revoke`, (route) => {
    const request = route.request();

    if (request.method() === "OPTIONS") {
      return route.fulfill({ status: 204, headers: cors });
    }
    const id = new URL(request.url()).pathname.split("/")[2]!;
    revoked.push(id);
    list = list.map((item) => (item.id === id ? { ...item, status: "REVOKED" } : item));
    return route.fulfill({
      status: 200,
      headers: cors,
      contentType: "application/json",
      body: JSON.stringify(list.find((item) => item.id === id)),
    });
  });
  await page.goto("/");
  await page.getByRole("link", { name: "Invitaciones" }).click();
  await expect(page).toHaveURL(/\/invitations\/?$/);
  await page.getByRole("button", { name: "Invitar atleta" }).click();
  const inviteDialog = page.getByRole("dialog", { name: "Invitar atleta" });
  await inviteDialog.getByLabel("Email").fill("nuevo@example.com");
  await inviteDialog.getByRole("button", { name: "Enviar invitación" }).click();
  await expect(page.getByText("Invitación enviada.")).toBeVisible();
  await expect(page.getByRole("row", { name: /nuevo@example\.com/ })).toBeVisible();
  await page
    .getByRole("row", { name: /luis@example\.com/ })
    .getByRole("button", { name: "Revocar" })
    .click();
  const revokeDialog = page.getByRole("dialog", {
    name: "¿Revocar la invitación a luis@example.com?",
  });

  // Act
  await revokeDialog.getByRole("button", { name: "Revocar" }).click();

  // Assert
  await expect(page.getByText("Invitación revocada.")).toBeVisible();
  await expect(revokeDialog).toHaveCount(0);
  await expect(page.getByRole("row", { name: /luis@example\.com/ })).toContainText("Revocada");
  expect(inviteBody).toEqual({ email: "nuevo@example.com" });
  expect(revoked).toEqual(["i1"]);
});

test("an admin filters every invitation by status and trainer, with no controls", async ({
  page,
}) => {
  // Arrange
  const list = [
    invitation("i1", "luis@example.com", "PENDING"),
    invitation("i2", "marta@example.com", "REVOKED"),
    invitation("i3", "sara@example.com", "PENDING", ["José", "Ruiz"]),
  ];
  await mockUsersMe(page, adminProfile);
  await page.route(invitationsUrl, (route) =>
    route.request().method() === "OPTIONS"
      ? route.fulfill({ status: 204, headers: cors })
      : route.fulfill({
          status: 200,
          headers: cors,
          contentType: "application/json",
          body: JSON.stringify(list),
        }),
  );
  await page.goto("/");
  await page.getByRole("link", { name: "Invitaciones" }).click();
  await expect(page.getByRole("row", { name: /marta@example\.com/ })).toBeVisible();
  await page.getByRole("button", { name: "Pendiente", exact: true }).click();
  await expect(page.getByRole("row", { name: /marta@example\.com/ })).toHaveCount(0);

  // Act
  await page.getByRole("searchbox", { name: "Buscar por entrenador o email" }).fill("jose ruiz");

  // Assert
  await expect(page.getByRole("row", { name: /José Ruiz/ })).toBeVisible();
  await expect(page.getByRole("row", { name: /luis@example\.com/ })).toHaveCount(0);
  await expect(page.getByRole("button", { name: "Invitar atleta" })).toHaveCount(0);
  await expect(page.getByRole("button", { name: "Revocar" })).toHaveCount(0);
});
