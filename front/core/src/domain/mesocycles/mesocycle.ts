import type { components } from "../../contract/generated/api";

/** A mesocycle with its defined weeks and the public profiles of its trainer and athlete. */
export type Mesocycle = components["schemas"]["MesocycleResponse"];
export type MesocycleWeek = components["schemas"]["MesocycleWeekResponse"];
export type TrainingWeekType = components["schemas"]["TrainingWeekType"];
export type CreateMesocycleRequest = components["schemas"]["CreateMesocycleRequest"];
export type UpdateMesocycleRequest = components["schemas"]["UpdateMesocycleRequest"];
export type DefineMesocycleWeekRequest = components["schemas"]["DefineMesocycleWeekRequest"];
export type RedefineMesocycleWeekRequest = components["schemas"]["RedefineMesocycleWeekRequest"];

export type GetMesocycleByIdInput = { id: string };
/** The mesocycle id travels in the path. */
export type GetMesocycleWorkoutsInput = { mesocycleId: string };
/** The athlete id travels in the path. */
export type GetAthleteMesocyclesInput = { userId: string };
/** The mesocycle id travels in the path, the rest of the input in the body. */
export type UpdateMesocycleInput = UpdateMesocycleRequest & { id: string };
/** The mesocycle id travels in the path; the week number goes in the body. */
export type DefineMesocycleWeekInput = DefineMesocycleWeekRequest & { mesocycleId: string };
/** The mesocycle id and the week number travel in the path, the rest of the input in the body. */
export type RedefineMesocycleWeekInput = RedefineMesocycleWeekRequest & {
  mesocycleId: string;
  weekNumber: number;
};
