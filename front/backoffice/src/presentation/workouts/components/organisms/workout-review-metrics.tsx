"use client";

import Box from "@mui/material/Box";
import type { DomainErrorCode, WorkoutById, WorkoutSummary } from "@weight-tracker/core/domain";
import Skeleton from "@/presentation/common/components/atoms/skeleton";
import LoadError from "@/presentation/common/components/molecules/load-error";
import ExerciseComparisonCard from "@/presentation/workouts/components/organisms/exercise-comparison-card";
import ExerciseEvolutionCard from "@/presentation/workouts/components/organisms/exercise-evolution-card";
import MuscleDistributionCard from "@/presentation/workouts/components/organisms/muscle-distribution-card";
import SessionObservationsCard from "@/presentation/workouts/components/organisms/session-observations-card";
import SessionResultsCard from "@/presentation/workouts/components/organisms/session-results-card";
import useExerciseLabels from "@/presentation/workouts/hooks/use-exercise-labels";
import { muscleDistributionOf, weightedExercisesOf } from "@/presentation/workouts/workout-review";

type EvolutionHistory = Parameters<typeof ExerciseEvolutionCard>[0]["history"];

interface WorkoutReviewMetricsProps {
  workout: WorkoutById;
  /** The session's duration, already formatted as the review's elapsed time is, or "—". */
  elapsed: string;
  summary: {
    status: "idle" | "loading" | "loaded" | "failed";
    summary: WorkoutSummary | null;
    errorCode: DomainErrorCode | null;
    reload: () => void;
  };
  history: EvolutionHistory;
  /** The catalog id of the exercise the evolution charts show. */
  selectedExerciseId: string | null;
  onSelectExercise: (exerciseId: string) => void;
}

/** The "Resumen y métricas" tab: the session's results, the exercises' evolution and comparison, muscles and notes. */
export default function WorkoutReviewMetrics({
  workout,
  elapsed,
  summary,
  history,
  selectedExerciseId,
  onSelectExercise,
}: WorkoutReviewMetricsProps) {
  const { nameOf } = useExerciseLabels();

  if (summary.status === "failed" && summary.errorCode) {
    return (
      <Box sx={{ mx: -3, mt: -2 }}>
        <LoadError code={summary.errorCode} onRetry={summary.reload} />
      </Box>
    );
  }
  if (summary.status !== "loaded" || summary.summary === null) {
    return (
      <Box sx={{ display: "grid", gap: 3 }} aria-busy>
        <Skeleton variant="rounded" height={170} />
        <Skeleton variant="rounded" height={360} />
        <Skeleton variant="rounded" height={320} />
      </Box>
    );
  }

  const weighted = weightedExercisesOf(summary.summary, workout);
  const exercises = weighted.map(({ exercise }) => ({
    id: exercise.exerciseId,
    name: nameOf({ id: exercise.exerciseId, translations: exercise.translations }),
  }));

  return (
    <Box sx={{ display: "grid", gap: 3 }}>
      <SessionResultsCard workout={workout} summary={summary.summary} elapsed={elapsed} />
      <Box sx={{ display: "flex", flexWrap: "wrap", gap: 3 }}>
        {(["load", "volume"] as const).map((variant) => (
          <ExerciseEvolutionCard
            key={variant}
            variant={variant}
            exercises={exercises}
            selected={selectedExerciseId}
            onSelect={onSelectExercise}
            history={history}
          />
        ))}
      </Box>
      <ExerciseComparisonCard weighted={weighted} />
      <Box sx={{ display: "flex", flexWrap: "wrap", gap: 3 }}>
        <MuscleDistributionCard distribution={muscleDistributionOf(workout)} />
        <SessionObservationsCard />
      </Box>
    </Box>
  );
}
