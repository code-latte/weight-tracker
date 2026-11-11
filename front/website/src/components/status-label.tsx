import { soonLabel, type Status } from "@/content/landing";
import styles from "./status-label.module.css";

/** Says a capability is not available yet; shows nothing for one that is. */
export default function StatusLabel({
  status,
  onInk = false,
}: {
  status: Status;
  onInk?: boolean;
}) {
  if (status === "available") {
    return null;
  }
  return <span className={`${styles.label} ${onInk ? styles.onInk : ""}`}>{soonLabel}</span>;
}
