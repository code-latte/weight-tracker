"use client";

import { useId } from "react";
import CloseOutlined from "@mui/icons-material/CloseOutlined";
import Box from "@mui/material/Box";
import type { WorkoutById, WorkoutObservations } from "@weight-tracker/core/domain";
import { useTranslation } from "react-i18next";
import Chip from "@/presentation/common/components/atoms/chip";
import Dialog from "@/presentation/common/components/atoms/dialog";
import DialogContent from "@/presentation/common/components/atoms/dialog-content";
import DialogTitle from "@/presentation/common/components/atoms/dialog-title";
import IconButton from "@/presentation/common/components/atoms/icon-button";
import Typography from "@/presentation/common/components/atoms/typography";
import ObservationItem from "@/presentation/workouts/components/molecules/observation-item";
import useObservationCopy from "@/presentation/workouts/hooks/use-observation-copy";
import {
  highlightedIdsOf,
  observationGroupsOf,
} from "@/presentation/workouts/workout-observations";

interface WorkoutObservationsDialogProps {
  workout: Pick<WorkoutById, "exercises">;
  observations: WorkoutObservations;
  onClose: () => void;
}

/**
 * Every observation of the session, grouped by what it is about: the workout first, then each exercise. The ones the
 * API highlighted carry a mark. The caller mounts it per opening. Cerrar, Escape and a click outside close it.
 */
export default function WorkoutObservationsDialog({
  workout,
  observations,
  onClose,
}: WorkoutObservationsDialogProps) {
  const { t } = useTranslation("workouts");
  const { copyOf, exerciseNameOf } = useObservationCopy(workout);
  const titleId = useId();
  const highlighted = highlightedIdsOf(observations);

  return (
    <Dialog open onClose={onClose} fullWidth maxWidth="sm" aria-labelledby={titleId}>
      <DialogTitle
        id={titleId}
        sx={{ display: "flex", alignItems: "center", justifyContent: "space-between", gap: 1 }}
      >
        {t("review.metrics.observations.title")}
        <IconButton edge="end" aria-label={t("workout.catalog.close")} onClick={onClose}>
          <CloseOutlined />
        </IconButton>
      </DialogTitle>
      <DialogContent>
        <Box sx={{ display: "grid", gap: 3 }}>
          {observationGroupsOf(observations.observations).map((group) => (
            <Box
              key={group.exerciseId ?? "workout"}
              component="section"
              sx={{ display: "grid", gap: 1.5 }}
            >
              <Typography variant="subtitle1" component="h3" sx={{ fontWeight: 700 }}>
                {group.exerciseId === null
                  ? t("review.metrics.observations.workoutGroup")
                  : exerciseNameOf(group.exerciseId)}
              </Typography>
              {group.observations.map((observation) => {
                const { title, text } = copyOf(observation);

                return (
                  <ObservationItem
                    key={observation.id}
                    severity={observation.severity}
                    title={title}
                    text={text}
                    mark={
                      highlighted.has(observation.id) ? (
                        <Chip
                          size="small"
                          color="primary"
                          variant="outlined"
                          label={t("review.metrics.observations.highlighted")}
                        />
                      ) : undefined
                    }
                  />
                );
              })}
            </Box>
          ))}
        </Box>
      </DialogContent>
    </Dialog>
  );
}
