"use client";

import { useEffect } from "react";
import type { SectionId } from "@/content/landing";
import { track } from "./track";

/** Reports each section once, the first time at least a third of it is on screen. Renders nothing. */
export default function SectionViews() {
  useEffect(() => {
    if (typeof IntersectionObserver === "undefined") {
      return;
    }
    const seen = new Set<string>();
    const observer = new IntersectionObserver(
      (entries) => {
        for (const entry of entries) {
          const section = (entry.target as HTMLElement).dataset.section;
          if (!entry.isIntersecting || !section || seen.has(section)) {
            continue;
          }
          seen.add(section);
          observer.unobserve(entry.target);
          track({ name: "section_view", section: section as SectionId });
        }
      },
      { threshold: 0.33 },
    );

    document
      .querySelectorAll<HTMLElement>("[data-section]")
      .forEach((element) => observer.observe(element));
    return () => observer.disconnect();
  }, []);

  return null;
}
