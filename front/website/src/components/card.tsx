import type { ReactNode } from "react";
import styles from "./card.module.css";

/** A surface for a group of content; `flat` keeps it still on hover, for cards that hold controls. */
export default function Card({ flat = false, children }: { flat?: boolean; children: ReactNode }) {
  return <div className={`${styles.card} ${flat ? styles.flat : ""}`}>{children}</div>;
}
