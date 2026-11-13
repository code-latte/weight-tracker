import type {
  WorkoutById,
  WorkoutByIdExercise,
  WorkoutObservation,
  WorkoutObservations,
} from "@weight-tracker/core/domain";

/** The observations of one subject: the whole workout (`exerciseId` null) or one catalog exercise. */
export interface ObservationGroup {
  exerciseId: string | null;
  observations: WorkoutObservation[];
}

/** Locale formatters the caller builds once, so these helpers stay free of React and of translations. */
export interface ObservationFormat {
  /** A weight with its unit: "130 kg". */
  kg: (value: number) => string;
  /** A change with one decimal and a sign: "+8,3 %". */
  signedPercent: (value: number) => string;
  /** A plain percentage: "60 %". */
  percent: (value: number) => string;
  integer: (value: number) => string;
}

/** One affected set of a rep observation: its 1-based position, the reps done and the target it missed or exceeded. */
export interface SetDetail {
  position: number;
  reps: number;
  target: number;
}

/**
 * What to show for one observation: the translation keys under `review.metrics.observations` and the values to fill
 * in. `text` is `null` when the code is unknown or a value it needs is missing, and the observation then shows its
 * title alone.
 */
export interface ObservationCopy {
  title: string;
  text: string | null;
  /** Interpolation values, already formatted, for the text. */
  values: Record<string, string>;
  /** The affected sets of a rep observation, for the caller to join with its own copy. */
  details: SetDetail[];
  /** The optional earlier reference, appended as a trailing sentence. */
  baseline: { key: string; value: string } | null;
}

/** The highlights in rank order, each as the observation it points at. A highlight with no observation is skipped. */
export function highlightedObservationsOf(response: WorkoutObservations): WorkoutObservation[] {
  const byId = new Map(response.observations.map((observation) => [observation.id, observation]));

  return [...response.highlights]
    .sort((left, right) => Number(left.rank) - Number(right.rank))
    .flatMap((highlight) => {
      const observation = byId.get(highlight.observationId);

      return observation === undefined ? [] : [observation];
    });
}

/** The ids of the highlighted observations. */
export function highlightedIdsOf(response: WorkoutObservations): Set<string> {
  return new Set(response.highlights.map((highlight) => highlight.observationId));
}

/**
 * Groups the observations by subject, keeping the API's order inside each group. The workout's own group comes first,
 * then the exercises in order of first appearance.
 */
export function observationGroupsOf(
  observations: readonly WorkoutObservation[],
): ObservationGroup[] {
  const groups = new Map<string | null, WorkoutObservation[]>();

  for (const observation of observations) {
    const group = groups.get(observation.exerciseId);

    if (group === undefined) {
      groups.set(observation.exerciseId, [observation]);
    } else {
      group.push(observation);
    }
  }

  const workoutLevel = groups.get(null);
  const exerciseGroups = [...groups.entries()]
    .filter(([exerciseId]) => exerciseId !== null)
    .map(([exerciseId, grouped]) => ({ exerciseId, observations: grouped }));

  return workoutLevel === undefined
    ? exerciseGroups
    : [{ exerciseId: null, observations: workoutLevel }, ...exerciseGroups];
}

/** The first exercise of the workout with this catalog id, or `null` when none matches. */
export function exerciseOf(
  workout: Pick<WorkoutById, "exercises">,
  exerciseId: string | null,
): WorkoutByIdExercise | null {
  if (exerciseId === null) {
    return null;
  }

  return workout.exercises.find((exercise) => exercise.exerciseId === exerciseId) ?? null;
}

type Data = Record<string, unknown>;

/** The code's numbers, which the contract leaves untyped: a missing or non-numeric one is `undefined`. */
function numberOf(data: Data, name: string): number | undefined {
  const value = data[name];

  return typeof value === "number" && Number.isFinite(value) ? value : undefined;
}

function numbersOf(data: Data, name: string): number[] | undefined {
  const value = data[name];

  return Array.isArray(value) && value.every((item) => typeof item === "number")
    ? (value as number[])
    : undefined;
}

type Formatter = (value: number) => string;

/** How each data name of a code is formatted, and which of them the text cannot be built without. */
interface CodeShape {
  required: Record<string, Formatter>;
  baseline?: { name: string; key: string; format: Formatter };
}

