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

/** A workout placed in a mesocycle week, as the workout endpoints answer with it. */
export interface StoredWorkout {
  id: string;
  name: string;
  description: string | null;
  workoutDate: string;
  userId: string;
  trainerId: string | null;
  status: string;
  exercises: unknown[];
  mesocycleId: string | null;
  mesocycleWeekNumber: number | null;
  athlete: Person;
  trainer: Person | null;
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
 * given `newId` and the `people` it names. `workouts` is the list `GET /mesocycles/{id}/workouts` reads, shared with
 * `mockWeekWorkoutsApi`. Every write is recorded, in order.
 */
export async function mockMesocyclesApi(
  page: Page,
  {
    initial,
    people,
    newId = "9d2e3f4a-5b6c-4d7e-8f9a-0b1c2d3e4f5a",
    trainer,
    workouts = [],
  }: {
    initial: StoredMesocycle[];
    people: Person[];
    newId?: string;
    trainer: Person;
    workouts?: StoredWorkout[];
  },
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
    if (weeks === "workouts") {
      return route.fulfill(json(workouts.filter((workout) => workout.mesocycleId === id)));
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

/**
 * Intercepts what creating and opening a week workout needs: `POST /workouts` stores the workout in `workouts` with
 * `newId`, the `people` it names and the date of its week; `GET /workouts/{id}` answers it, with its mesocycle from
 * `mesocycles` embedded as the API does; the catalog and the favorites are empty. Every creation body is recorded, in order.
 */
export async function mockWeekWorkoutsApi(
  page: Page,
  {
    workouts,
    people,
    newId = "8c1d2e3f-4a5b-4c6d-8e7f-9a0b1c2d3e4f",
    workoutDate,
    mesocycles = [],
  }: {
    workouts: StoredWorkout[];
    people: Person[];
    newId?: string;
    workoutDate: string;
    mesocycles?: StoredMesocycle[];
  },
): Promise<SentRequest[]> {
  const sent: SentRequest[] = [];
  const personOf = (id: string) => people.find((person) => person.id === id)!;

  await page.route(`${api}/workouts`, (route) => {
    const request = route.request();
    if (request.method() === "OPTIONS") {
      return route.fulfill({ status: 204, headers: cors });
    }
    const body = request.postDataJSON() as {
      name: string;
      description?: string;
      userId: string;
      trainerId?: string;
      mesocycleId: string;
      mesocycleWeekNumber: number;
    };
    sent.push({ method: "POST", path: "/workouts", body });
    const trainerId = body.trainerId ?? people.find((person) => person.role === "TRAINER")!.id;
    const created: StoredWorkout = {
      id: newId,
      name: body.name,
      description: body.description ?? null,
      // The API dates a placed workout from its week.
      workoutDate,
      userId: body.userId,
      trainerId,
      status: "DRAFT",
      exercises: [],
      mesocycleId: body.mesocycleId,
      mesocycleWeekNumber: body.mesocycleWeekNumber,
      athlete: personOf(body.userId),
      trainer: personOf(trainerId),
    };
    workouts.push(created);
    return route.fulfill(json(created, 201));
  });
  await page.route(`${api}/workouts/*`, (route) => {
    if (route.request().method() === "OPTIONS") {
      return route.fulfill({ status: 204, headers: cors });
    }
    const id = new URL(route.request().url()).pathname.split("/").pop() ?? "";
    const workout = workouts.find((item) => item.id === id);
    if (!workout) {
      return route.fulfill(json({ status: 404 }, 404));
    }
    // The API embeds the mesocycle, without its people: the workout already carries them.
    const placedIn = mesocycles.find((mesocycle) => mesocycle.id === workout.mesocycleId);
    const mesocycle = placedIn
      ? {
          id: placedIn.id,
          name: placedIn.name,
          objective: placedIn.objective,
          startDate: placedIn.startDate,
          durationInWeeks: placedIn.durationInWeeks,
          weeks: placedIn.weeks,
        }
      : null;
    return route.fulfill(json({ ...workout, mesocycle }));
  });
  for (const path of ["exercises", "exercise-favorites"]) {
    await page.route(`${api}/${path}`, (route) =>
      route.request().method() === "OPTIONS"
        ? route.fulfill({ status: 204, headers: cors })
        : route.fulfill(json([])),
    );
  }

  return sent;
}
