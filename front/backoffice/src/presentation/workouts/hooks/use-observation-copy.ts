import { useTranslation } from "react-i18next";
import type { WorkoutById, WorkoutObservation } from "@weight-tracker/core/domain";
import useExerciseLabels from "@/presentation/workouts/hooks/use-exercise-labels";
import {
  exerciseOf,
  observationCopyOf,
  type ObservationFormat,
} from "@/presentation/workouts/workout-observations";

const KEY = "review.metrics.observations";

export interface ObservationText {
  title: string;
  /** `null` when the observation has no text to show, such as an unknown code. */
  text: string | null;
}

/**
 * Turns an observation into the title and text the trainer reads, in the current language. Weights and percentages
 * follow the locale, and the exercise is named from the workout under review.
 */
export default function useObservationCopy(workout: Pick<WorkoutById, "exercises">) {
  const { t, i18n } = useTranslation("workouts");
  const { nameOf } = useExerciseLabels();
  const number = new Intl.NumberFormat(i18n.language, { maximumFractionDigits: 2 });
  const signed = new Intl.NumberFormat(i18n.language, {
    signDisplay: "always",
    minimumFractionDigits: 1,
    maximumFractionDigits: 1,
  });
  const format: ObservationFormat = {
    kg: (value) => t("review.volumeValue", { value: number.format(value) }),
    signedPercent: (value) =>
      t("review.metrics.comparison.changeValue", { value: signed.format(value) }),
    percent: (value) => t("review.metrics.comparison.changeValue", { value: number.format(value) }),
    integer: (value) => number.format(value),
  };

  /** The name of the workout's exercise with this catalog id, or the generic one when the workout has none. */
  const exerciseNameOf = (exerciseId: string | null): string => {
    const exercise = exerciseOf(workout, exerciseId);

    return exercise === null
      ? t(`${KEY}.unknownExercise`)
      : nameOf({ id: exercise.exerciseId, translations: exercise.translations });
  };

  const copyOf = (observation: WorkoutObservation): ObservationText => {
    const copy = observationCopyOf(observation, format);
    const title = t(`${KEY}.${copy.title}`);

    if (copy.text === null) {
      return { title, text: null };
    }
    const detail = copy.details.map((entry) => t(`${KEY}.setDetail`, { ...entry })).join(" · ");
    const text = t(`${KEY}.${copy.text}`, {
      ...copy.values,
      exercise: exerciseNameOf(observation.exerciseId),
      detail,
    });

    return {
      title,
      text:
        copy.baseline === null
          ? text
          : `${text} ${t(`${KEY}.${copy.baseline.key}`, { value: copy.baseline.value })}`,
    };
  };

  return { copyOf, exerciseNameOf };
}
