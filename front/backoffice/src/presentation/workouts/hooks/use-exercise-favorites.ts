import { useCallback, useEffect, useRef, useState } from "react";
import type { DomainErrorCode } from "@weight-tracker/core/domain";
import useSession from "@/presentation/auth/hooks/use-session";
import useContainer from "@/presentation/common/hooks/use-container";

type ExerciseFavoritesStatus = "loading" | "loaded" | "failed";

export interface ExerciseFavoritesState {
  status: ExerciseFavoritesStatus;
  /** The ids of the user's favorite exercises. Changes only when the API accepts a mark or an unmark. */
  favoriteIds: ReadonlySet<string>;
  loadErrorCode: DomainErrorCode | null;
  /** The exercises whose mark or unmark is in progress. */
  pendingIds: ReadonlySet<string>;
  /** The code of the last refused mark or unmark; the next toggle clears it. */
  toggleErrorCode: DomainErrorCode | null;
  /** Marks the exercise as a favorite, or unmarks it when it already is one. */
  toggle: (exerciseId: string) => Promise<void>;
}

/** Reads the user's favorites when the screen opens. Held by the screen only, so it resets on exit. */
export default function useExerciseFavorites(): ExerciseFavoritesState {
  const container = useContainer();
  const { runWithSession } = useSession();
  const [status, setStatus] = useState<ExerciseFavoritesStatus>("loading");
  const [favoriteIds, setFavoriteIds] = useState<ReadonlySet<string>>(new Set());
  const [loadErrorCode, setLoadErrorCode] = useState<DomainErrorCode | null>(null);
  const [pendingIds, setPendingIds] = useState<ReadonlySet<string>>(new Set());
  const [toggleErrorCode, setToggleErrorCode] = useState<DomainErrorCode | null>(null);
  // Bumped on unmount, so an answer that arrives after it is ignored.
  const generation = useRef(0);

  useEffect(() => {
    const current = ++generation.current;

    void runWithSession(() => container.resolve("getExerciseFavoritesUseCase").execute()).then(
      (result) => {
        if (current !== generation.current) {
          return;
        }
        if (result.ok) {
          setFavoriteIds(new Set(result.value.map((favorite) => favorite.exerciseId)));
          setStatus("loaded");
        } else {
          setLoadErrorCode(result.error.code);
          setStatus("failed");
        }
      },
    );

    return () => {
      generation.current += 1;
    };
  }, [container, runWithSession]);

  const toggle = useCallback(
    async (exerciseId: string) => {
      const marking = !favoriteIds.has(exerciseId);

      setPendingIds((current) => new Set(current).add(exerciseId));
      setToggleErrorCode(null);

      const result = marking
        ? await runWithSession(() =>
            container.resolve("markExerciseFavoriteUseCase").execute(exerciseId),
          )
        : await runWithSession(() =>
            container.resolve("unmarkExerciseFavoriteUseCase").execute(exerciseId),
          );

      setPendingIds((current) => {
        const next = new Set(current);
        next.delete(exerciseId);
        return next;
      });
      if (!result.ok) {
        setToggleErrorCode(result.error.code);
        return;
      }
      setFavoriteIds((current) => {
        const next = new Set(current);
        if (marking) {
          next.add(exerciseId);
        } else {
          next.delete(exerciseId);
        }
        return next;
      });
    },
    [container, runWithSession, favoriteIds],
  );

  return { status, favoriteIds, loadErrorCode, pendingIds, toggleErrorCode, toggle };
}
