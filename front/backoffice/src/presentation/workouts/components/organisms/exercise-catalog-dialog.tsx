"use client";

import { useId } from "react";
import CloseOutlined from "@mui/icons-material/CloseOutlined";
import type { DomainErrorCode, Exercise } from "@weight-tracker/core/domain";
import { useTranslation } from "react-i18next";
import Dialog from "@/presentation/common/components/atoms/dialog";
import DialogContent from "@/presentation/common/components/atoms/dialog-content";
import DialogTitle from "@/presentation/common/components/atoms/dialog-title";
import IconButton from "@/presentation/common/components/atoms/icon-button";
import ExerciseCatalogBrowser from "@/presentation/workouts/components/organisms/exercise-catalog-browser";
import type { ExerciseCatalogFilters } from "@/presentation/workouts/hooks/use-exercise-catalog-filters";

interface ExerciseCatalogDialogProps {
  open: boolean;
  /** The catalog exercises that are not in the workout yet. */
  exercises: Exercise[];
  filters: ExerciseCatalogFilters;
  /** The signed-in user, whose own exercises "Mis ejercicios" lists. */
  currentUserId: string | null;
  /** Body parts whose catalog exercises all work one muscle: their exercises show no muscle label. */
  singleMuscleBodyParts: ReadonlySet<string>;
  /** The ids of the user's favorite exercises, and whether they have been read yet. */
  favoriteIds: ReadonlySet<string>;
  favoritesStatus: "loading" | "loaded" | "failed";
  /** The exercises whose star is waiting for the API. */
  pendingFavoriteIds: ReadonlySet<string>;
  /** Why the last star was refused, if it was. */
  favoriteErrorCode: DomainErrorCode | null;
  onToggleFavorite: (exerciseId: string) => void;
  onChoose: (exercise: Exercise) => void;
  onClose: () => void;
}

/**
 * The catalog browser in a dialog that keeps one height, whatever the filters leave. The plus chooses an exercise to
 * configure; Cerrar, Escape and a click outside close it without choosing. The screen keeps the section and the
 * filters, so they survive a reopening.
 */
export default function ExerciseCatalogDialog({
  open,
  exercises,
  filters,
  currentUserId,
  singleMuscleBodyParts,
  favoriteIds,
  favoritesStatus,
  pendingFavoriteIds,
  favoriteErrorCode,
  onToggleFavorite,
  onChoose,
  onClose,
}: ExerciseCatalogDialogProps) {
  const { t } = useTranslation("workouts");
  const titleId = useId();

  return (
    <Dialog
      open={open}
      onClose={onClose}
      fullWidth
      maxWidth={false}
      aria-labelledby={titleId}
      slotProps={{ paper: { sx: { width: { md: "90vw" }, height: "min(85vh, 860px)" } } }}
    >
      <DialogTitle
        id={titleId}
        sx={{ display: "flex", alignItems: "center", justifyContent: "space-between", gap: 1 }}
      >
        {t("workout.catalog.title")}
        <IconButton edge="end" aria-label={t("workout.catalog.close")} onClick={onClose}>
          <CloseOutlined />
        </IconButton>
      </DialogTitle>
      <DialogContent sx={{ display: "flex", flexDirection: "column" }}>
        <ExerciseCatalogBrowser
          contained
          exercises={exercises}
          filters={filters}
          currentUserId={currentUserId}
          singleMuscleBodyParts={singleMuscleBodyParts}
          favoriteIds={favoriteIds}
          favoritesStatus={favoritesStatus}
          pendingFavoriteIds={pendingFavoriteIds}
          favoriteErrorCode={favoriteErrorCode}
          onToggleFavorite={onToggleFavorite}
          onChoose={onChoose}
        />
      </DialogContent>
    </Dialog>
  );
}
