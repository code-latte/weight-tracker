import { isCalendarDate } from "./calendar-date";
import { isMinutesText } from "./time-in-minutes";

export const WORKOUT_NAME_MAX_LENGTH = 256;
export const WORKOUT_DESCRIPTION_MAX_LENGTH = 1024;

export type WorkoutFieldViolation = "required" | "maxLength" | "date" | "uuid";

/** A workout's own data as typed in a form: every field is a string, empty when not filled. */
export interface WorkoutDetailsFormValues {
  name: string;
  description: string;
  workoutDate: string;
}

export type WorkoutDetailsFormViolations = Partial<
  Record<keyof WorkoutDetailsFormValues, WorkoutFieldViolation>
>;

/** The create workout form as typed: the workout's data plus who it is for. */
export interface CreateWorkoutFormValues extends WorkoutDetailsFormValues {
  userId: string;
  trainerId: string;
}

export type CreateWorkoutFormViolations = Partial<
  Record<keyof CreateWorkoutFormValues, WorkoutFieldViolation>
>;

const UUID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

function validateId(value: string): WorkoutFieldViolation | null {
  const id = value.trim();

  if (id === "") {
    return "required";
  }
  return UUID_PATTERN.test(id) ? null : "uuid";
}

/**
 * Structural rules of a workout's own data, with the API's limits. The name is trimmed before it is measured.
 * Shared by the create form and the workout screen.
 */
export function validateWorkoutDetailsForm(
  values: WorkoutDetailsFormValues,
): WorkoutDetailsFormViolations {
  const violations: WorkoutDetailsFormViolations = {};
  const name = values.name.trim();

  if (name === "") {
    violations.name = "required";
  } else if (name.length > WORKOUT_NAME_MAX_LENGTH) {
    violations.name = "maxLength";
  }
  if (values.description.length > WORKOUT_DESCRIPTION_MAX_LENGTH) {
    violations.description = "maxLength";
  }
  if (values.workoutDate === "") {
    violations.workoutDate = "required";
  } else if (!isCalendarDate(values.workoutDate)) {
    violations.workoutDate = "date";
  }
  return violations;
}

/**
 * Structural rules of the create workout form, with the API's limits. The name is trimmed before it is measured.
 * `withTrainer` mirrors whom the API requires a trainer from; it is passed in, never derived here.
 */
export function validateCreateWorkoutForm(
  values: CreateWorkoutFormValues,
  { withTrainer }: { withTrainer: boolean },
): CreateWorkoutFormViolations {
  const violations: CreateWorkoutFormViolations = validateWorkoutDetailsForm(values);
  const userId = validateId(values.userId);
  if (userId) {
    violations.userId = userId;
  }
  if (withTrainer) {
    const trainerId = validateId(values.trainerId);
    if (trainerId) {
      violations.trainerId = trainerId;
    }
  }
  return violations;
}

/** The API's set target types that need no value, and those that need a maximum as well. */
const TARGET_TYPES_WITHOUT_VALUE = new Set(["MaxReps"]);
const TARGET_TYPES_WITH_MAXIMUM = new Set(["RepsRange"]);

export type SetFieldViolation = "positiveInteger" | "minutesFormat";

/** One set prescription as typed in a form: every field is a string, empty when not filled. */
export interface SetFormValues {
  count: string;
  type: string;
  value: string;
  maxValue: string;
}

export type SetFormViolations = Partial<Record<keyof SetFormValues, SetFieldViolation>>;

const POSITIVE_INTEGER_PATTERN = /^[1-9]\d*$/;

/** Whether the type of a set needs a value, and a maximum. */
export function targetFieldsOf(type: string): { value: boolean; maxValue: boolean } {
  return {
    value: !TARGET_TYPES_WITHOUT_VALUE.has(type),
    maxValue: TARGET_TYPES_WITH_MAXIMUM.has(type),
  };
}

/**
 * Structural rules of one set: the number of sets, and the target fields its type needs, are whole numbers from 1,
 * except a minutes value, typed as `M` or `M:SS`. Whether a range's maximum is above its minimum is left to the API.
 */
export function validateSetForm(values: SetFormValues): SetFormViolations {
  const violations: SetFormViolations = {};
  const needs = targetFieldsOf(values.type);

  if (!POSITIVE_INTEGER_PATTERN.test(values.count.trim())) {
    violations.count = "positiveInteger";
  }
  if (needs.value && values.type === "Minutes") {
    if (!isMinutesText(values.value)) {
      violations.value = "minutesFormat";
    }
  } else if (needs.value && !POSITIVE_INTEGER_PATTERN.test(values.value.trim())) {
    violations.value = "positiveInteger";
  }
  if (needs.maxValue && !POSITIVE_INTEGER_PATTERN.test(values.maxValue.trim())) {
    violations.maxValue = "positiveInteger";
  }
  return violations;
}

export interface ExerciseConfigurationViolations {
  effort?: "required";
  sets?: "minItems";
  rest?: "minutesFormat";
}

/** An exercise is saved with an effort and at least one set; its rest is optional, typed as `M` or `M:SS`. */
export function validateExerciseConfiguration({
  effort,
  setCount,
  rest = "",
}: {
  effort: string;
  setCount: number;
  rest?: string;
}): ExerciseConfigurationViolations {
  const violations: ExerciseConfigurationViolations = {};

  if (effort === "") {
    violations.effort = "required";
  }
  if (setCount < 1) {
    violations.sets = "minItems";
  }
  if (rest.trim() !== "" && !isMinutesText(rest)) {
    violations.rest = "minutesFormat";
  }
  return violations;
}
