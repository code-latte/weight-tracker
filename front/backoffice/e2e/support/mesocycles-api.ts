import type { Page } from "@playwright/test";

const api = "http://localhost:5199";
const cors = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, content-type",
  "Access-Control-Allow-Methods": "GET, POST, PUT, OPTIONS",
};

export interface Person {
  id: string;
  email: string;
  name: string;
  surname: string;
  role: string;
}

export interface Week {
  weekNumber: number;
  trainingWeekType: string;
  objective: string;
}

export interface StoredMesocycle {
  id: string;
  trainerId: string;
  athleteId: string;
  name: string;
  objective: string;
  startDate: string;
  durationInWeeks: number;
  weeks: Week[];
  trainer: Person;
  athlete: Person;
}

export interface SentRequest {
  method: string;
  path: string;
  body: unknown;
}

const json = (body: unknown, status = 200) => ({
  status,
  headers: cors,
  contentType: "application/json",
  body: JSON.stringify(body),
});

/**
 * Intercepts the mesocycle endpoints over an in-memory list, so the screens see their own writes. A creation is
 * given `newId` and the `people` it names. Every write is recorded, in order.
 */
export async function mockMesocyclesApi(
  page: Page,
  {
    initial,
    people,
    newId = "9d2e3f4a-5b6c-4d7e-8f9a-0b1c2d3e4f5a",
    trainer,
  }: { initial: StoredMesocycle[]; people: Person[]; newId?: string; trainer: Person },
): Promise<SentRequest[]> {
  const mesocycles = [...initial];
  const sent: SentRequest[] = [];
  const personOf = (id: string) => people.find((person) => person.id === id)!;

  await page.route(new RegExp(`^${api}/mesocycles(/.*)?$`), (route) => {
    const request = route.request();
    const path = new URL(request.url()).pathname;
    const method = request.method();

    if (method === "OPTIONS") {
      return route.fulfill({ status: 204, headers: cors });
    }
    if (method !== "GET") {
      sent.push({ method, path, body: request.postDataJSON() });
    }

    const [, , id, weeks, weekNumber] = path.split("/");
    if (id === undefined) {
      if (method === "POST") {
        const body = request.postDataJSON() as Omit<
          StoredMesocycle,
          "id" | "weeks" | "trainer" | "athlete"
        > & {
          trainerId?: string;
        };
        const created: StoredMesocycle = {
          ...body,
          id: newId,
          trainerId: body.trainerId ?? trainer.id,
          weeks: [],
          trainer: body.trainerId ? personOf(body.trainerId) : trainer,
          athlete: personOf(body.athleteId),
        };
        mesocycles.push(created);
        return route.fulfill(json(created, 201));
      }
      return route.fulfill(json(mesocycles));
    }

    const mesocycle = mesocycles.find((item) => item.id === id);
    if (!mesocycle) {
      return route.fulfill(json({}, 404));
    }
    if (weeks === undefined) {
      if (method === "PUT") {
        Object.assign(mesocycle, request.postDataJSON());
      }
      return route.fulfill(json(mesocycle));
    }
    if (weekNumber === undefined) {
      mesocycle.weeks.push(request.postDataJSON() as Week);
      return route.fulfill(json(mesocycle, 201));
    }
    const week = mesocycle.weeks.find((item) => item.weekNumber === Number(weekNumber))!;
    Object.assign(week, request.postDataJSON());
    return route.fulfill(json(mesocycle));
  });

  return sent;
}
