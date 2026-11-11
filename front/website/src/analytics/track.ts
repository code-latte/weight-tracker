import type { CtaLocation, SectionId } from "@/content/landing";
import { type Attribution, visitAttribution } from "./attribution";

/** The events the landing emits, with the data each carries. */
export type AnalyticsEvent =
  | { name: "cta_click"; location: CtaLocation }
  | { name: "form_start" }
  | { name: "form_submit"; outcome: "sent" | "invalid" | "tooMany" | "network" }
  | { name: "section_view"; section: SectionId };

/** An event as a provider would receive it: with the visit's attribution. */
export type TrackedEvent = AnalyticsEvent & Attribution;

type Sink = (event: TrackedEvent) => void;

// No provider yet: events go nowhere and no cookie is set. A later change replaces the sink.
let sink: Sink = () => {};

/** Emits one named event with the visit's attribution. The only way the website reports anything. */
export function track(event: AnalyticsEvent): void {
  sink({ ...visitAttribution(), ...event });
}

/** Replaces where events go and returns the previous sink; for tests only. */
export function setSink(next: Sink): Sink {
  const previous = sink;
  sink = next;
  return previous;
}
