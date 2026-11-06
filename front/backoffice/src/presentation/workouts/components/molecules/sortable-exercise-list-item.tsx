"use client";

import type { ComponentProps } from "react";
import { useSortable } from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import DragIndicator from "@mui/icons-material/DragIndicator";
import Checkbox from "@/presentation/common/components/atoms/checkbox";
import IconButton from "@/presentation/common/components/atoms/icon-button";
import ExerciseListItem from "@/presentation/workouts/components/molecules/exercise-list-item";

type SortableExerciseListItemProps = Omit<
  ComponentProps<typeof ExerciseListItem>,
  "leading" | "ref" | "style"
> & {
  /** The sortable id, the same one the surrounding sortable list holds. */
  id: string;
  /** A disabled item keeps its place: its handle is disabled and it is not a drop target. */
  disabled?: boolean;
  handleLabel: string;
  /** Read by assistive technology in place of "button" while the handle is focused. */
  roleDescription: string;
  /** A checkbox shown after the handle, such as the one that picks exercises to group. */
  selection?: { checked: boolean; label: string; onChange: () => void };
};

/**
 * An exercise that moves within a sortable list when dragged by the handle on its left, optionally followed by a
 * checkbox.
 */
export default function SortableExerciseListItem({
  id,
  disabled = false,
  handleLabel,
  roleDescription,
  selection,
  ...itemProps
}: SortableExerciseListItemProps) {
  const {
    attributes,
    listeners,
    setNodeRef,
    setActivatorNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({ id, disabled, attributes: { roleDescription } });

  return (
    <ExerciseListItem
      {...itemProps}
      ref={setNodeRef}
      style={{
        transform: CSS.Transform.toString(transform),
        transition,
        position: "relative",
        zIndex: isDragging ? 1 : undefined,
      }}
      leading={
        <>
          <IconButton
            ref={setActivatorNodeRef}
            {...attributes}
            {...listeners}
            aria-label={handleLabel}
            disabled={disabled}
            size="small"
            edge="start"
            sx={{ cursor: isDragging ? "grabbing" : "grab", touchAction: "none" }}
          >
            <DragIndicator fontSize="small" />
          </IconButton>
          {selection && (
            <Checkbox
              size="small"
              checked={selection.checked}
              onChange={selection.onChange}
              slotProps={{ input: { "aria-label": selection.label } }}
              sx={{ p: 0.5 }}
            />
          )}
        </>
      }
    />
  );
}
