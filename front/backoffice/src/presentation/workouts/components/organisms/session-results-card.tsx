"use client";

import BarChartOutlined from "@mui/icons-material/BarChartOutlined";
import CheckCircleOutlineOutlined from "@mui/icons-material/CheckCircleOutlineOutlined";
import FitnessCenterOutlined from "@mui/icons-material/FitnessCenterOutlined";
import GpsFixedOutlined from "@mui/icons-material/GpsFixedOutlined";
import LayersOutlined from "@mui/icons-material/LayersOutlined";
import TimerOutlined from "@mui/icons-material/TimerOutlined";
import Box from "@mui/material/Box";
import type { WorkoutById, WorkoutSummary } from "@weight-tracker/core/domain";
import { useTranslation } from "react-i18next";
import Card from "@/presentation/common/components/atoms/card";
import ReviewSectionHeader from "@/presentation/workouts/components/molecules/review-section-header";
import ReviewStatCard from "@/presentation/workouts/components/molecules/review-stat-card";
import { percentOf, reviewSummaryOf } from "@/presentation/workouts/workout-review";

interface SessionResultsCardProps {
  workout: WorkoutById;
  summary: WorkoutSummary;
  /** The session's duration, already formatted as the review's elapsed time is, or "—". */
  elapsed: string;
}

/** How the session went: the sets and exercises done, the sets in target, the strength volume and the duration. */
export default function SessionResultsCard({ workout, summary, elapsed }: SessionResultsCardProps) {
  const { t, i18n } = useTranslation("workouts");
  const review = reviewSummaryOf(workout);
  const notAvailable = t("review.notAvailable");
  const { inTarget, evaluated } = summary.setsInTarget;

  function percentText(percent: number | null): string | undefined {
    return percent === null ? undefined : t("review.metrics.percentValue", { value: percent });
  }
  function percentColor(percent: number | null): string {
    return percent === 100 ? "success.main" : "text.secondary";
  }
  const setsPercent = percentOf(review.counted, review.planned);
  const exercisesPercent = percentOf(review.completedExercises, review.totalExercises);
  const targetPercent = percentOf(inTarget, evaluated);
  const volume =
    summary.volume == null
      ? notAvailable
      : t("review.volumeValue", {
          value: new Intl.NumberFormat(i18n.language, {
            useGrouping: "always",
            maximumFractionDigits: 1,
          }).format(Number(summary.volume)),
        });

  return (
    <Card variant="outlined" sx={{ p: 2 }}>
      <ReviewSectionHeader
        icon={<BarChartOutlined />}
        title={t("review.metrics.results.title")}
        subtitle={t("review.metrics.results.subtitle")}
      />
      <Box sx={{ display: "flex", flexWrap: "wrap", gap: 2, "& > *": { flexBasis: "150px" } }}>
        <ReviewStatCard
          icon={<CheckCircleOutlineOutlined />}
          label={t("review.metrics.results.setsDone")}
          value={`${review.counted} / ${review.planned}`}
          caption={percentText(setsPercent)}
          captionColor={percentColor(setsPercent)}
        />
        <ReviewStatCard
          icon={<FitnessCenterOutlined />}
          label={t("review.metrics.results.exercisesDone")}
          value={`${review.completedExercises} / ${review.totalExercises}`}
          caption={percentText(exercisesPercent)}
          captionColor={percentColor(exercisesPercent)}
        />
        <ReviewStatCard
          icon={<GpsFixedOutlined />}
          label={t("review.metrics.results.setsInTarget")}
          value={targetPercent === null ? notAvailable : `${inTarget} / ${evaluated}`}
          caption={percentText(targetPercent)}
          captionColor={percentColor(targetPercent)}
        />
        <ReviewStatCard
          icon={<LayersOutlined />}
          label={t("review.metrics.results.volume")}
          value={volume}
        />
        <ReviewStatCard
          icon={<TimerOutlined />}
          label={t("review.metrics.results.duration")}
          value={elapsed}
        />
      </Box>
    </Card>
  );
}
