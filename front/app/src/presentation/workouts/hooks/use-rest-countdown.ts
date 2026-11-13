import { useCallback, useEffect, useState } from "react";
import { AppState } from "react-native";

type CountdownState =
  | { status: "idle" }
  | { status: "running"; endsAt: number; seconds: number }
  | { status: "paused"; remainingMs: number }
  | { status: "finished" };

const TICK_MS = 250;

const secondsLeft = (ms: number) => Math.ceil(Math.max(0, ms) / 1000);

function remainingSecondsOf(state: CountdownState, total: number): number {
  switch (state.status) {
    case "idle":
      return total;
    case "running":
      return state.seconds;
    case "paused":
      return secondsLeft(state.remainingMs);
    case "finished":
      return 0;
  }
}

/**
 * A rest countdown the athlete starts, pauses and resets. The remaining time is derived from the clock (the moment
 * the rest ends), never from counting ticks, so a late tick or time in the background cannot leave it behind. It
 * goes back to idle when the exercise changes or its rest time does; a `null` total (the workout is reloading) is
 * ignored.
 */
export default function useRestCountdown(exerciseKey: string, totalSeconds: number | null) {
  const [state, setState] = useState<CountdownState>({ status: "idle" });
  const [lastTotal, setLastTotal] = useState(totalSeconds);
  const [lastKey, setLastKey] = useState(exerciseKey);

  // Adjust state while rendering, so the first frame of a new exercise never shows the old countdown.
  if (exerciseKey !== lastKey || (totalSeconds !== null && totalSeconds !== lastTotal)) {
    setLastKey(exerciseKey);
    setLastTotal(totalSeconds ?? lastTotal);
    setState({ status: "idle" });
  }

  const total = totalSeconds ?? lastTotal ?? 0;

  const refresh = useCallback(() => {
    setState((current) => {
      if (current.status !== "running") {
        return current;
      }
      const ms = current.endsAt - Date.now();

      if (ms <= 0) {
        return { status: "finished" };
      }

      // Only a change of the shown second renders again.
      return secondsLeft(ms) === current.seconds
        ? current
        : { ...current, seconds: secondsLeft(ms) };
    });
  }, []);

  const running = state.status === "running";

  useEffect(() => {
    if (!running) {
      return;
    }
    const interval = setInterval(refresh, TICK_MS);
    const subscription = AppState.addEventListener("change", (next) => {
      if (next === "active") {
        refresh();
      }
    });

    return () => {
      clearInterval(interval);
      subscription.remove();
    };
  }, [running, refresh]);

  const play = useCallback(() => {
    setState((current) => {
      if (current.status === "running" || current.status === "finished") {
        return current;
      }
      const ms = current.status === "paused" ? current.remainingMs : total * 1000;

      return { status: "running", endsAt: Date.now() + ms, seconds: secondsLeft(ms) };
    });
  }, [total]);

  const pause = useCallback(() => {
    setState((current) => {
      if (current.status !== "running") {
        return current;
      }
      return { status: "paused", remainingMs: Math.max(0, current.endsAt - Date.now()) };
    });
  }, []);

  const reset = useCallback(() => setState({ status: "idle" }), []);

  return {
    status: state.status,
    remainingSeconds: remainingSecondsOf(state, total),
    play,
    pause,
    reset,
  };
}
