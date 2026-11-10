"use client";

import { useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from "react";
import type { DomainErrorCode, Exercise } from "@weight-tracker/core/domain";
import useSession from "@/presentation/auth/hooks/use-session";
import ExerciseCatalogContext, {
  type ExerciseCatalogContextValue,
  type ExerciseCatalogStatus,
} from "../context/exercise-catalog-context";
import useContainer from "../hooks/use-container";

interface ExerciseCatalogProviderProps {
  children: ReactNode;
}

/**
 * Holds the exercise catalog for the whole signed-in session, so the screens that need it do not read it again. It
 * reads nothing until `load` is called, and only one read is ever in flight.
 */
export default function ExerciseCatalogProvider({ children }: ExerciseCatalogProviderProps) {
  const container = useContainer();
  const { runWithSession, onEnd } = useSession();
  const [status, setStatus] = useState<ExerciseCatalogStatus>("idle");
  const [exercises, setExercises] = useState<Exercise[]>([]);
  const [errorCode, setErrorCode] = useState<DomainErrorCode | null>(null);
  const statusRef = useRef<ExerciseCatalogStatus>("idle");
  // Bumped by every read and by `clear`, so an answer that arrives after either is ignored.
  const generation = useRef(0);
  const inFlight = useRef(false);
  // A refresh asked for while a read runs: that read may predate the change, so one more follows it.
  const refreshQueued = useRef(false);
  // The latest `read`, so an answer can start the queued refresh without `read` referring to itself.
  const readLatest = useRef<(background: boolean) => void>(() => {});

  const changeStatus = useCallback((next: ExerciseCatalogStatus) => {
    statusRef.current = next;
    setStatus(next);
  }, []);

  const read = useCallback(
    (background: boolean) => {
      if (inFlight.current) {
        if (background) {
          refreshQueued.current = true;
        }
        return;
      }
      // A refresh only stays in the background while there is a catalog to keep showing.
      const keepShowing = background && statusRef.current === "loaded";
      const current = ++generation.current;
      inFlight.current = true;
      if (!keepShowing) {
        changeStatus("loading");
        setErrorCode(null);
      }

      void runWithSession(() => container.resolve("getExercisesUseCase").execute()).then(
        (result) => {
          if (current !== generation.current) {
            return;
          }
          inFlight.current = false;
          if (result.ok) {
            setExercises(result.value);
            setErrorCode(null);
            changeStatus("loaded");
          } else if (!keepShowing) {
            setErrorCode(result.error.code);
            changeStatus("failed");
          }
          // A failed refresh keeps the catalog already shown, without an error.
          if (refreshQueued.current) {
            refreshQueued.current = false;
            readLatest.current(true);
          }
        },
      );
    },
    [container, runWithSession, changeStatus],
  );

  useEffect(() => {
    readLatest.current = read;
  }, [read]);

  const load = useCallback(() => {
    if (statusRef.current === "idle") {
      read(false);
    }
  }, [read]);

  const reload = useCallback(() => read(false), [read]);

  const refresh = useCallback(() => read(true), [read]);

  const add = useCallback((exercise: Exercise) => {
    setExercises((current) => [...current, exercise]);
  }, []);

  const clear = useCallback(() => {
    generation.current += 1;
    inFlight.current = false;
    refreshQueued.current = false;
    setExercises([]);
    setErrorCode(null);
    changeStatus("idle");
  }, [changeStatus]);

  // A session that ends takes the catalog with it, so the next user never sees it.
  useEffect(() => onEnd(clear), [onEnd, clear]);

  const value = useMemo<ExerciseCatalogContextValue>(
    () => ({ status, exercises, errorCode, load, reload, refresh, add, clear }),
    [status, exercises, errorCode, load, reload, refresh, add, clear],
  );

  return <ExerciseCatalogContext value={value}>{children}</ExerciseCatalogContext>;
}
