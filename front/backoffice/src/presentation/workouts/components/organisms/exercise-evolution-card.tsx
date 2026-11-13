"use client";

import BarChartOutlined from "@mui/icons-material/BarChartOutlined";
import FitnessCenterOutlined from "@mui/icons-material/FitnessCenterOutlined";
import Box from "@mui/material/Box";
import type { DomainErrorCode, ExerciseHistorySession } from "@weight-tracker/core/domain";
import { useTranslation } from "react-i18next";
import BarChart from "@/presentation/common/components/atoms/bar-chart";
import Card from "@/presentation/common/components/atoms/card";
import LineChart from "@/presentation/common/components/atoms/line-chart";
import MenuItem from "@/presentation/common/components/atoms/menu-item";
import Skeleton from "@/presentation/common/components/atoms/skeleton";
import TextField from "@/presentation/common/components/atoms/text-field";
import Typography from "@/presentation/common/components/atoms/typography";
import LoadError from "@/presentation/common/components/molecules/load-error";
import ReviewSectionHeader from "@/presentation/workouts/components/molecules/review-section-header";
import { historyPointsOf } from "@/presentation/workouts/workout-review";

const CHART_HEIGHT = 280;

interface ExerciseEvolutionCardProps {
  /** `load` plots each session's max weight as a line, `volume` its volume as bars. */
  variant: "load" | "volume";
  /** The exercises the selector offers; `id` is the catalog exercise id. */
  exercises: readonly { id: string; name: string }[];
  selected: string | null;
  onSelect: (exerciseId: string) => void;
  history: {
    status: "idle" | "loading" | "loaded" | "failed";
    sessions: readonly ExerciseHistorySession[];
    errorCode: DomainErrorCode | null;
    reload: () => void;
  };
}

/** The progress of one exercise over the athlete's latest sessions, as a chart of its max weight or of its volume. */
export default function ExerciseEvolutionCard({
  variant,
  exercises,
  selected,
  onSelect,
  history,
}: ExerciseEvolutionCardProps) {
  const { t, i18n } = useTranslation("workouts");
  const points = historyPointsOf(history.sessions, variant === "load" ? "maxWeight" : "volume");
  const kg = new Intl.NumberFormat(i18n.language, {
    useGrouping: "always",
    maximumFractionDigits: 1,
  });
  const dayFormat = new Intl.DateTimeFormat(i18n.language, { day: "numeric", month: "short" });
  const chartProps = {
    labels: points.map(({ workoutDate }) => {
      // A `YYYY-MM-DD` date is read as a local one, so it does not shift a day back.
      const [year, month, day] = workoutDate.split("-").map(Number);

      return dayFormat.format(new Date(year, month - 1, day));
    }),
    values: points.map(({ value }) => value),
    valueFormatter: (value: number) => t("review.volumeValue", { value: kg.format(value) }),
    height: CHART_HEIGHT,
    ariaLabel: t(`review.metrics.${variant}.aria`),
  };

  function body() {
    if (history.status === "failed" && history.errorCode) {
      return (
        <Box sx={{ mx: -3, mb: 2 }}>
          <LoadError code={history.errorCode} onRetry={history.reload} />
        </Box>
      );
    }
    if (history.status === "loading") {
      return <Skeleton variant="rounded" height={CHART_HEIGHT} />;
    }
    if (points.length === 0) {
      return (
        <Typography color="text.secondary" sx={{ py: 6, textAlign: "center" }}>
          {t("review.metrics.history.empty")}
        </Typography>
      );
    }
    return variant === "load" ? <LineChart {...chartProps} /> : <BarChart {...chartProps} />;
  }

  return (
    <Card variant="outlined" sx={{ p: 2, flex: "1 1 360px", minWidth: 0 }}>
      <ReviewSectionHeader
        icon={variant === "load" ? <FitnessCenterOutlined /> : <BarChartOutlined />}
        title={t(`review.metrics.${variant}.title`)}
        subtitle={t(`review.metrics.${variant}.subtitle`)}
        action={
          exercises.length > 0 && (
            <TextField
              select
              size="small"
              label={t("review.metrics.evolution.exercise")}
              value={selected ?? ""}
              onChange={(event) => onSelect(event.target.value)}
              sx={{ minWidth: 180, maxWidth: "100%" }}
            >
              {exercises.map(({ id, name }) => (
                <MenuItem key={id} value={id}>
                  {name}
                </MenuItem>
              ))}
            </TextField>
          )
        }
      />
      {body()}
    </Card>
  );
}
