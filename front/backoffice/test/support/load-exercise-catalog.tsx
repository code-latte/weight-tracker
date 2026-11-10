import { useEffect, type ReactNode } from "react";
import useExerciseCatalog from "@/presentation/common/hooks/use-exercise-catalog";

/**
 * Stands in for `AppShell` in tests of a screen opened after the session's catalog was read: asks for the catalog once
 * on mount and renders its children only when it is in.
 */
export default function LoadExerciseCatalog({ children }: { children: ReactNode }) {
  const { status, load } = useExerciseCatalog();

  useEffect(() => {
    load();
  }, [load]);

  return status === "loaded" ? children : null;
}
