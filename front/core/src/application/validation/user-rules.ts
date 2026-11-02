import { isCalendarDate } from "./calendar-date";
import { isEmail } from "./email";
import { PROFILE_NAME_MAX_LENGTH } from "./profile-name-rules";

export type UserFieldViolation = "required" | "email" | "maxLength" | "date";

/** The create user form as typed: every field is a string, empty when not filled. `role` holds the role's value. */
export interface CreateUserFormValues {
  email: string;
  password: string;
  role: string;
  name: string;
  surname: string;
  dateBirth: string;
}

export type CreateUserFormViolations = Partial<
  Record<keyof CreateUserFormValues, UserFieldViolation>
>;

function validateOptionalName(value: string): UserFieldViolation | null {
  return value.trim().length > PROFILE_NAME_MAX_LENGTH ? "maxLength" : null;
}

/**
 * Structural rules of the create user form, with the API's limits. Names are trimmed before they are measured.
 * Password strength and email uniqueness are left to the API.
 */
export function validateCreateUserForm(values: CreateUserFormValues): CreateUserFormViolations {
  const violations: CreateUserFormViolations = {};
  const email = values.email.trim();

  if (email === "") {
    violations.email = "required";
  } else if (!isEmail(email)) {
    violations.email = "email";
  }
  if (values.password === "") {
    violations.password = "required";
  }
  if (values.role === "") {
    violations.role = "required";
  }
  const name = validateOptionalName(values.name);
  if (name) {
    violations.name = name;
  }
  const surname = validateOptionalName(values.surname);
  if (surname) {
    violations.surname = surname;
  }
  if (values.dateBirth !== "" && !isCalendarDate(values.dateBirth)) {
    violations.dateBirth = "date";
  }
  return violations;
}
