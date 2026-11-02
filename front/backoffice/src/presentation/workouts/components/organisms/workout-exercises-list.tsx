"use client";

import { useId } from "react";
import {
  closestCenter,
  DndContext,
  type DragEndEvent,
  KeyboardSensor,
  PointerSensor,
  type UniqueIdentifier,
  useSensor,
  useSensors,
} from "@dnd-kit/core";
import {
  SortableContext,
  sortableKeyboardCoordinates,
  rectSortingStrategy,
} from "@dnd-kit/sortable";
import DeleteOutlined from "@mui/icons-material/DeleteOutlined";
import EditOutlined from "@mui/icons-material/EditOutlined";
import { minutesToText } from "@weight-tracker/core/application";
import type { DomainErrorCode, Exercise, WorkoutExerciseDetail } from "@weight-tracker/core/domain";
import { useTranslation } from "react-i18next";
import Alert from "@/presentation/common/components/atoms/alert";
import IconButton from "@/presentation/common/components/atoms/icon-button";
import List from "@/presentation/common/components/atoms/list";
import Typography from "@/presentation/common/components/atoms/typography";
import { EXERCISE_GRID_COLUMNS } from "@/presentation/workouts/components/molecules/exercise-list-item";
import SortableExerciseListItem from "@/presentation/workouts/components/molecules/sortable-exercise-list-item";
import useExerciseLabels from "@/presentation/workouts/hooks/use-exercise-labels";

/** A workout exercise with its catalog entry, when the catalog has one. */
export interface WorkoutExerciseItem {
  workoutExercise: WorkoutExerciseDetail;
  exercise?: Exercise;
}

interface WorkoutExercisesListProps {
  items: WorkoutExerciseItem[];
  /** Body parts whose catalog exercises all work one muscle: their exercises show no muscle label. */
  singleMuscleBodyParts: ReadonlySet<string>;
  /** The pencil was pressed: the exercise's configuration opens. */
  onEdit: (item: WorkoutExerciseItem) => void;
  onRemove: (item: WorkoutExerciseItem) => void;
  /** The entry whose removal is in progress. */
  removingId: string | null;
  /** Why the last removal failed, if it did. */
  removeErrorCode: DomainErrorCode | null;
  /** A saved entry was dropped onto another one's place; both are workout entry ids. */
  onReorder: (activeId: string, overId: string) => void;
  /** While a reorder is in progress no exercise can be dragged. */
  reordering: boolean;
  /** Why the last reorder failed, if it did. */
  reorderErrorCode: DomainErrorCode | null;
}

/**
 * The workout's saved exercises in their current order, numbered, as cards in rows of at most three. They are
 * reordered by dragging their handle, across rows too; the pencil opens one's configuration and the trash removes it.
 * The card around it owns the title and the add action.
 */
