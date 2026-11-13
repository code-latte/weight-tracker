import { useEffect, useState } from "react";

/** The current time, read once on mount and, while `live`, again every `intervalMs`. */
export default function useNow(live: boolean, intervalMs = 60_000): number {
  const [now, setNow] = useState(() => Date.now());

  useEffect(() => {
    if (!live) {
      return;
    }
    const interval = setInterval(() => setNow(Date.now()), intervalMs);

    return () => clearInterval(interval);
  }, [live, intervalMs]);

  return now;
}
