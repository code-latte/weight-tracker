export type ObservationSeverity = "positive" | "info" | "warning";

/** The shape the API will give each observation of a session: a code to translate and a severity to colour. */
export interface SessionObservation {
  code: "LOAD_PROGRESSION" | "GOOD_COMPLIANCE" | "SET_BELOW_TARGET";
  severity: ObservationSeverity;
}

/**
 * Sample observations, the same for every workout, until the API returns the real ones. Their texts are fixed
 * examples in the translations, and the review marks them as a preview.
 */
export const SESSION_OBSERVATIONS_PREVIEW: readonly SessionObservation[] = [
  { code: "LOAD_PROGRESSION", severity: "positive" },
  { code: "GOOD_COMPLIANCE", severity: "info" },
  { code: "SET_BELOW_TARGET", severity: "warning" },
];
