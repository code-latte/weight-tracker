import type { Page } from "@playwright/test";

const usersMeUrl = "http://localhost:5199/users/me";
const exercisesUrl = "http://localhost:5199/exercises";
const cors = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, content-type",
  "Access-Control-Allow-Methods": "GET, PUT, OPTIONS",
};

export interface Profile {
  id: string;
  email: string;
  name: string;
  surname: string;
  role: string;
}

export const completeProfile: Profile = {
  id: "5f1c2f0e-9d5b-4a53-8c7e-0d6f7c1a2b3c",
  email: "trainer@example.com",
  name: "Ana",
  surname: "García",
  role: "TRAINER",
};

/**
 * Intercepts `GET` and `PUT /users/me`, including the preflight. A `PUT` stores what it receives. Every signed-in page
 * also reads the exercise catalog in the background, so `GET /exercises` answers an empty catalog here; a spec that
 * routes `/exercises` after this call answers it instead, since Playwright uses the route registered last.
 */
export async function mockUsersMe(page: Page, initial: Profile = completeProfile) {
  let profile = initial;

  await page.route(exercisesUrl, (route) =>
    route.request().method() === "OPTIONS"
      ? route.fulfill({ status: 204, headers: cors })
      : route.fulfill({ status: 200, headers: cors, contentType: "application/json", body: "[]" }),
  );

  await page.route(usersMeUrl, (route) => {
    const request = route.request();

    if (request.method() === "OPTIONS") {
      return route.fulfill({ status: 204, headers: cors });
    }
    if (request.method() === "PUT") {
      profile = { ...profile, ...(request.postDataJSON() as Partial<Profile>) };
    }
    return route.fulfill({
      status: 200,
      headers: cors,
      contentType: "application/json",
      body: JSON.stringify(profile),
    });
  });
}
