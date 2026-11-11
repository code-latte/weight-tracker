import { expect, test } from "@playwright/test";
import { completeProfile, mockUsersMe } from "./support/users-me";

const customerLeadsUrl = "http://localhost:5199/customer-leads";
const cors = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, content-type",
  "Access-Control-Allow-Methods": "GET, OPTIONS",
};

const adminProfile = { ...completeProfile, email: "admin@example.com", role: "ADMIN" };

const leads = [
  { email: "ana@example.com", registeredAtUtc: "2026-11-10T08:15:00Z" },
  { email: "luis@example.com", registeredAtUtc: "2026-11-09T17:40:00Z" },
];

test("an admin opens Leads from the drawer and sees every lead in the API's order", async ({
  page,
}) => {
  // Arrange
  await mockUsersMe(page, adminProfile);
  await page.route(customerLeadsUrl, (route) =>
    route.request().method() === "OPTIONS"
      ? route.fulfill({ status: 204, headers: cors })
      : route.fulfill({
          status: 200,
          headers: cors,
          contentType: "application/json",
          body: JSON.stringify(leads),
        }),
  );
  await page.addInitScript(() => {
    window.localStorage.setItem("auth.accessToken", "access");
    window.localStorage.setItem("auth.refreshToken", "refresh");
  });
  await page.goto("/");

  // Act
  await page.getByRole("link", { name: "Leads" }).click();

  // Assert
  await expect(page).toHaveURL(/\/customer-leads\/?$/);
  await expect(page.getByRole("heading", { level: 1, name: "Leads" })).toBeVisible();
  const rows = page.getByRole("row");
  await expect(rows).toHaveCount(3);
  await expect(rows.nth(1)).toContainText("ana@example.com");
  await expect(rows.nth(2)).toContainText("luis@example.com");
});
