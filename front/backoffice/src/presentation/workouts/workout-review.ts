import type {
  ExerciseHistorySession,
  ExerciseSummary,
  SetRecord,
  WorkoutById,
  WorkoutByIdExercise,
  WorkoutSummary,
} from "@weight-tracker/core/domain";

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

/**
 * How long the session lasted: since it started while it is in progress, between its start and its completion once
 * it is over. `null` when a needed date is missing, when the dates are out of order or for any other status.
 */
export function elapsedMsOf(
  workout: Pick<WorkoutById, "status" | "startedAt" | "completedAt">,
  now: number,
): number | null {
  const startedAt = workout.startedAt ? Date.parse(workout.startedAt) : Number.NaN;
  let end = Number.NaN;

  if (workout.status === "STARTED") {
    end = now;
  } else if (workout.status === "COMPLETED" || workout.status === "REVIEWED") {
    end = workout.completedAt ? Date.parse(workout.completedAt) : Number.NaN;
  }

  const elapsed = end - startedAt;

  return Number.isNaN(elapsed) || elapsed < 0 ? null : elapsed;
}

/** A duration in whole hours and minutes; the seconds are truncated. */
export function elapsedPartsOf(ms: number): { hours: number; minutes: number } {
  const totalMinutes = Math.floor(ms / 60_000);

  return { hours: Math.floor(totalMinutes / 60), minutes: totalMinutes % 60 };
}

/** An exercise of the summary is weighted when the API answered a volume for it. */
export function isWeighted(exercise: ExerciseSummary): boolean {
  return exercise.volume != null;
}

/** A weighted exercise of the summary, with its entry in the workout and its 1-based position there. */
export interface WeightedExercise {
  summary: ExerciseSummary;
  exercise: WorkoutByIdExercise;
  position: number;
}

/** The weighted exercises of the summary, in workout order, each paired with its workout entry. */
export function weightedExercisesOf(
  summary: WorkoutSummary,
  workout: Pick<WorkoutById, "exercises">,
): WeightedExercise[] {
  const weighted: WeightedExercise[] = [];

  workout.exercises.forEach((exercise, index) => {
    const entry = summary.exercises.find(
      ({ workoutExerciseId }) => workoutExerciseId === exercise.id,
    );

    if (entry && isWeighted(entry)) {
      weighted.push({ summary: entry, exercise, position: index + 1 });
    }
  });
  return weighted;
}

export interface WeightChange {
  direction: "up" | "down" | "equal";
  /** The signed change from the previous weight to the current one, in percent and not rounded. */
  percent: number;
}

/** How the weight moved between two top sets, or `null` when either is missing or the previous one is 0. */
export function weightChangeOf(
  previous: number | string | null | undefined,
  current: number | string | null | undefined,
): WeightChange | null {
  if (previous == null || current == null || Number(previous) === 0) {
    return null;
  }
  const before = Number(previous);
  const after = Number(current);

  if (after === before) {
    return { direction: "equal", percent: 0 };
  }
  return { direction: after > before ? "up" : "down", percent: ((after - before) / before) * 100 };
}

/** A part over a total, rounded, or `null` when there is no total. */
export function percentOf(part: number | string, total: number | string): number | null {
  return Number(total) === 0 ? null : Math.round((Number(part) / Number(total)) * 100);
}

export interface MuscleSets {
  muscle: string;
  sets: number;
}

/** The counted records per muscle, for the muscles that were trained only, most sets first. */
export function muscleDistributionOf(workout: Pick<WorkoutById, "exercises">): MuscleSets[] {
  const setsByMuscle = new Map<string, number>();

  for (const exercise of workout.exercises) {
    const sets = countedRecordsOf(exercise).length;

    if (sets > 0) {
      setsByMuscle.set(exercise.muscle, (setsByMuscle.get(exercise.muscle) ?? 0) + sets);
    }
  }
  return [...setsByMuscle]
    .map(([muscle, sets]) => ({ muscle, sets }))
    .sort((a, b) => b.sets - a.sets || a.muscle.localeCompare(b.muscle));
}

export interface HistoryPoint {
  workoutDate: string;
  value: number;
}

/** The sessions oldest first, as the chart reads them, without the ones that have no value for the field. */
export function historyPointsOf(
  sessions: readonly ExerciseHistorySession[],
  field: "maxWeight" | "volume",
): HistoryPoint[] {
  return [...sessions]
    .reverse()
    .filter((session) => session[field] != null)
    .map((session) => ({ workoutDate: session.workoutDate, value: Number(session[field]) }));
}
