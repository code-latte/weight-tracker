import { use } from "react";
import ExerciseCatalogContext, {
  type ExerciseCatalogContextValue,
} from "../context/exercise-catalog-context";

/** The session's exercise catalog, kept by `ExerciseCatalogProvider`. */
export default function useExerciseCatalog(): ExerciseCatalogContextValue {
  const context = use(ExerciseCatalogContext);

  if (context === null) {
    throw new Error("useExerciseCatalog must be used inside ExerciseCatalogProvider");
  }

  return context;
}
