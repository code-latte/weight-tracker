import type { SetRecord, WorkoutById, WorkoutByIdExercise } from "@weight-tracker/core/domain";

/** The workout statuses the backoffice shows as a review, once the athlete has started the workout. */
const REVIEW_STATUSES = new Set(["STARTED", "COMPLETED", "REVIEWED"]);

export type ExerciseState = "completed" | "inProgress" | "pending";

type SetTarget = WorkoutByIdExercise["sets"][number]["target"];

/** One planned set paired with what the athlete recorded for it, or `null` while it is pending. */
export interface PlannedRow {
  /** 1-based. */
  number: number;
  target: SetTarget;
  record: SetRecord | null;
}

export interface ReviewSummary {
  planned: number;
  counted: number;
  pending: number;
  /** Counted over planned, rounded; 0 when nothing is planned. */
  percent: number;
  completedExercises: number;
  totalExercises: number;
  /** The sum of reps × weight over the counted records that have both. */
  volumeKg: number;
}

/** Whether a workout with this status opens the review instead of the editor. */
export function isReviewStatus(status: string): boolean {
  return REVIEW_STATUSES.has(status);
}

/** The sets the exercise prescribes: the sum of its set groups' counts. */
export function plannedSetsOf(exercise: WorkoutByIdExercise): number {
  return exercise.sets.reduce((total, set) => total + Number(set.count), 0);
}

/** The records that count: the first ones, up to the planned sets. Any further record is discarded. */
export function countedRecordsOf(exercise: WorkoutByIdExercise): SetRecord[] {
  return exercise.setRecords.slice(0, plannedSetsOf(exercise));
}

export function exerciseStateOf(exercise: WorkoutByIdExercise): ExerciseState {
  const planned = plannedSetsOf(exercise);
  const counted = countedRecordsOf(exercise).length;

  if (planned > 0 && counted === planned) {
    return "completed";
  }
  return counted > 0 ? "inProgress" : "pending";
}

export function reviewSummaryOf(workout: WorkoutById): ReviewSummary {
  let planned = 0;
  let counted = 0;
  let completedExercises = 0;
  let volumeKg = 0;

  for (const exercise of workout.exercises) {
    const records = countedRecordsOf(exercise);

    planned += plannedSetsOf(exercise);
    counted += records.length;
    if (exerciseStateOf(exercise) === "completed") {
      completedExercises += 1;
    }
    for (const { reps, weight } of records) {
      if (reps != null && weight != null) {
        volumeKg += Number(reps) * Number(weight);
      }
    }
  }

  return {
    planned,
    counted,
    pending: Math.max(planned - counted, 0),
    percent: planned === 0 ? 0 : Math.round((counted / planned) * 100),
    completedExercises,
    totalExercises: workout.exercises.length,
    volumeKg,
  };
}

/** One row per planned set, the set groups expanded by their count and paired in order with the counted records. */
export function plannedRowsOf(exercise: WorkoutByIdExercise): PlannedRow[] {
  const records = countedRecordsOf(exercise);
  const rows: PlannedRow[] = [];

  for (const { count, target } of exercise.sets) {
    for (let index = 0; index < Number(count); index += 1) {
      rows.push({ number: rows.length + 1, target, record: records[rows.length] ?? null });
    }
  }
  return rows;
}

/** The id of the exercise whose card starts expanded: the first in progress, else the first not completed. */
export function initiallyExpandedOf(exercises: readonly WorkoutByIdExercise[]): string | null {
  const open =
    exercises.find((exercise) => exerciseStateOf(exercise) === "inProgress") ??
    exercises.find((exercise) => exerciseStateOf(exercise) !== "completed");

  return open?.id ?? null;
}

/** A rest in minutes as `m:ss`, or `null` when the exercise has none. */
export function restOf(restMinutes: WorkoutByIdExercise["restMinutes"]): string | null {
  if (restMinutes == null) {
    return null;
  }
  const seconds = Math.round(Number(restMinutes) * 60);

  return `${Math.floor(seconds / 60)}:${String(seconds % 60).padStart(2, "0")}`;
}
