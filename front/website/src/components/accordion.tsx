"use client";

import { useId, useState } from "react";
import { ChevronDown } from "lucide-react";
import styles from "./accordion.module.css";

export interface AccordionItem {
  question: string;
  answer: string;
}

/** Questions that each show or hide their answer; several can be open at once. */
export default function Accordion({ items }: { items: AccordionItem[] }) {
  const baseId = useId();
  const [open, setOpen] = useState<ReadonlySet<number>>(new Set());

  const toggle = (index: number) =>
    setOpen((current) => {
      const next = new Set(current);
      if (next.has(index)) {
        next.delete(index);
      } else {
        next.add(index);
      }
      return next;
    });

  return (
    <div className={styles.list}>
      {items.map((item, index) => {
        const expanded = open.has(index);
        const triggerId = `${baseId}-q${index}`;
        const panelId = `${baseId}-a${index}`;

        return (
          <div key={item.question} className={`${styles.item} ${expanded ? styles.open : ""}`}>
            <h3 className={styles.question}>
              <button
                id={triggerId}
                type="button"
                className={styles.trigger}
                aria-expanded={expanded}
                aria-controls={panelId}
                onClick={() => toggle(index)}
              >
                {item.question}
                <span className={styles.chevron}>
                  <ChevronDown size={20} aria-hidden="true" />
                </span>
              </button>
            </h3>
            <div id={panelId} role="region" aria-labelledby={triggerId} hidden={!expanded}>
              <p className={styles.answer}>{item.answer}</p>
            </div>
          </div>
        );
      })}
    </div>
  );
}
