"use client";

import { useState } from "react";
import AssignmentOutlined from "@mui/icons-material/AssignmentOutlined";
import Box from "@mui/material/Box";
import type {
  DomainErrorCode,
  WorkoutById,
  WorkoutObservations,
} from "@weight-tracker/core/domain";
import { useTranslation } from "react-i18next";
import Button from "@/presentation/common/components/atoms/button";
import Card from "@/presentation/common/components/atoms/card";
import Skeleton from "@/presentation/common/components/atoms/skeleton";
import Typography from "@/presentation/common/components/atoms/typography";
import LoadError from "@/presentation/common/components/molecules/load-error";
import ObservationItem from "@/presentation/workouts/components/molecules/observation-item";
import ReviewSectionHeader from "@/presentation/workouts/components/molecules/review-section-header";
import WorkoutObservationsDialog from "@/presentation/workouts/components/organisms/workout-observations-dialog";
import useObservationCopy from "@/presentation/workouts/hooks/use-observation-copy";
import { highlightedObservationsOf } from "@/presentation/workouts/workout-observations";

interface SessionObservationsCardProps {
  workout: Pick<WorkoutById, "exercises">;
  observations: {
    status: "idle" | "loading" | "loaded" | "failed";
    observations: WorkoutObservations | null;
    errorCode: DomainErrorCode | null;
    reload: () => void;
  };
}

/**
 * The key points of the session: the observations the API highlighted, with a way to open all of them. The API decides
 * what is observed and what is highlighted, so this only shows them.
 */
export default function SessionObservationsCard({
  workout,
  observations,
}: SessionObservationsCardProps) {
  const { t } = useTranslation("workouts");
  const { copyOf } = useObservationCopy(workout);
  const [open, setOpen] = useState(false);
  const answer = observations.status === "loaded" ? observations.observations : null;
  const highlighted = answer === null ? [] : highlightedObservationsOf(answer);

  return (
    <Card variant="outlined" sx={{ p: 2, flex: "1 1 360px", minWidth: 0 }}>
      <ReviewSectionHeader
        icon={<AssignmentOutlined />}
        title={t("review.metrics.observations.title")}
        subtitle={t("review.metrics.observations.subtitle")}
      />
      {observations.status === "failed" && observations.errorCode ? (
        // The alert brings its own page margins, which would push it out of the card.
        <Box sx={{ mx: -3, mt: -2 }}>
          <LoadError code={observations.errorCode} onRetry={observations.reload} />
        </Box>
      ) : answer === null ? (
        <Skeleton variant="rounded" height={160} aria-busy />
      ) : (
        <Box sx={{ display: "grid", gap: 1.5 }}>
          {answer.observations.length === 0 ? (
            <Typography variant="body2" color="text.secondary">
              {t("review.metrics.observations.none")}
            </Typography>
          ) : highlighted.length === 0 ? (
            <Typography variant="body2" color="text.secondary">
              {t("review.metrics.observations.noHighlights")}
            </Typography>
          ) : (
            highlighted.map((observation) => {
              const { title, text } = copyOf(observation);

              return (
                <ObservationItem
                  key={observation.id}
                  severity={observation.severity}
                  title={title}
                  text={text}
                />
              );
            })
          )}
          {answer.observations.length > 0 ? (
            <Box>
              <Button variant="text" onClick={() => setOpen(true)}>
                {t("review.metrics.observations.viewAll", { count: answer.observations.length })}
              </Button>
            </Box>
          ) : null}
        </Box>
      )}
      {open && answer !== null ? (
        <WorkoutObservationsDialog
          workout={workout}
          observations={answer}
          onClose={() => setOpen(false)}
        />
      ) : null}
    </Card>
  );
}
