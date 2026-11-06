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
import LinkIcon from "@mui/icons-material/Link";
import { minutesToText } from "@weight-tracker/core/application";
import type {
  DomainErrorCode,
  Exercise,
  SuperSeriesId,
  WorkoutExerciseDetail,
} from "@weight-tracker/core/domain";
import { useTranslation } from "react-i18next";
import Alert from "@/presentation/common/components/atoms/alert";
import Button from "@/presentation/common/components/atoms/button";
import IconButton from "@/presentation/common/components/atoms/icon-button";
import List from "@/presentation/common/components/atoms/list";
import Typography from "@/presentation/common/components/atoms/typography";
import ExerciseListItem, {
  EXERCISE_GRID_COLUMNS,
} from "@/presentation/workouts/components/molecules/exercise-list-item";
import SortableExerciseListItem from "@/presentation/workouts/components/molecules/sortable-exercise-list-item";
import SuperSeriesCard from "@/presentation/workouts/components/molecules/super-series-card";
import useExerciseLabels from "@/presentation/workouts/hooks/use-exercise-labels";
import type { SuperSeriesError } from "@/presentation/workouts/hooks/use-super-series";

/** A workout exercise with its catalog entry, when the catalog has one. */
export interface WorkoutExerciseItem {
  workoutExercise: WorkoutExerciseDetail;
  exercise?: Exercise;
}

/**
 * What moves as one when dragged: a single exercise, or a run of consecutive exercises of the same superseries.
 * `start` and `length` locate the unit in the list it was built from.
 */
export interface ExerciseUnit {
  /** The entry id for a single exercise; `group:` and the first member's entry id for a group. */
  id: string;
  start: number;
  length: number;
  /** Set only on a group. */
  superSeriesId: SuperSeriesId | null;
}

/**
 * Splits the entries into units. Consecutive entries sharing a superseries form a group, even a group of one when its
 * other members are elsewhere in the list; an entry no other entry shares its superseries with is a single exercise.
 */
export function unitsOf(entries: Pick<WorkoutExerciseDetail, "id" | "superSeriesId">[]) {
  const members = new Map<string, number>();
  for (const { superSeriesId } of entries) {
    if (superSeriesId != null) {
      const key = String(superSeriesId);
      members.set(key, (members.get(key) ?? 0) + 1);
    }
  }
  const units: ExerciseUnit[] = [];
  let start = 0;

  while (start < entries.length) {
    const { id, superSeriesId } = entries[start];
    let length = 1;

    while (
      superSeriesId != null &&
      start + length < entries.length &&
      String(entries[start + length].superSeriesId) === String(superSeriesId)
    ) {
      length += 1;
    }
    units.push(
      superSeriesId != null && (members.get(String(superSeriesId)) ?? 0) > 1
        ? { id: `group:${id}`, start, length, superSeriesId }
        : { id, start, length, superSeriesId: null },
    );
    start += length;
  }
  return units;
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
  /** A unit was dropped onto another one's place; both are `ExerciseUnit` ids. */
  onReorder: (activeId: string, overId: string) => void;
  /** While a reorder is in progress no exercise can be dragged. */
  reordering: boolean;
  /** Why the last reorder failed, if it did. */
  reorderErrorCode: DomainErrorCode | null;
  /** The entries checked to be grouped. */
  selectedIds: ReadonlySet<string>;
  onToggleSelected: (id: string) => void;
  /** Agrupar en superserie was pressed. */
  onGroup: () => void;
  /** While a grouping is in progress Agrupar en superserie is disabled. */
  grouping: boolean;
  /** Why the last grouping or break failed, if it did. */
  superSeriesError: SuperSeriesError | null;
  /** A group's link icon was pressed. */
  onBreak: (superSeriesId: SuperSeriesId) => void;
}

