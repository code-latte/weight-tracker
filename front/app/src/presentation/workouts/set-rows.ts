import type {
  AddOrUpdateSetRecordRequest,
  SetRecord,
  WorkoutByIdExercise,
} from "@weight-tracker/core/domain";

type SetTarget = WorkoutByIdExercise["sets"][number]["target"];

/** What the athlete reports for a set: nothing but the weight (reps are prescribed), reps and weight, or nothing. */
export type SetKind = "reps" | "range" | "timed";

/** One prescribed set, with the record that is already saved for it, if any. */
export interface SetRow {
  /** From 1, across every prescription of the exercise. */
  number: number;
  kind: SetKind;
  target: SetTarget;
  record: SetRecord | null;
}

/** A translation key under `workouts` and its values. */
export interface TargetLabel {
  key: string;
  values: Record<string, number>;
}

export function setKindOf(target: SetTarget): SetKind {
  switch (target.type) {
    case "Reps":
      return "reps";
    case "Duration":
    case "Minutes":
      return "timed";
    default:
      // RepsRange, MaxReps and any target the app does not know ask for the reps done.
      return "range";
  }
}

/**
 * The prescribed sets of an exercise, one per set: each prescription is `count × target`. A row takes the record at
 * its own position, the same rule the API uses to pick the next prescribed set. Records past the last row are not shown.
 */
export function expandSets(exercise: WorkoutByIdExercise): SetRow[] {
  const targets = exercise.sets.flatMap((set) =>
    Array.from({ length: Number(set.count) }, () => set.target),
  );

  return targets.map((target, index) => ({
    number: index + 1,
    kind: setKindOf(target),
    target,
    record: exercise.setRecords[index] ?? null,
  }));
}

/** How a set's target reads: the key and the values it interpolates. */
export function setTargetLabel(target: SetTarget): TargetLabel {
  switch (target.type) {
    case "Reps":
      return { key: "exercise.target.reps", values: { count: Number(target.value) } };
    case "RepsRange":
      return {
        key: "exercise.target.range",
        values: { min: Number(target.value), max: Number(target.maxValue) },
      };
    case "Duration":
      return { key: "exercise.target.seconds", values: { count: Number(target.value) } };
    case "Minutes":
      return { key: "exercise.target.minutes", values: { count: Number(target.minutes) } };
    default:
      return { key: "exercise.target.max", values: {} };
  }
}

/** What the athlete typed for a row, already read as numbers; `null` for an input left empty. */
export interface SetValues {
  reps: number | null;
  weight: number | null;
}

/**
 * The body that records or corrects the set: `reps` for a rep-based target (the prescribed value when the athlete does
 * not report them), `durationSeconds` for a timed one, and `weight` only when typed. Always explicit, so a correction
 * and a first record send the same thing.
 */
export function setRecordBodyOf(row: SetRow, values: SetValues): AddOrUpdateSetRecordRequest {
  const weight = values.weight === null ? {} : { weight: values.weight };

  switch (row.kind) {
    case "reps":
      return { reps: Number(row.target.value), ...weight };
    case "range":
      return { reps: values.reps, ...weight };
    default:
      return {
        durationSeconds:
          row.target.type === "Minutes"
            ? Math.round(Number(row.target.minutes) * 60)
            : Number(row.target.value),
        ...weight,
      };
  }
}

/** The exercise's name in the language, else its first translation, else `null`. */
export function nameOf(exercise: WorkoutByIdExercise, language: string): string | null {
  const translation =
    exercise.translations.find((candidate) => candidate.languageCode === language) ??
    exercise.translations[0];

  return translation?.name ?? null;
}

/** The English name as a subtitle: only when the language is not English and it adds something to the shown name. */
export function englishSubtitleOf(
  exercise: WorkoutByIdExercise,
  language: string,
  shownName: string | null,
): string | null {
  if (language === "en") {
    return null;
  }
  const english = exercise.translations.find((candidate) => candidate.languageCode === "en")?.name;

  return english && english !== shownName ? english : null;
}

/** Whether the API takes set records for a workout in this status; it answers `409` otherwise. */
export function isRecordable(status: string | null | undefined): boolean {
  return status === "STARTED" || status === "COMPLETED";
}

/** The rest in whole seconds, rounded like `minutesToText` does, so `1.5` is `90`. */
export function restSecondsOf(minutes: number | string): number {
  return Math.round(Number(minutes) * 60);
}

/** Seconds as `M:SS`, so `90` reads `1:30` and `0` reads `0:00`. */
export function countdownTextOf(seconds: number): string {
  return `${Math.floor(seconds / 60)}:${String(seconds % 60).padStart(2, "0")}`;
}

/** The rest as `M:SS`, always with the seconds, so `1.5` reads `1:30` and `2` reads `2:00`. */
export function restTimeOf(minutes: number | string): string {
  return countdownTextOf(restSecondsOf(minutes));
}
