import type { components } from "../../contract/generated/api";

export type Workout = components["schemas"]["WorkoutResponse"];
/** A workout as listed: the workout plus its athlete and, when it has one, its trainer. */
export type WorkoutListItem = components["schemas"]["WorkoutListItemResponse"];
export type WorkoutDetail = components["schemas"]["WorkoutDetailResponse"];
/**
 * A workout as read by id: the detail plus its athlete, its trainer when it has one, the catalog data of each entry's
 * exercise and the mesocycle it is placed in, so it can be shown without further reads.
 */
export type WorkoutById = components["schemas"]["WorkoutByIdResponse"];
/** An entry of a workout read by id: the entry plus its exercise's names, video, muscle, body part and type. */
export type WorkoutByIdExercise = components["schemas"]["WorkoutByIdExerciseResponse"];
/** The mesocycle a workout read by id is placed in, with its weeks. */
export type WorkoutMesocycle = components["schemas"]["WorkoutMesocycleResponse"];
export type WorkoutExerciseDetail = components["schemas"]["WorkoutExerciseDetailResponse"];
export type CreateWorkoutRequest = components["schemas"]["CreateWorkoutRequest"];
export type UpdateWorkoutRequest = components["schemas"]["UpdateWorkoutRequest"];
export type AddExerciseRequest = components["schemas"]["AddOrUpdateExerciseRequest"];
export type SetPrescription = components["schemas"]["SetPrescriptionRequest"];
export type ReorderWorkoutExercisesRequest =
  components["schemas"]["ReorderWorkoutExercisesRequest"];
export type CreateSuperSeriesRequest = components["schemas"]["CreateSuperSeriesRequest"];
export type SuperSeries = components["schemas"]["SuperSeriesResponse"];
/** The id that groups the members of a superseries, unique within its workout. */
export type SuperSeriesId = NonNullable<WorkoutExerciseDetail["superSeriesId"]>;
/** A set actually performed by the athlete, as opposed to the prescribed sets. */
export type SetRecord = components["schemas"]["SetRecordResponse"];
export type AddOrUpdateSetRecordRequest = components["schemas"]["AddOrUpdateSetRecordRequest"];
export type ReorderSetRecordsRequest = components["schemas"]["ReorderSetRecordsRequest"];
export type ReviewWorkoutRequest = components["schemas"]["ReviewWorkoutRequest"];
export type ChangeWorkoutStatusRequest = components["schemas"]["ChangeWorkoutStatusRequest"];
export type WorkoutStatus = components["schemas"]["WorkoutStatus"];
/**
 * The stats the API computes for one workout: its sets in target and volume, and per exercise the top set and the top
 * set of its previous execution.
 */
export type WorkoutSummary = components["schemas"]["WorkoutSummaryResponse"];
/** The stats of one exercise of a {@link WorkoutSummary}; `volume`, `topSet` and `previous` are `null` when not applicable. */
export type ExerciseSummary = components["schemas"]["ExerciseSummaryResponse"];
/** How many of the evaluated sets met their target. */
export type SetsInTarget = components["schemas"]["SetsInTargetResponse"];
/** The previous execution of an exercise: its workout, its date and its top set. */
export type PreviousExecution = components["schemas"]["PreviousExecutionResponse"];
/**
 * What the API generated when a prescribed workout was completed: every observation, and which of them are highlighted.
 * Both lists are empty when none were generated.
 */
export type WorkoutObservations = components["schemas"]["WorkoutObservationsResponse"];
/** One measurable fact about a workout or one of its exercises; `data` holds the numbers it was calculated from. */
export type WorkoutObservation = components["schemas"]["WorkoutObservationResponse"];
/** An observation the API selected for the trainer's attention, with its position among the highlights. */
export type WorkoutHighlight = components["schemas"]["WorkoutHighlightResponse"];

/** The workout id travels in the path, the rest of the input in the body. */
export type UpdateWorkoutInput = UpdateWorkoutRequest & { id: string };
export type AddExerciseToWorkoutInput = AddExerciseRequest & { workoutId: string };
export type GetWorkoutByIdInput = { id: string };
export type GetWorkoutSummaryInput = { workoutId: string };
export type GetWorkoutObservationsInput = { workoutId: string };
export type DeleteWorkoutInput = { id: string };
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
/** The workout id travels in the path; `exerciseIds` are workout entry ids, not catalog exercise ids. */
export type CreateSuperSeriesInput = { workoutId: string; exerciseIds: string[] };
/** Both ids travel in the path. */
export type BreakSuperSeriesInput = { workoutId: string; superSeriesId: SuperSeriesId };
/** Both ids travel in the path; the body holds `reps` or `durationSeconds` and an optional `weight`. `exerciseId` is the catalog exercise. */
export type AddSetRecordInput = AddOrUpdateSetRecordRequest & {
  workoutId: string;
  exerciseId: string;
};
/** The three ids travel in the path; the body holds `reps` or `durationSeconds` and an optional `weight`. `exerciseId` is the catalog exercise. */
export type UpdateSetRecordInput = AddOrUpdateSetRecordRequest & {
  workoutId: string;
  exerciseId: string;
  setRecordId: string;
};
/** The three ids travel in the path; `exerciseId` is the catalog exercise. */
export type RemoveSetRecordInput = { workoutId: string; exerciseId: string; setRecordId: string };
/** Both ids travel in the path; the body holds every set record id of the exercise, in the new order. */
export type ReorderSetRecordsInput = ReorderSetRecordsRequest & {
  workoutId: string;
  exerciseId: string;
};
/** The workout id travels in the path; `resultsUpdatedAt` is the value last read, sent unchanged. */
export type ReviewWorkoutInput = ReviewWorkoutRequest & { workoutId: string };
/** The workout id travels in the path, the target status in the body. */
export type ChangeWorkoutStatusInput = ChangeWorkoutStatusRequest & { workoutId: string };
