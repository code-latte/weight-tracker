import { expect, test } from "@playwright/test";
import { completeProfile, mockUsersMe } from "./support/users-me";

const usersUrl = "http://localhost:5199/users";
const cors = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, content-type",
  "Access-Control-Allow-Methods": "GET, POST, PUT, OPTIONS",
};

const adminProfile = { ...completeProfile, email: "admin@example.com", role: "ADMIN" };

const users = [
  {
    id: "5f1c2f0e-9d5b-4a53-8c7e-0d6f7c1a2b3c",
    email: "admin@example.com",
    name: "Ana",
    surname: "García",
    role: "ADMIN",
    isActive: true,
    deactivatedAtUtc: null,
    trainer: null,
  },
  {
    id: "6a2d3f1e-0e6c-4b64-9d8f-1e7a8d2b3c4d",
    email: "luis@example.com",
    name: "Luis",
    surname: "Pérez",
    role: "TRAINER",
    isActive: true,
    deactivatedAtUtc: null,
    trainer: null,
  },
  {
    id: "7b0c1f7e-3a54-4d4e-9d0e-1a2b3c4d5e6f",
    email: "carla@example.com",
    name: "Carla",
    surname: "Pérez",
    role: "USER",
    isActive: true,
    deactivatedAtUtc: null,
    trainer: null,
  },
];

const carla = users[2]!;

const inactive = {
  id: "9d5a6c4b-3b9f-4e97-8a0b-4b0d1a5e6f70",
  email: "sara@example.com",
  name: "Sara",
  surname: "Ruiz",
  role: "TRAINER",
  isActive: false,
  deactivatedAtUtc: "2026-10-20T09:30:00Z",
  trainer: null,
};

const created = {
  id: "8c1d2e3f-4a5b-4c6d-8e7f-9a0b1c2d3e4f",
  email: "marta@example.com",
  name: "Marta",
  surname: "Pérez",
  dateBirth: null,
  role: "TRAINER",
};

test("an admin filters the users and creates a trainer from the modal", async ({ page }) => {
  // Arrange
  let list = users;
  let createBody: unknown = null;
  await mockUsersMe(page, adminProfile);
  await page.route(usersUrl, (route) => {
    const request = route.request();

    if (request.method() === "OPTIONS") {
      return route.fulfill({ status: 204, headers: cors });
    }
    if (request.method() === "POST") {
      createBody = request.postDataJSON();
      list = [...list, { ...created, isActive: true, deactivatedAtUtc: null, trainer: null }];
      return route.fulfill({
        status: 201,
        headers: cors,
        contentType: "application/json",
        body: JSON.stringify(created),
      });
    }
    return route.fulfill({
      status: 200,
      headers: cors,
      contentType: "application/json",
      body: JSON.stringify(list),
    });
  });
  await page.addInitScript(() => {
    window.localStorage.setItem("auth.accessToken", "access");
    window.localStorage.setItem("auth.refreshToken", "refresh");
  });
  await page.goto("/");
  await page.getByRole("link", { name: "Todos los usuarios" }).click();
  await expect(page).toHaveURL(/\/users\/?$/);
  await page.getByRole("searchbox", { name: "Buscar por nombre o email" }).fill("perez");
  await page.getByRole("button", { name: "Entrenador" }).click();
  await expect(page.getByRole("row", { name: /Luis Pérez/ })).toBeVisible();
  await expect(page.getByRole("row", { name: /Carla Pérez/ })).toHaveCount(0);
  await expect(page.getByRole("row", { name: /Ana García/ })).toHaveCount(0);
  await page.getByRole("button", { name: "Añadir usuario" }).click();
  const dialog = page.getByRole("dialog", { name: "Nuevo usuario" });
  await dialog.getByLabel("Email").fill("marta@example.com");
  await dialog.getByLabel("Contraseña").fill("Secreta.123");
  await dialog.getByRole("combobox", { name: "Perfil" }).click();
  await page.getByRole("option", { name: "Entrenador" }).click();
  await dialog.getByLabel("Nombre").fill("Marta");
  await dialog.getByLabel("Apellidos").fill("Pérez");

  // Act
  await dialog.getByRole("button", { name: "Crear usuario" }).click();

  // Assert
  await expect(page.getByText("Usuario creado.")).toBeVisible();
  await expect(dialog).toHaveCount(0);
  await expect(page.getByRole("row", { name: /Marta Pérez/ })).toBeVisible();
  await expect(page.getByRole("row", { name: /Luis Pérez/ })).toBeVisible();
  expect(createBody).toEqual({
    email: "marta@example.com",
    password: "Secreta.123",
    role: 2,
    name: "Marta",
    surname: "Pérez",
  });
});

test("an admin shows the inactive users and deactivates an active one", async ({ page }) => {
  // Arrange
  let list = [...users, inactive];
  const deactivated: string[] = [];
  await mockUsersMe(page, adminProfile);
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
  await page.route(`${usersUrl}/*/deactivate`, (route) => {
    const request = route.request();

    if (request.method() === "OPTIONS") {
      return route.fulfill({ status: 204, headers: cors });
    }
    const id = new URL(request.url()).pathname.split("/")[2]!;
    deactivated.push(id);
    list = list.map((user) =>
      user.id === id
        ? { ...user, isActive: false, deactivatedAtUtc: "2026-10-30T10:00:00Z" }
        : user,
    );
    return route.fulfill({
      status: 200,
      headers: cors,
      contentType: "application/json",
      body: JSON.stringify(list.find((user) => user.id === id)),
    });
  });
  await page.addInitScript(() => {
    window.localStorage.setItem("auth.accessToken", "access");
    window.localStorage.setItem("auth.refreshToken", "refresh");
  });
  await page.goto("/users");
  await expect(page.getByRole("row", { name: /Carla Pérez/ })).toBeVisible();
  await expect(page.getByRole("row", { name: /Sara Ruiz/ })).toHaveCount(0);
  await page.getByRole("switch", { name: "Mostrar inactivos" }).check();
  await expect(page.getByRole("row", { name: /Sara Ruiz/ })).toContainText("Inactivo");
  await page.getByRole("button", { name: "Acciones de Carla Pérez" }).click();
  await page.getByRole("menuitem", { name: "Desactivar" }).click();
  const dialog = page.getByRole("dialog", { name: "¿Desactivar a Carla Pérez?" });

  // Act
  await dialog.getByRole("button", { name: "Desactivar" }).click();

  // Assert
  await expect(page.getByText("Usuario desactivado.")).toBeVisible();
  await expect(dialog).toHaveCount(0);
  await expect(page.getByRole("row", { name: /Carla Pérez/ })).toContainText("Inactivo");
  expect(deactivated).toEqual([carla.id]);
});