/**
 * The workout's saved exercises in their current order, numbered, as cards in rows of at most three. They are
 * reordered by dragging their handle, across rows too; the pencil opens one's configuration and the trash removes it.
 * The members of a superseries sit together inside a group card that is dragged as one; the other exercises carry a
 * checkbox to group them. The card around it owns the title and the add action.
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
  selectedIds,
  onToggleSelected,
  onGroup,
  grouping,
  superSeriesError,
  onBreak,
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
  const units = unitsOf(items.map((item) => item.workoutExercise));
  const sortableIds = units.map((unit) => unit.id);

  function displayNameOf({ workoutExercise, exercise }: WorkoutExerciseItem) {
    return exercise ? nameOf(exercise) : workoutExercise.exerciseId;
  }

  function membersOf({ start, length }: ExerciseUnit) {
    return items.slice(start, start + length);
  }

  function groupNameOf(unit: ExerciseUnit) {
    return t("workout.exercises.dnd.groupName", {
      names: membersOf(unit).map(displayNameOf).join(", "),
    });
  }

  function nameById(id: UniqueIdentifier) {
    const unit = units.find((other) => other.id === id);
    if (!unit) {
      return String(id);
    }
    return unit.superSeriesId == null ? displayNameOf(items[unit.start]) : groupNameOf(unit);
  }

  /** A unit's position is its first exercise's. */
  function positionOf(id: UniqueIdentifier) {
    const unit = units.find((other) => other.id === id);
    return unit ? unit.start + 1 : 0;
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

  /** What every exercise card shows, inside a group card or not. */
  function itemPropsOf(item: WorkoutExerciseItem, index: number) {
    const { workoutExercise, exercise } = item;

    return {
      position: index + 1,
      name: displayNameOf(item),
      labels: exercise
        ? exerciseLabelsOf(exercise, !singleMuscleBodyParts.has(exercise.bodyPart))
        : [],
      lines: linesOf(workoutExercise, exercise?.type),
      watermark: exercise ? typeWatermark(exercise.type) : undefined,
      actions: (
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
      ),
    };
  }

  return (
    <>
      {selectedIds.size >= 2 && (
        <Button
          variant="outlined"
          startIcon={<LinkIcon />}
          disabled={grouping}
          onClick={onGroup}
          sx={{ mt: 1 }}
        >
          {t("workout.superSeries.group")}
        </Button>
      )}
      {superSeriesError && (
        <Alert severity="error" sx={{ mt: 1 }}>
          {superSeriesError.action === "create"
            ? // A grouping can explain its failure better than the generic message, as with the set totals.
              t(`workout.superSeries.errors.${superSeriesError.code}`, {
                defaultValue: tc(`errors:${superSeriesError.code}`),
              })
            : tc(`errors:${superSeriesError.code}`)}
        </Alert>
      )}
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
              {units.map((unit) => {
                if (unit.superSeriesId == null) {
                  const item = items[unit.start];
                  const id = item.workoutExercise.id;

                  return (
                    <SortableExerciseListItem
                      key={unit.id}
                      id={unit.id}
                      disabled={reordering}
                      handleLabel={t("workout.exercises.reorder")}
                      roleDescription={t("workout.exercises.dnd.roleDescription")}
                      selection={{
                        checked: selectedIds.has(id),
                        label: t("workout.superSeries.select"),
                        onChange: () => onToggleSelected(id),
                      }}
                      {...itemPropsOf(item, unit.start)}
                    />
                  );
                }

                const superSeriesId = unit.superSeriesId;

                return (
                  <SuperSeriesCard
                    key={unit.id}
                    id={unit.id}
                    disabled={reordering}
                    handleLabel={t("workout.superSeries.reorder")}
                    roleDescription={t("workout.exercises.dnd.roleDescription")}
                    title={t("workout.superSeries.title")}
                    label={groupNameOf(unit)}
                    breakLabel={t("workout.superSeries.break")}
                    onBreak={() => onBreak(superSeriesId)}
                  >
                    {membersOf(unit).map((item, offset) => (
                      <ExerciseListItem
                        key={item.workoutExercise.id}
                        {...itemPropsOf(item, unit.start + offset)}
                      />
                    ))}
                  </SuperSeriesCard>
                );
              })}
            </List>
          </SortableContext>
        </DndContext>
      )}
    </>
  );
}
