import { createContext } from "react";
import type { DomainErrorCode, Exercise } from "@weight-tracker/core/domain";

export type ExerciseCatalogStatus = "idle" | "loading" | "loaded" | "failed";

export interface ExerciseCatalogContextValue {
  status: ExerciseCatalogStatus;
  exercises: Exercise[];
  errorCode: DomainErrorCode | null;
  /** Reads the catalog when none was requested yet; does nothing otherwise. */
  load: () => void;
  /** Reads the catalog again, after a failure. */
  reload: () => void;
  /** Reads the catalog again in the background, keeping the current one until the answer arrives. */
  refresh: () => void;
  /** Lists an exercise the user just created, before the catalog is read again. */
  add: (exercise: Exercise) => void;
  /** Forgets the catalog, as on logout. */
  clear: () => void;
}

const ExerciseCatalogContext = createContext<ExerciseCatalogContextValue | null>(null);

export default ExerciseCatalogContext;
