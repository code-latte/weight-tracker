import type { RegisterLeadFailure } from "./register-lead";

/** The email the API accepts at most (its `Email` value object's limit). */
export const MAX_EMAIL_LENGTH = 254;

// Structure only: the API normalises the email and decides whether it is valid.
const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export interface FieldErrors {
  email?: true;
  consent?: true;
}

/** The structural problems of a form, if any: the email and the consent are both required. */
export function fieldErrorsOf(email: string, consent: boolean): FieldErrors {
  const trimmed = email.trim();
  const errors: FieldErrors = {};
  if (!EMAIL_PATTERN.test(trimmed) || trimmed.length > MAX_EMAIL_LENGTH) {
    errors.email = true;
  }
  if (!consent) {
    errors.consent = true;
  }
  return errors;
}

export type LeadFormState =
  | { status: "idle" }
  | { status: "submitting" }
  | { status: "sent" }
  | { status: "failed"; reason: RegisterLeadFailure };

export type LeadFormAction =
  { type: "submit" } | { type: "sent" } | { type: "failed"; reason: RegisterLeadFailure };

/** A sent form stays sent; a form already sending ignores another submit, so a double click sends once. */
export function leadFormReducer(state: LeadFormState, action: LeadFormAction): LeadFormState {
  if (state.status === "sent") {
    return state;
  }
  switch (action.type) {
    case "submit":
      return state.status === "submitting" ? state : { status: "submitting" };
    case "sent":
      return { status: "sent" };
    case "failed":
      return { status: "failed", reason: action.reason };
  }
}
