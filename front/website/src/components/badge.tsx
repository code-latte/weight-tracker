import type { ReactNode } from "react";
import styles from "./badge.module.css";

/** A short uppercase label: an overline, a programme name. */
export default function Badge({
  tone = "teal",
  children,
}: {
  tone?: "teal" | "orange" | "onInk";
  children: ReactNode;
}) {
  return <span className={`${styles.badge} ${styles[tone]}`}>{children}</span>;
}