export default function WorkoutExercisesList({
  items,
  singleMuscleBodyParts,
  onEdit,
  onRemove,
  removingId,
  removeErrorCode,
  onReorder,
  reordering,
  reorderErrorCode,
}: WorkoutExercisesListProps) {
  const { t } = useTranslation("workouts");
  const { t: tc } = useTranslation();
  const { nameOf, effortLabel, compactSetSummary, exerciseLabelsOf, typeWatermark } =
    useExerciseLabels();
  // A stable id keeps dnd-kit's generated accessibility ids the same in the static export and in the browser.
  const dndId = useId();
  const sensors = useSensors(
    useSensor(PointerSensor),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates }),
  );
  const sortableIds = items.map((item) => item.workoutExercise.id);

  function displayNameOf({ workoutExercise, exercise }: WorkoutExerciseItem) {
    return exercise ? nameOf(exercise) : workoutExercise.exerciseId;
  }

  function nameById(id: UniqueIdentifier) {
    const item = items.find((other) => other.workoutExercise.id === id);
    return item ? displayNameOf(item) : String(id);
  }

  function positionOf(id: UniqueIdentifier) {
    return sortableIds.indexOf(String(id)) + 1;
  }

  function handleDragEnd({ active, over }: DragEndEvent) {
    if (over && active.id !== over.id) {
      onReorder(String(active.id), String(over.id));
    }
  }

  /**
   * The sets on one line, comma separated and marked when they are per side, the effort in its type's scale on the
   * next, and the rest on the last; empty properties are left out. Minutes are shown as `M` or `M:SS`.
   */
  function linesOf({ sets, effort, perSide, restMinutes }: WorkoutExerciseDetail, type?: string) {
    const lines: string[] = [];

    if (sets.length > 0) {
      const summaries = sets.map(({ count, target }) =>
        compactSetSummary({
          count,
          type: target.type,
          value:
            target.type === "Minutes" && target.minutes != null
              ? minutesToText(target.minutes)
              : target.value,
          maxValue: target.maxValue,
        }),
      );
      lines.push(summaries.join(", ") + (perSide ? t("workout.exercises.perSideSuffix") : ""));
    }
    if (effort) {
      lines.push(t("workout.exercises.effortLine", { effort: effortLabel(effort, type) }));
    }
    if (restMinutes != null) {
      lines.push(t("workout.exercises.restLine", { rest: minutesToText(restMinutes) }));
    }
    return lines;
  }

  return (
    <>
      {removeErrorCode && (
        <Alert severity="error" sx={{ mt: 1 }}>
          {tc(`errors:${removeErrorCode}`)}
        </Alert>
      )}
      {reorderErrorCode && (
        <Alert severity="error" sx={{ mt: 1 }}>
          {tc(`errors:${reorderErrorCode}`)}
        </Alert>
      )}
      {items.length === 0 ? (
        <Typography color="text.secondary" sx={{ mt: 2 }}>
          {t("workout.exercises.empty")}
        </Typography>
      ) : (
        <DndContext
          id={dndId}
          sensors={sensors}
          collisionDetection={closestCenter}
          onDragEnd={handleDragEnd}
          accessibility={{
            screenReaderInstructions: { draggable: t("workout.exercises.dnd.instructions") },
            announcements: {
              onDragStart: ({ active }) =>
                t("workout.exercises.dnd.picked", { name: nameById(active.id) }),
              onDragOver: ({ active, over }) =>
                over
                  ? t("workout.exercises.dnd.over", {
                      name: nameById(active.id),
                      position: positionOf(over.id),
                    })
                  : undefined,
              onDragEnd: ({ active, over }) =>
                over
                  ? t("workout.exercises.dnd.dropped", {
                      name: nameById(active.id),
                      position: positionOf(over.id),
                    })
                  : t("workout.exercises.dnd.cancelled", { name: nameById(active.id) }),
              onDragCancel: ({ active }) =>
                t("workout.exercises.dnd.cancelled", { name: nameById(active.id) }),
            },
          }}
        >
          <SortableContext items={sortableIds} strategy={rectSortingStrategy}>
            <List
              aria-label={t("workout.exercises.title")}
              sx={{
                mt: 1,
                display: "grid",
                // No maximum height: the card grows with the rows.
                gridTemplateColumns: EXERCISE_GRID_COLUMNS,
                gap: 2,
              }}
            >
              {items.map((item, index) => {
                const { workoutExercise, exercise } = item;

                return (
                  <SortableExerciseListItem
                    key={workoutExercise.id}
                    id={workoutExercise.id}
                    disabled={reordering}
                    handleLabel={t("workout.exercises.reorder")}
                    roleDescription={t("workout.exercises.dnd.roleDescription")}
                    position={index + 1}
                    name={displayNameOf(item)}
                    labels={
                      exercise
                        ? exerciseLabelsOf(exercise, !singleMuscleBodyParts.has(exercise.bodyPart))
                        : []
                    }
                    lines={linesOf(workoutExercise, exercise?.type)}
                    watermark={exercise ? typeWatermark(exercise.type) : undefined}
                    actions={
                      <>
                        <IconButton
                          aria-label={t("workout.exercises.remove")}
                          disabled={removingId === workoutExercise.id}
                          onClick={() => onRemove(item)}
                        >
                          <DeleteOutlined />
                        </IconButton>
                        <IconButton
                          edge="end"
                          aria-label={t("workout.exercises.edit")}
                          onClick={() => onEdit(item)}
                        >
                          <EditOutlined />
                        </IconButton>
                      </>
                    }
                  />
                );
              })}
            </List>
          </SortableContext>
        </DndContext>
      )}
    </>
  );
}
