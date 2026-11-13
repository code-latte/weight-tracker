"use client";

import CheckCircleOutlineOutlined from "@mui/icons-material/CheckCircleOutlineOutlined";
import FitnessCenterOutlined from "@mui/icons-material/FitnessCenterOutlined";
import TimerOutlined from "@mui/icons-material/TimerOutlined";
import TrendingUpOutlined from "@mui/icons-material/TrendingUpOutlined";
import Box from "@mui/material/Box";
import type { WorkoutById } from "@weight-tracker/core/domain";
import NextLink from "next/link";
import { useState } from "react";
import { useTranslation } from "react-i18next";
import Alert from "@/presentation/common/components/atoms/alert";
import Button from "@/presentation/common/components/atoms/button";
import useCurrentUser from "@/presentation/common/hooks/use-current-user";
import ReviewStatCard from "@/presentation/workouts/components/molecules/review-stat-card";
import SessionProgressCard from "@/presentation/workouts/components/molecules/session-progress-card";
import ReviewExerciseCard from "@/presentation/workouts/components/organisms/review-exercise-card";
import WorkoutReviewHeader from "@/presentation/workouts/components/organisms/workout-review-header";
import WorkoutReviewPersonStrip from "@/presentation/workouts/components/organisms/workout-review-person-strip";
import WorkoutReviewTabs, {
  type ReviewTab,
} from "@/presentation/workouts/components/organisms/workout-review-tabs";
import useSaveWorkoutAsTemplate from "@/presentation/workouts/hooks/use-save-workout-as-template";
import { backHrefOf, personCardOf } from "@/presentation/workouts/workout-context";
import { initiallyExpandedOf, reviewSummaryOf } from "@/presentation/workouts/workout-review";

/** The read-only review of a started, completed or reviewed workout: the session's figures and its exercises. */
export default function WorkoutReview({ workout }: { workout: WorkoutById }) {
  const { t } = useTranslation("workouts");
  const { t: tc, i18n } = useTranslation();
  const { profile } = useCurrentUser();
  const asTemplate = useSaveWorkoutAsTemplate();
  const [tab, setTab] = useState<ReviewTab>("exercises");
  // The ids of the expanded cards: the one the athlete is on, or the next to do, until the trainer changes it.
  const [expanded, setExpanded] = useState<ReadonlySet<string>>(() => {
    const first = initiallyExpandedOf(workout.exercises);
    return new Set(first === null ? [] : [first]);
  });
  const summary = reviewSummaryOf(workout);
  const notAvailable = t("review.notAvailable");
  const subtitle = [
    workout.name,
    workout.mesocycle?.name,
    workout.mesocycle
      ? t("workout.context.week", { number: Number(workout.mesocycleWeekNumber) })
      : null,
  ]
    .filter(Boolean)
    .join(" · ");
  const volume = new Intl.NumberFormat(i18n.language, {
    useGrouping: "always",
    maximumFractionDigits: 1,
  }).format(summary.volumeKg);

  function toggle(id: string) {
    setExpanded((current) => {
      const next = new Set(current);
      if (!next.delete(id)) {
        next.add(id);
      }
      return next;
    });
  }

  return (
    <>
      <WorkoutReviewHeader
        backHref={backHrefOf(workout)}
        subtitle={subtitle}
        status={workout.status}
        savingTemplate={asTemplate.saving}
        onSaveAsTemplate={() => void asTemplate.save(workout.id)}
      />
      {asTemplate.errorCode && (
        <Alert severity="error" sx={{ mb: 3 }}>
          {tc(`errors:${asTemplate.errorCode}`)}
        </Alert>
      )}
      {asTemplate.createdId && (
        <Alert
          severity="success"
          sx={{ mb: 3 }}
          action={
            <Button
              component={NextLink}
              href={`/workout-template?id=${encodeURIComponent(asTemplate.createdId)}`}
              color="inherit"
              size="small"
            >
              {t("workout.viewTemplate")}
            </Button>
          }
        >
          {t("workout.savedAsTemplate")}
        </Alert>
      )}
      <WorkoutReviewPersonStrip
        athlete={personCardOf(workout.athlete, tc("person.role.athlete"))}
        // Only an admin is shown who prepares the workout.
        trainer={
          profile?.role === "ADMIN" && workout.trainer
            ? personCardOf(workout.trainer, tc("person.role.trainer"))
            : null
        }
        resultsUpdatedAt={workout.resultsUpdatedAt}
      />
      <Box sx={{ display: "flex", flexWrap: "wrap", gap: 2, mb: 3 }}>
        <ReviewStatCard
          icon={<TimerOutlined />}
          label={t("review.stats.elapsed")}
          value={notAvailable}
        />
        <ReviewStatCard
          icon={<CheckCircleOutlineOutlined />}
          label={t("review.stats.setsDone")}
          value={`${summary.counted} / ${summary.planned}`}
        />
        <ReviewStatCard
          icon={<FitnessCenterOutlined />}
          label={t("review.stats.exercisesDone")}
          value={`${summary.completedExercises} / ${summary.totalExercises}`}
        />
        <ReviewStatCard
          icon={<TrendingUpOutlined />}
          label={t("review.stats.volume")}
          value={t("review.volumeValue", { value: volume })}
        />
      </Box>
      <Box sx={{ mb: 3 }}>
        <SessionProgressCard
          percent={summary.percent}
          done={summary.counted}
          pending={summary.pending}
        />
      </Box>
      <WorkoutReviewTabs value={tab} onChange={setTab}>
        <Box component="ul" sx={{ display: "grid", gap: 1.5, listStyle: "none", m: 0, p: 0 }}>
          {workout.exercises.map((exercise, index) => (
            <li key={exercise.id}>
              <ReviewExerciseCard
                position={index + 1}
                exercise={exercise}
                expanded={expanded.has(exercise.id)}
                onToggle={() => toggle(exercise.id)}
              />
            </li>
          ))}
        </Box>
      </WorkoutReviewTabs>
    </>
  );
}
