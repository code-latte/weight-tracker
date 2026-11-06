import type { components } from "../../contract/generated/api";
import type {
  AddExerciseRequest,
  ReorderWorkoutExercisesRequest,
  SuperSeriesId,
} from "../workouts/workout";

/** A template with its exercises in order; it holds only its trainer's id, no profile. */
export type WorkoutTemplate = components["schemas"]["WorkoutTemplateResponse"];
export type CreateWorkoutTemplateRequest = components["schemas"]["CreateWorkoutTemplateRequest"];
export type UpdateWorkoutTemplateRequest = components["schemas"]["UpdateWorkoutTemplateRequest"];
export type CreateWorkoutFromTemplateRequest =
  components["schemas"]["CreateWorkoutFromTemplateRequest"];

/** The list's query: `trainerId` keeps only that trainer's templates, and only an admin's is honoured. */
export type GetWorkoutTemplatesInput = { trainerId?: string };
export type GetWorkoutTemplateByIdInput = { id: string };
/** The template id travels in the path, the rest of the input in the body. */
export type UpdateWorkoutTemplateInput = UpdateWorkoutTemplateRequest & { id: string };
export type DeleteWorkoutTemplateInput = { id: string };
export type AddExerciseToWorkoutTemplateInput = AddExerciseRequest & { templateId: string };
/** The template id travels in the path; the catalog exercise id goes in the path and, repeated, in the body. */
export type UpdateWorkoutTemplateExerciseInput = AddExerciseRequest & {
  templateId: string;
  exerciseId: string;
};
/** Both ids travel in the path; `exerciseId` is the catalog exercise, not the template entry. */
export type RemoveExerciseFromWorkoutTemplateInput = { templateId: string; exerciseId: string };
/** The template id travels in the path; the body holds every catalog exercise id of the template, in the new order. */
export type ReorderWorkoutTemplateExercisesInput = ReorderWorkoutExercisesRequest & {
  templateId: string;
};
/** The template id travels in the path; `exerciseIds` are template entry ids, not catalog exercise ids. */
export type CreateWorkoutTemplateSuperSeriesInput = { templateId: string; exerciseIds: string[] };
/** Both ids travel in the path. */
export type BreakWorkoutTemplateSuperSeriesInput = {
  templateId: string;
  superSeriesId: SuperSeriesId;
};
/** The template id travels in the path, the athlete and the placement in the body. */
export type CreateWorkoutFromTemplateInput = CreateWorkoutFromTemplateRequest & {
  templateId: string;
};
/** The workout id travels in the path; there is no body. */
export type CreateWorkoutTemplateFromWorkoutInput = { workoutId: string };
