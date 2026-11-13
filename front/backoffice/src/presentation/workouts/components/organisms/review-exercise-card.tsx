"use client";

import CheckCircleOutlineOutlined from "@mui/icons-material/CheckCircleOutlineOutlined";
import ExpandMoreOutlined from "@mui/icons-material/ExpandMoreOutlined";
import PlayArrowOutlined from "@mui/icons-material/PlayArrowOutlined";
import RadioButtonUncheckedOutlined from "@mui/icons-material/RadioButtonUncheckedOutlined";
import Box from "@mui/material/Box";
import type { WorkoutByIdExercise } from "@weight-tracker/core/domain";
import { useTranslation } from "react-i18next";
import Card from "@/presentation/common/components/atoms/card";
import Chip from "@/presentation/common/components/atoms/chip";
import Collapse from "@/presentation/common/components/atoms/collapse";
import IconButton from "@/presentation/common/components/atoms/icon-button";
import Typography from "@/presentation/common/components/atoms/typography";
import ReviewSetsTable from "@/presentation/workouts/components/molecules/review-sets-table";
import useExerciseLabels from "@/presentation/workouts/hooks/use-exercise-labels";
import { exerciseStateOf, type ExerciseState } from "@/presentation/workouts/workout-review";

interface ReviewExerciseCardProps {
  /** Its place in the workout, from 1. */
  position: number;
  exercise: WorkoutByIdExercise;
  expanded: boolean;
  onToggle: () => void;
}

const STATE_COLORS = { completed: "success", inProgress: "primary", pending: "default" } as const;

function StateIcon({ state }: { state: ExerciseState }) {
  if (state === "completed") {
    return <CheckCircleOutlineOutlined color="success" aria-hidden />;
  }
  if (state === "inProgress") {
    return <PlayArrowOutlined color="primary" aria-hidden />;
  }
  return <RadioButtonUncheckedOutlined color="disabled" aria-hidden />;
}

/** One exercise of the review: its state and labels in a row, and its sets table when expanded. */
export default function ReviewExerciseCard({
  position,
  exercise,
  expanded,
  onToggle,
}: ReviewExerciseCardProps) {
  const { t } = useTranslation("workouts");
  const { nameOf, exerciseLabelsOf } = useExerciseLabels();
  const state = exerciseStateOf(exercise);
  const name = nameOf({ id: exercise.exerciseId, translations: exercise.translations });

  return (
    <Card variant="outlined">
      <Box sx={{ display: "flex", flexWrap: "wrap", alignItems: "center", gap: 1.5, p: 1.5 }}>
        <StateIcon state={state} />
        <Box
          aria-hidden
          sx={{
            display: "grid",
            placeItems: "center",
            width: 28,
            height: 28,
            borderRadius: "50%",
            bgcolor: "primary.main",
            color: "primary.contrastText",
            fontSize: "0.875rem",
            fontWeight: 700,
          }}
        >
          {position}
        </Box>
        <Typography component="h3" sx={{ fontWeight: 600, overflowWrap: "anywhere" }}>
          {name}
        </Typography>
        {exerciseLabelsOf(exercise).map(({ text, color, variant }, index) => (
          // Body part and muscle can share a text, so the position is the key.
          <Chip key={index} label={text} size="small" color={color} variant={variant} />
        ))}
        <Box sx={{ display: "flex", alignItems: "center", gap: 1, ml: "auto" }}>
          <Chip label={t(`review.exercise.${state}`)} size="small" color={STATE_COLORS[state]} />
          <IconButton
            size="small"
            aria-expanded={expanded}
            aria-label={t(expanded ? "review.collapse" : "review.expand", { name })}
            onClick={onToggle}
          >
            <ExpandMoreOutlined
              sx={{ transform: expanded ? "rotate(180deg)" : "none", transition: "transform 0.2s" }}
            />
          </IconButton>
        </Box>
      </Box>
      <Collapse in={expanded} unmountOnExit>
        <Box sx={{ px: 1.5, pb: 1.5 }}>
          <ReviewSetsTable exercise={exercise} />
        </Box>
      </Collapse>
    </Card>
  );
}
