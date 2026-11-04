export const EXERCISE_NAME_MIN_LENGTH = 3;
export const EXERCISE_NAME_MAX_LENGTH = 150;
export const EXERCISE_VIDEO_URL_MAX_LENGTH = 500;

export type ExerciseFieldViolation = "required" | "minLength" | "maxLength" | "url";

/** The exercise form as typed: every field is a string, empty when not filled. */
export interface ExerciseFormValues {
  name: string;
  muscle: string;
  type: string;
  videoUrl: string;
}

export type ExerciseFormViolations = Partial<
  Record<keyof ExerciseFormValues, ExerciseFieldViolation>
>;

/**
 * An absolute `http` or `https` address with no whitespace and a dot in its host, as the API accepts. The host is read
 * by hand because `core` has no DOM library, so no `URL`.
 */
function isWebAddress(value: string): boolean {
  const authority = /^https?:\/\/([^/?#]*)/i.exec(value)?.[1];

  if (authority === undefined || /\s/.test(value)) {
    return false;
  }
  const host = authority.replace(/^.*@/, "").replace(/:\d*$/, "");
  return host.includes(".");
}

/**
 * Structural rules of the exercise form, with the API's limits: the name, trimmed, is required and 3 to 150
 * characters; the muscle and the type are required; the video link is optional and, when given, a web address of at
 * most 500 characters. Nothing else is checked: the API owns the rest.
 */
export function validateExerciseForm(values: ExerciseFormValues): ExerciseFormViolations {
  const violations: ExerciseFormViolations = {};
  const name = values.name.trim();
  const videoUrl = values.videoUrl.trim();

  if (name === "") {
    violations.name = "required";
  } else if (name.length < EXERCISE_NAME_MIN_LENGTH) {
    violations.name = "minLength";
  } else if (name.length > EXERCISE_NAME_MAX_LENGTH) {
    violations.name = "maxLength";
  }
  if (values.muscle === "") {
    violations.muscle = "required";
  }
  if (values.type === "") {
    violations.type = "required";
  }
  if (videoUrl.length > EXERCISE_VIDEO_URL_MAX_LENGTH) {
    violations.videoUrl = "maxLength";
  } else if (videoUrl !== "" && !isWebAddress(videoUrl)) {
    violations.videoUrl = "url";
  }
  return violations;
}