function shapeOf(code: string, format: ObservationFormat): CodeShape | null {
  const { kg, signedPercent, percent, integer } = format;

  switch (code) {
    case "WORKOUT_INCOMPLETE":
      return {
        required: {
          recordedSets: integer,
          prescribedSets: integer,
          completionPercent: percent,
        },
      };
    case "EXERCISE_SKIPPED":
    case "ALL_TARGETS_MET":
      return { required: { prescribedSets: integer } };
    case "REPS_BELOW_TARGET":
    case "REPS_ABOVE_TARGET":
      return { required: {} };
    case "WEIGHT_INCREASE":
    case "WEIGHT_DECREASE":
      return {
        required: { previousMaxKg: kg, currentMaxKg: kg, changePercent: signedPercent },
        baseline: { name: "baselineMaxKg", key: "baseline.weight", format: kg },
      };
    case "WEIGHT_PERSONAL_RECORD":
      return {
        required: { previousMaxKg: kg, currentMaxKg: kg, changePercent: signedPercent },
      };
    case "REPS_PERSONAL_RECORD":
      return {
        required: { weightKg: kg, previousBestReps: integer, currentReps: integer },
      };
    case "VOLUME_INCREASE":
    case "VOLUME_DECREASE":
      return {
        required: {
          previousVolumeKg: kg,
          currentVolumeKg: kg,
          changePercent: signedPercent,
        },
        baseline: { name: "baselineVolumeKg", key: "baseline.volume", format: kg },
      };
    case "REPS_INCREASE":
    case "REPS_DECREASE":
      return {
        required: {
          previousTotalReps: integer,
          currentTotalReps: integer,
          sets: integer,
          changePercent: signedPercent,
        },
        baseline: { name: "baselineTotalReps", key: "baseline.reps", format: integer },
      };
    case "REPEATED_TARGET_MISS":
      return { required: { consecutiveMisses: integer } };
    case "WEIGHT_PLATEAU":
      return { required: { baselineMaxKg: kg, executions: integer } };
    case "WEIGHT_UPTREND":
      return { required: { currentWeightKg: kg, consecutiveIncreases: integer } };
    case "WEIGHT_DOWNTREND":
      return { required: { currentWeightKg: kg, consecutiveDecreases: integer } };
    default:
      return null;
  }
}

/** The set positions, reps and targets a below- or above-target observation lists, zipped into one entry per set. */
function setDetailsOf(code: string, data: Data): SetDetail[] | undefined {
  const positions = numbersOf(data, "setPositions");
  const reps = numbersOf(data, "reps");
  const targets = numbersOf(data, code === "REPS_BELOW_TARGET" ? "targetMins" : "targetMaxes");

  if (
    positions === undefined ||
    reps === undefined ||
    targets === undefined ||
    positions.length !== reps.length ||
    positions.length !== targets.length
  ) {
    return undefined;
  }

  return positions.map((position, index) => ({
    position,
    reps: reps[index],
    target: targets[index],
  }));
}

/**
 * The titles and text to show for one observation. Which numbers matter and how they read is decided here, per code.
 * The API decides what was observed, so a code the front has no copy for shows the generic title.
 */
export function observationCopyOf(
  observation: Pick<WorkoutObservation, "code" | "data">,
  format: ObservationFormat,
): ObservationCopy {
  const { code } = observation;
  const data: Data = observation.data;
  const shape = shapeOf(code, format);

  if (shape === null) {
    return { title: "unknown.title", text: null, values: {}, details: [], baseline: null };
  }

  const title = `codes.${code}.title`;
  const none: ObservationCopy = { title, text: null, values: {}, details: [], baseline: null };
  const values: Record<string, string> = {};

  for (const [name, formatter] of Object.entries(shape.required)) {
    const value = numberOf(data, name);

    if (value === undefined) {
      return none;
    }
    values[name] = formatter(value);
  }

  const details =
    code === "REPS_BELOW_TARGET" || code === "REPS_ABOVE_TARGET"
      ? (setDetailsOf(code, data) ?? null)
      : [];

  if (details === null) {
    return none;
  }

  const baselineValue =
    shape.baseline === undefined ? undefined : numberOf(data, shape.baseline.name);
  const baseline =
    shape.baseline === undefined || baselineValue === undefined
      ? null
      : { key: shape.baseline.key, value: shape.baseline.format(baselineValue) };

  return { title, text: `codes.${code}.text`, values, details, baseline };
}
