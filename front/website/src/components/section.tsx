import type { ReactNode } from "react";
import type { SectionId } from "@/content/landing";
import styles from "./section.module.css";

export type SectionTone = "paper" | "fog" | "background" | "ink" | "gradient";

interface SectionProps {
  id: SectionId;
  tone?: SectionTone;
  /** The level-2 heading; the hero passes none, since it holds the page's only level-1 heading. */
  title?: string;
  lead?: string;
  centered?: boolean;
  children: ReactNode;
}

/** A landing section: its anchor, its background and a centred container, with an optional heading. */
export default function Section({
  id,
  tone = "paper",
  title,
  lead,
  centered = false,
  children,
}: SectionProps) {
  const headingId = title ? `${id}-titulo` : undefined;

  return (
    <section
      id={id}
      data-section={id}
      aria-labelledby={headingId}
      className={`${styles.section} ${styles[tone]}`}
    >
      <div className={styles.container}>
        {title && (
          <header className={`${styles.heading} ${centered ? styles.centered : ""}`}>
            <h2 id={headingId} className={styles.title}>
              {title}
            </h2>
            {lead && <p className={styles.lead}>{lead}</p>}
          </header>
        )}
        {children}
      </div>
    </section>
  );
}
