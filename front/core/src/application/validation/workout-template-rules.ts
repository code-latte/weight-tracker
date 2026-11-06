export const WORKOUT_TEMPLATE_NAME_MAX_LENGTH = 256;
export const WORKOUT_TEMPLATE_DESCRIPTION_MAX_LENGTH = 1024;

export type WorkoutTemplateFieldViolation = "required" | "maxLength";

/** A template's own data as typed in a form: every field is a string, empty when not filled. */
export interface WorkoutTemplateDetailsFormValues {
  name: string;
  description: string;
}

export type WorkoutTemplateDetailsFormViolations = Partial<
  Record<keyof WorkoutTemplateDetailsFormValues, WorkoutTemplateFieldViolation>
>;

/** The create template form as typed: the template's data plus the trainer who owns it. */
export interface CreateWorkoutTemplateFormValues extends WorkoutTemplateDetailsFormValues {
  trainerId: string;
}

export type CreateWorkoutTemplateFormViolations = Partial<
  Record<keyof CreateWorkoutTemplateFormValues, WorkoutTemplateFieldViolation>
>;

/**
 * Structural rules of a template's own data, with the API's limits. Both fields are trimmed before they are measured.
 * Shared by the create form and the template screen.
 */
export function validateWorkoutTemplateDetailsForm(
  values: WorkoutTemplateDetailsFormValues,
): WorkoutTemplateDetailsFormViolations {
  const violations: WorkoutTemplateDetailsFormViolations = {};
  const name = values.name.trim();

  if (name === "") {
    violations.name = "required";
  } else if (name.length > WORKOUT_TEMPLATE_NAME_MAX_LENGTH) {
    violations.name = "maxLength";
  }
  if (values.description.trim().length > WORKOUT_TEMPLATE_DESCRIPTION_MAX_LENGTH) {
    violations.description = "maxLength";
  }
  return violations;
}

/**
 * Structural rules of the create template form. `withTrainer` mirrors whom the API requires a trainer from; it is
 * passed in, never derived here.
 */
export function validateCreateWorkoutTemplateForm(
  values: CreateWorkoutTemplateFormValues,
  { withTrainer }: { withTrainer: boolean },
): CreateWorkoutTemplateFormViolations {
  const violations: CreateWorkoutTemplateFormViolations =
    validateWorkoutTemplateDetailsForm(values);

  if (withTrainer && values.trainerId.trim() === "") {
    violations.trainerId = "required";
  }
  return violations;
}
