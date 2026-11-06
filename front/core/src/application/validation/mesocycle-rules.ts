import { isCalendarDate } from "./calendar-date";

export const MESOCYCLE_NAME_MAX_LENGTH = 256;
export const MESOCYCLE_OBJECTIVE_MAX_LENGTH = 4000;
export const MESOCYCLE_DURATION_MIN = 1;
export const MESOCYCLE_DURATION_MAX = 52;

export type MesocycleFieldViolation = "required" | "maxLength" | "date" | "range" | "uuid";

/** A mesocycle's own data as typed in a form: every field is a string, empty when not filled. */
export interface MesocycleDetailsFormValues {
  name: string;
  objective: string;
  startDate: string;
  durationInWeeks: string;
}

export type MesocycleDetailsFormViolations = Partial<
  Record<keyof MesocycleDetailsFormValues, MesocycleFieldViolation>
>;

/** The create mesocycle form as typed: the mesocycle's data plus who it is for. */
export interface CreateMesocycleFormValues extends MesocycleDetailsFormValues {
  athleteId: string;
  trainerId: string;
}

export type CreateMesocycleFormViolations = Partial<
  Record<keyof CreateMesocycleFormValues, MesocycleFieldViolation>
>;

/** One week of a mesocycle as typed in a form: its type is empty until one is chosen. */
export interface MesocycleWeekFormValues {
  trainingWeekType: string;
  objective: string;
}

export type MesocycleWeekFormViolations = Partial<
  Record<keyof MesocycleWeekFormValues, MesocycleFieldViolation>
>;

const UUID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
const INTEGER_PATTERN = /^-?\d+$/;

function validateId(value: string): MesocycleFieldViolation | null {
  const id = value.trim();

  if (id === "") {
    return "required";
  }
  return UUID_PATTERN.test(id) ? null : "uuid";
}

/** A required text, trimmed before it is measured. */
function validateText(value: string, maxLength: number): MesocycleFieldViolation | null {
  const text = value.trim();

  if (text === "") {
    return "required";
  }
  return text.length > maxLength ? "maxLength" : null;
}

function validateDuration(value: string): MesocycleFieldViolation | null {
  const duration = value.trim();

  if (duration === "") {
    return "required";
  }
  if (!INTEGER_PATTERN.test(duration)) {
    return "range";
  }
  const weeks = Number(duration);

  return weeks < MESOCYCLE_DURATION_MIN || weeks > MESOCYCLE_DURATION_MAX ? "range" : null;
}

/**
 * Structural rules of a mesocycle's own data, with the API's limits. Texts are trimmed before they are measured.
 * Shared by the create and the edit form.
 */
export function validateMesocycleDetailsForm(
  values: MesocycleDetailsFormValues,
): MesocycleDetailsFormViolations {
  const violations: MesocycleDetailsFormViolations = {};
  const name = validateText(values.name, MESOCYCLE_NAME_MAX_LENGTH);
  const objective = validateText(values.objective, MESOCYCLE_OBJECTIVE_MAX_LENGTH);
  const durationInWeeks = validateDuration(values.durationInWeeks);

  if (name) {
    violations.name = name;
  }
  if (objective) {
    violations.objective = objective;
  }
  if (values.startDate === "") {
    violations.startDate = "required";
  } else if (!isCalendarDate(values.startDate)) {
    violations.startDate = "date";
  }
  if (durationInWeeks) {
    violations.durationInWeeks = durationInWeeks;
  }
  return violations;
}

/**
 * Structural rules of the create mesocycle form, with the API's limits. `withTrainer` mirrors whom the API requires a
 * trainer from; it is passed in, never derived here.
 */
export function validateCreateMesocycleForm(
  values: CreateMesocycleFormValues,
  { withTrainer }: { withTrainer: boolean },
): CreateMesocycleFormViolations {
  const violations: CreateMesocycleFormViolations = validateMesocycleDetailsForm(values);
  const athleteId = validateId(values.athleteId);
  if (athleteId) {
    violations.athleteId = athleteId;
  }
  if (withTrainer) {
    const trainerId = validateId(values.trainerId);
    if (trainerId) {
      violations.trainerId = trainerId;
    }
  }
  return violations;
}

/** Structural rules of one mesocycle week: a chosen type and an objective within the API's limit. */
export function validateMesocycleWeekForm(
  values: MesocycleWeekFormValues,
): MesocycleWeekFormViolations {
  const violations: MesocycleWeekFormViolations = {};
  const objective = validateText(values.objective, MESOCYCLE_OBJECTIVE_MAX_LENGTH);

  if (values.trainingWeekType === "") {
    violations.trainingWeekType = "required";
  }
  if (objective) {
    violations.objective = objective;
  }
  return violations;
}
