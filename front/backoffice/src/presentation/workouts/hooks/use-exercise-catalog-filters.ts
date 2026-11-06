import { useState } from "react";
import type { ExerciseSection } from "@/presentation/workouts/catalog";

export interface ExerciseCatalogFilters {
  /** The section of the catalog shown; it narrows the exercises before the other filters apply. */
  section: ExerciseSection;
  setSection: (section: ExerciseSection) => void;
  text: string;
  setText: (text: string) => void;
  /** The exercise type pill selected, if any; it never shows or hides another row. */
  type: string | null;
  toggleType: (type: string) => void;
  bodyPart: string | null;
  toggleBodyPart: (bodyPart: string) => void;
  /** Only meaningful while a body part is selected; changing the body part clears it. */
  muscle: string | null;
  toggleMuscle: (muscle: string) => void;
}

/**
 * The section, the text and the pills of the catalog. Held by the screen, not by the catalog, so they survive a
 * dialog closing and opening again; selecting the selected pill again clears it.
 */
export default function useExerciseCatalogFilters(): ExerciseCatalogFilters {
  const [section, setSection] = useState<ExerciseSection>("ALL");
  const [text, setText] = useState("");
  const [type, setType] = useState<string | null>(null);
  const [bodyPart, setBodyPart] = useState<string | null>(null);
  const [muscle, setMuscle] = useState<string | null>(null);

  return {
    section,
    setSection,
    text,
    setText,
    type,
    toggleType: (selected) => setType((current) => (current === selected ? null : selected)),
    bodyPart,
    toggleBodyPart: (selected) => {
      setBodyPart((current) => (current === selected ? null : selected));
      // A muscle only refines its own body part.
      setMuscle(null);
    },
    muscle,
    toggleMuscle: (selected) => setMuscle((current) => (current === selected ? null : selected)),
  };
}
