import type { components } from "../../contract/generated/api";

export type Workout = components["schemas"]["WorkoutResponse"];
/** A workout as listed: the workout plus its athlete and, when it has one, its trainer. */
export type WorkoutListItem = components["schemas"]["WorkoutListItemResponse"];
export type WorkoutDetail = components["schemas"]["WorkoutDetailResponse"];
export type WorkoutExerciseDetail = components["schemas"]["WorkoutExerciseDetailResponse"];
export type CreateWorkoutRequest = components["schemas"]["CreateWorkoutRequest"];
export type UpdateWorkoutRequest = components["schemas"]["UpdateWorkoutRequest"];
export type AddExerciseRequest = components["schemas"]["AddOrUpdateExerciseRequest"];
export type SetPrescription = components["schemas"]["SetPrescriptionRequest"];
export type ReorderWorkoutExercisesRequest =
  components["schemas"]["ReorderWorkoutExercisesRequest"];

/** The workout id travels in the path, the rest of the input in the body. */
export type UpdateWorkoutInput = UpdateWorkoutRequest & { id: string };
export type AddExerciseToWorkoutInput = AddExerciseRequest & { workoutId: string };
export type GetWorkoutByIdInput = { id: string };
/** The list's query: `standalone` keeps only the workouts not placed in a mesocycle. */
export type GetWorkoutsInput = { standalone?: boolean };
/** Both ids travel in the path; `exerciseId` is the catalog exercise, not the workout entry. */
export type RemoveExerciseFromWorkoutInput = { workoutId: string; exerciseId: string };
/** The workout id travels in the path; the catalog exercise id goes in the path and, repeated, in the body. */
export type UpdateWorkoutExerciseInput = AddExerciseRequest & {
  workoutId: string;
  exerciseId: string;
};
/** The workout id travels in the path; the body holds every catalog exercise id of the workout, in the new order. */
export type ReorderWorkoutExercisesInput = ReorderWorkoutExercisesRequest & { workoutId: string };
