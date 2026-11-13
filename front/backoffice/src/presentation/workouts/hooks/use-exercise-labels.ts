import { createElement, type ReactNode, useCallback, useMemo } from "react";
import type { SvgIconComponent } from "@mui/icons-material";
import Favorite from "@mui/icons-material/Favorite";
import FitnessCenter from "@mui/icons-material/FitnessCenter";
import SelfImprovement from "@mui/icons-material/SelfImprovement";
import type { CreateExerciseRequest, Exercise } from "@weight-tracker/core/domain";
import { useTranslation } from "react-i18next";

/** A chip colour of the MUI theme, including the exercise type colours it adds. */
export type LabelColor =
  | "default"
  | "primary"
  | "secondary"
  | "error"
  | "info"
  | "success"
  | "warning"
  | "strength"
  | "cardio"
  | "stretching";

/** One colour per API body part, so a body part looks the same everywhere. */
const BODY_PART_COLORS: Record<string, LabelColor> = {
  CHEST: "error",
  BACK: "info",
  LEGS: "success",
  ARMS: "warning",
  SHOULDERS: "secondary",
  CORE: "primary",
  NECK: "default",
};

/**
 * The muscles an exercise can be created with. Typed from the API's request, so a muscle the API adds or drops fails
 * to compile here.
 */
export const EXERCISE_MUSCLES: NonNullable<CreateExerciseRequest["muscle"]>[] = [
  "ABDOMINALS",
  "ABDUCTORS",
  "ADDUCTORS",
  "BICEPS",
  "CALVES",
  "CHEST",
  "FOREARMS",
  "GLUTES",
  "HAMSTRINGS",
  "LATS",
  "LOWER_BACK",
  "MIDDLE_BACK",
  "NECK",
  "QUADRICEPS",
  "SHOULDERS",
  "TRAPS",
  "TRICEPS",
];

/**
 * The body part each muscle belongs to, only to group the muscles of the new exercise form. The API derives the
 * exercise's body part from its muscle and stays the authority: this never reaches a request.
 */
export const MUSCLE_BODY_PARTS: Record<(typeof EXERCISE_MUSCLES)[number], string> = {
  ABDOMINALS: "CORE",
  ABDUCTORS: "LEGS",
  ADDUCTORS: "LEGS",
  BICEPS: "ARMS",
  CALVES: "LEGS",
  CHEST: "CHEST",
  FOREARMS: "ARMS",
  GLUTES: "LEGS",
  HAMSTRINGS: "LEGS",
  LATS: "BACK",
  LOWER_BACK: "CORE",
  MIDDLE_BACK: "BACK",
  NECK: "NECK",
  QUADRICEPS: "LEGS",
  SHOULDERS: "SHOULDERS",
  TRAPS: "BACK",
  TRICEPS: "ARMS",
};

/** The exercise types the backoffice knows, in the order their filter pills are shown. */
export const EXERCISE_TYPES: NonNullable<CreateExerciseRequest["type"]>[] = [
  "STRENGTH",
  "CARDIO",
  "STRETCHING",
];

/** One theme colour per exercise type, for its pills and, in its pastel tone, its cards. */
const TYPE_COLORS: Record<string, "strength" | "cardio" | "stretching"> = {
  STRENGTH: "strength",
  CARDIO: "cardio",
  STRETCHING: "stretching",
};

/** One icon per exercise type, drawn behind the cards of that type. */
const TYPE_ICONS: Record<string, SvgIconComponent> = {
  STRENGTH: FitnessCenter,
  CARDIO: Favorite,
  STRETCHING: SelfImprovement,
};

/** The set target types that have a summary of their own; any other is summarised generically. */
const SUMMARY_TYPES = new Set(["Reps", "Duration", "MaxReps", "RepsRange", "Minutes"]);

export interface SetSummaryValues {
  count: string | number;
  type: string;
  value?: string | number | null;
  maxValue?: string | number | null;
  /** The targets are per side; `setSummary` then reads "… por lado". */
  perSide?: boolean;
}

/** One label of an exercise, as its card shows it. */
export interface ExerciseLabelValues {
  text: string;
  color: LabelColor;
  variant: "filled" | "outlined";
}

