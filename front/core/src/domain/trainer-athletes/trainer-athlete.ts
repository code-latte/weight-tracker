import type { components } from "../../contract/generated/api";

/** One of a trainer's current athletes: the relationship id and start, the athlete's profile and the trainer's name. */
export type TrainerAthlete = components["schemas"]["TrainerAthleteListItemResponse"];
/** A relationship with the names of its trainer and athlete, and its status. */
export type TrainerAthleteRelationship = components["schemas"]["TrainerAthleteResponse"];
export type TrainerAthleteRequest = components["schemas"]["TrainerAthleteRequest"];

/** The trainer id travels in the path; the athlete id in the body. */
export type TrainerAthleteInput = TrainerAthleteRequest & { trainerId: string };
