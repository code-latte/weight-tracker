const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

/** A value shaped like an email address. Whether it exists or may be used is left to the API. */
export function isEmail(value: string): boolean {
  return EMAIL_PATTERN.test(value);
}