interface ExerciseLabels {
  /** The name in the active language, else the first translation, else the exercise id. */
  nameOf: (exercise: Pick<Exercise, "id" | "translations">) => string;
  /**
   * The names the catalog finder looks in, joined by a space. With English selected, only the name shown, which is the
   * English one; with any other language, the name shown and the English name, so "Sentadilla" and "Squat" both find
   * the squat.
   */
  searchTextOf: (exercise: Exercise) => string;
  /**
   * The English name to show under the name shown, or `null` when English is selected, the exercise has no English
   * name, or it is the same as the name shown.
   */
  englishNameOf: (exercise: Exercise) => string | null;
  muscleLabel: (code: string) => string;
  bodyPartLabel: (code: string) => string;
  /** An effort level in the scale of the exercise's type (RIR, RPE…); the generic scale for no known type. */
  effortLabel: (code: string, type?: string) => string;
  targetTypeLabel: (type: string) => string;
  /**
   * One set as a short text, such as "2 × 15 repeticiones" or "2 × 8 repeticiones por lado"; values are given as typed
   * or as numbers, and minutes already as `M` or `M:SS`.
   */
  setSummary: (set: SetSummaryValues) => string;
  /** One set as its count and target only, such as "2 × 15"; for lists where space is short. */
  compactSetSummary: (set: SetSummaryValues) => string;
  /** The chip colour of a body part; unknown codes are neutral. */
  bodyPartColor: (code: string) => LabelColor;
  typeLabel: (code: string) => string;
  /** The chip colour of an exercise type; unknown codes are neutral. */
  typeColor: (code: string) => LabelColor;
  /** The icon drawn behind an exercise type's cards; none for an unknown type. */
  typeIcon: (code: string) => SvgIconComponent | undefined;
  /** The `sx` colour token of that icon, the type's pastel tone; none for an unknown type. */
  typeIconColor: (code: string) => string | undefined;
  /** The type's icon in its pastel tone, ready to draw behind a card; none for an unknown type. */
  typeWatermark: (code: string) => ReactNode;
  /**
   * An exercise's body part, filled with its colour, then its muscle, outlined in the same colour. The muscle is left
   * out when `withMuscle` is false, such as for a body part whose exercises all work one muscle.
   */
  exerciseLabelsOf: (
    exercise: Pick<Exercise, "bodyPart" | "muscle">,
    withMuscle?: boolean,
  ) => ExerciseLabelValues[];
}

/** The single place that turns catalog codes into text. An unknown code is shown as it comes. */
export default function useExerciseLabels(): ExerciseLabels {
  const { t, i18n } = useTranslation("workouts");
  const language = i18n.language;

  const nameOf = useCallback(
    (exercise: Pick<Exercise, "id" | "translations">) =>
      (
        exercise.translations.find((translation) => translation.languageCode === language) ??
        exercise.translations[0]
      )?.name ?? exercise.id,
    [language],
  );

  const searchTextOf = useCallback(
    (exercise: Exercise) => {
      const shown = nameOf(exercise);
      const english =
        language === "en"
          ? undefined
          : exercise.translations.find((translation) => translation.languageCode === "en")?.name;

      return english === undefined ? shown : `${shown} ${english}`;
    },
    [language, nameOf],
  );

  const englishNameOf = useCallback(
    (exercise: Exercise) => {
      const english = exercise.translations.find(
        (translation) => translation.languageCode === "en",
      )?.name;

      return language === "en" || english === undefined || english === nameOf(exercise)
        ? null
        : english;
    },
    [language, nameOf],
  );

  return useMemo(() => {
    const labelIn = (group: string) => (code: string) =>
      t(`${group}.${code}`, { defaultValue: code });
    const bodyPartColor = (code: string) => BODY_PART_COLORS[code] ?? "default";
    const typeIcon = (code: string) =>
      Object.hasOwn(TYPE_ICONS, code) ? TYPE_ICONS[code] : undefined;
    const typeIconColor = (code: string) =>
      Object.hasOwn(TYPE_COLORS, code) ? `${TYPE_COLORS[code]}.pastel` : undefined;

    return {
      nameOf,
      searchTextOf,
      englishNameOf,
      muscleLabel: labelIn("muscles"),
      bodyPartLabel: labelIn("bodyParts"),
      effortLabel: (code: string, type?: string) =>
        type !== undefined && Object.hasOwn(TYPE_COLORS, type)
          ? t(`effortsByType.${type}.${code}`, { defaultValue: labelIn("efforts")(code) })
          : labelIn("efforts")(code),
      targetTypeLabel: labelIn("targetTypes"),
      setSummary: ({ count, type, value, maxValue, perSide }: SetSummaryValues) =>
        t(`workout.configuration.summary.${SUMMARY_TYPES.has(type) ? type : "other"}`, {
          // `series`, not `count`: i18next reserves `count` for plurals.
          series: count,
          value: value ?? "",
          max: maxValue ?? "",
        }) + (perSide ? t("workout.configuration.perSideSuffix") : ""),
      compactSetSummary: ({ count, type, value, maxValue }: SetSummaryValues) =>
        t(`workout.exercises.compactSummary.${SUMMARY_TYPES.has(type) ? type : "other"}`, {
          series: count,
          value: value ?? "",
          max: maxValue ?? "",
        }),
      bodyPartColor,
      typeLabel: labelIn("exerciseTypes"),
      typeColor: (code: string) =>
        Object.hasOwn(TYPE_COLORS, code) ? TYPE_COLORS[code]! : "default",
      typeIcon,
      typeIconColor,
      typeWatermark: (code: string) => {
        const icon = typeIcon(code);
        return icon ? createElement(icon, { sx: { color: typeIconColor(code) } }) : undefined;
      },
      exerciseLabelsOf: (exercise: Pick<Exercise, "bodyPart" | "muscle">, withMuscle = true) => {
        const color = bodyPartColor(exercise.bodyPart);
        const labels: ExerciseLabelValues[] = [
          { text: labelIn("bodyParts")(exercise.bodyPart), color, variant: "filled" },
        ];

        if (withMuscle) {
          labels.push({ text: labelIn("muscles")(exercise.muscle), color, variant: "outlined" });
        }
        return labels;
      },
    };
  }, [t, nameOf, searchTextOf, englishNameOf]);
}
