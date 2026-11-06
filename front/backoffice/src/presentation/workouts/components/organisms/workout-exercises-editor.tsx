"use client";

import Box from "@mui/material/Box";
import BreakSuperSeriesDialog from "@/presentation/workouts/components/organisms/break-super-series-dialog";
import ExerciseCatalogDialog from "@/presentation/workouts/components/organisms/exercise-catalog-dialog";
import ExerciseConfigurationDialog from "@/presentation/workouts/components/organisms/exercise-configuration-dialog";
import WorkoutExercisesCard from "@/presentation/workouts/components/organisms/workout-exercises-card";
import WorkoutExercisesList from "@/presentation/workouts/components/organisms/workout-exercises-list";
import type { ExerciseFavoritesState } from "@/presentation/workouts/hooks/use-exercise-favorites";
import type { ExercisesEditorState } from "@/presentation/workouts/hooks/use-exercises-editor";

interface WorkoutExercisesEditorProps {
  /** The exercise list being edited, of a workout or a template; its screen owns it. */
  editor: ExercisesEditorState;
  favorites: ExerciseFavoritesState;
  currentUserId: string | null;
  titleId: string;
}

/**
 * The "Ejercicios" card with its list, and the catalog, configuration and break dialogs over it. Presentational: every
 * request and every piece of state comes from `editor`, so a workout and a template are edited the same way.
 */
export default function WorkoutExercisesEditor({
  editor,
  favorites,
  currentUserId,
  titleId,
}: WorkoutExercisesEditorProps) {
  const { dialog, removal, reordering, superSeries, breakTarget } = editor;

  return (
    <>
      <Box component="section" aria-labelledby={titleId} sx={{ minWidth: 0 }}>
        <WorkoutExercisesCard
          titleId={titleId}
          onAdd={editor.openCatalog}
          addButtonRef={editor.addButtonRef}
        >
          <WorkoutExercisesList
            items={editor.items}
            singleMuscleBodyParts={editor.singleMuscleBodyParts}
            onEdit={editor.openEdit}
            onRemove={(item) => void editor.handleRemove(item)}
            removingId={removal.removingId}
            removeErrorCode={removal.errorCode}
            onReorder={(activeId, overId) => void editor.handleReorder(activeId, overId)}
            reordering={reordering.reordering}
            reorderErrorCode={reordering.errorCode}
            selectedIds={editor.selectedIds}
            onToggleSelected={editor.handleToggleSelected}
            onGroup={() => void editor.handleGroup()}
            grouping={superSeries.pending || reordering.reordering}
            superSeriesError={superSeries.error}
            onBreak={editor.setBreakTarget}
          />
        </WorkoutExercisesCard>
      </Box>
      <ExerciseCatalogDialog
        open={dialog.kind === "catalog"}
        exercises={editor.available}
        filters={editor.filters}
        currentUserId={currentUserId}
        singleMuscleBodyParts={editor.singleMuscleBodyParts}
        favoriteIds={favorites.favoriteIds}
        favoritesStatus={favorites.status}
        pendingFavoriteIds={favorites.pendingIds}
        favoriteErrorCode={favorites.toggleErrorCode}
        onToggleFavorite={(exerciseId) => void favorites.toggle(exerciseId)}
        onChoose={editor.choose}
        onClose={editor.closeDialog}
      />
      <BreakSuperSeriesDialog
        open={breakTarget !== null}
        pending={superSeries.pending}
        onCancel={() => editor.setBreakTarget(null)}
        onConfirm={() => {
          if (breakTarget !== null) {
            void editor.handleBreak(breakTarget);
          }
        }}
      />
      {dialog.kind === "configure" && (
        // Mounted per opening and keyed by the item, so each one starts from the stored values.
        <ExerciseConfigurationDialog
          key={dialog.item.workoutExercise.id}
          open
          item={dialog.item}
          adding={dialog.adding}
          workoutId={editor.ownerId}
          owner={editor.owner}
          onSaved={(entry) => editor.handleExerciseSaved(entry, dialog.adding)}
          onClose={editor.closeDialog}
        />
      )}
    </>
  );
}
