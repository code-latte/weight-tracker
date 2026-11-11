import { expect, type Page, test } from "@playwright/test";

const leadsUrl = "http://localhost:5199/customer-leads";
const cors = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "content-type",
  "Access-Control-Allow-Methods": "POST, OPTIONS",
};

/** Intercepts `POST /customer-leads`, answering `status`, and records every body sent. */
async function mockLeads(page: Page, status: number) {
  const bodies: unknown[] = [];
  await page.route(leadsUrl, (route) => {
    const request = route.request();
    if (request.method() === "OPTIONS") {
      return route.fulfill({ status: 204, headers: cors });
    }
    bodies.push(request.postDataJSON());
    return route.fulfill({ status, headers: cors });
  });
  return bodies;
}

const confirmation =
  "Hemos recibido tu solicitud. Nos pondremos en contacto contigo para conocer mejor tu forma de trabajar y explicarte cómo participar en Trainera.";

test("goes from the hero to the form and registers the email", async ({ page }) => {
  // Arrange
  const bodies = await mockLeads(page, 204);
  await page.goto("/");
  await page
    .getByRole("region", { name: "Menos gestión. Más entrenamiento." })
    .getByRole("link", { name: "Solicitar acceso anticipado" })
    .click();
  await expect(page).toHaveURL(/#acceso-anticipado$/);
  await page.getByRole("textbox", { name: "Email" }).fill("ana@example.com");
  await page.getByRole("checkbox", { name: /política de privacidad/ }).check();

  // Act
  await page.getByRole("button", { name: "Quiero participar" }).click();

  // Assert
  await expect(page.getByText(confirmation)).toBeVisible();
  expect(bodies).toEqual([{ email: "ana@example.com" }]);
});

test("says to wait when the API refuses too many requests", async ({ page }) => {
  // Arrange
  await mockLeads(page, 429);
  await page.goto("/#acceso-anticipado");
  await page.getByRole("textbox", { name: "Email" }).fill("ana@example.com");
  await page.getByRole("checkbox", { name: /política de privacidad/ }).check();

  // Act
  await page.getByRole("button", { name: "Quiero participar" }).click();

  // Assert
  await expect(page.locator("#acceso-anticipado").getByRole("alert")).toHaveText(
    "Has enviado demasiadas solicitudes. Inténtalo de nuevo en unos minutos.",
  );
  await expect(page.getByRole("textbox", { name: "Email" })).toHaveValue("ana@example.com");
});

test("reaches the FAQ from the menu on a phone", async ({ page }) => {
  // Arrange
  await page.setViewportSize({ width: 375, height: 812 });
  await page.goto("/");
  await page.getByRole("button", { name: "Abrir menú" }).click();

  // Act
  await page.locator("#menu-movil").getByRole("link", { name: "FAQ" }).click();

  // Assert
  await expect(page).toHaveURL(/#faq$/);
  await expect(page.locator("#menu-movil")).toBeHidden();
  await expect(
    page.getByRole("heading", { level: 2, name: "Preguntas frecuentes" }),
  ).toBeInViewport();
});

test("has no horizontal scroll on a 360 px wide screen", async ({ page }) => {
  // Arrange
  await page.setViewportSize({ width: 360, height: 800 });

  // Act
  await page.goto("/");

  // Assert
  const widths = await page.evaluate(() => ({
    scroll: document.documentElement.scrollWidth,
    viewport: document.documentElement.clientWidth,
  }));
  expect(widths.scroll).toBe(widths.viewport);
});

test("keeps the header on screen and compacts it once the page scrolls", async ({ page }) => {
  // Arrange
  await page.goto("/");
  const header = page.locator("header[data-compact]");
  await expect(header).toHaveAttribute("data-compact", "false");

  // Act
  await page.mouse.wheel(0, 1200);

  // Assert
  await expect(header).toHaveAttribute("data-compact", "true");
  await expect(header).toBeInViewport();
});

test("shows the pending notice on a legal page without its text", async ({ page }) => {
  // Arrange & Act
  await page.goto("/cookies");

  // Assert
  await expect(page.getByRole("heading", { level: 1, name: "Política de cookies" })).toBeVisible();
  await expect(page.getByRole("heading", { name: "Contenido pendiente" })).toBeVisible();
});

test("serves the sitemap and robots.txt", async ({ request }) => {
  // Arrange & Act
  const [sitemap, robots] = await Promise.all([
    request.get("/sitemap.xml"),
    request.get("/robots.txt"),
  ]);

  // Assert
  expect(sitemap.ok()).toBe(true);
  const urls = await sitemap.text();
  for (const path of ["/aviso-legal", "/privacidad", "/cookies", "/condiciones"]) {
    expect(urls).toContain(`${path}</loc>`);
  }
  expect(robots.ok()).toBe(true);
  expect(await robots.text()).toContain("Allow: /");
});
