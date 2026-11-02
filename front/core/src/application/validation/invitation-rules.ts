import { isEmail } from "./email";

export type InvitationFieldViolation = "required" | "email";

/** The invite athlete form as typed: the email, empty when not filled. */
export interface InviteAthleteFormValues {
  email: string;
}

export type InviteAthleteFormViolations = Partial<
  Record<keyof InviteAthleteFormValues, InvitationFieldViolation>
>;

/**
 * Structural rules of the invite athlete form. The email is trimmed before it is checked. Whether that email can be
 * invited is left to the API.
 */
export function validateInviteAthleteForm(
  values: InviteAthleteFormValues,
): InviteAthleteFormViolations {
  const email = values.email.trim();

  if (email === "") {
    return { email: "required" };
  }
  if (!isEmail(email)) {
    return { email: "email" };
  }
  return {};
}
