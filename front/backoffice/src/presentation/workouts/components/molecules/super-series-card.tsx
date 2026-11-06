"use client";

import type { ReactNode } from "react";
import { useSortable } from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import Box from "@mui/material/Box";
import DragIndicator from "@mui/icons-material/DragIndicator";
import LinkIcon from "@mui/icons-material/Link";
import Card from "@/presentation/common/components/atoms/card";
import IconButton from "@/presentation/common/components/atoms/icon-button";
import List from "@/presentation/common/components/atoms/list";
import ListItem from "@/presentation/common/components/atoms/list-item";
import Typography from "@/presentation/common/components/atoms/typography";
import { EXERCISE_GRID_COLUMNS } from "@/presentation/workouts/components/molecules/exercise-list-item";

interface SuperSeriesCardProps {
  /** The sortable id, the same one the surrounding sortable list holds. */
  id: string;
  /** A disabled card keeps its place: its handle is disabled and it is not a drop target. */
  disabled?: boolean;
  handleLabel: string;
  /** Read by assistive technology in place of "button" while the handle is focused. */
  roleDescription: string;
  /** Shown beside the icons, at the top of the card. */
  title: string;
  /** Names the inner list of members. */
  label: string;
  breakLabel: string;
  /** The link icon was pressed. */
  onBreak: () => void;
  /** The members' cards. */
  children: ReactNode;
}

/**
 * The members of a superseries inside one card that takes a full row of the list. The link icon in its top-left
 * corner asks to break it; the handle beside it drags the whole group, and the title follows them.
 */
export default function SuperSeriesCard({
  id,
  disabled = false,
  handleLabel,
  roleDescription,
  title,
  label,
  breakLabel,
  onBreak,
  children,
}: SuperSeriesCardProps) {
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
    <ListItem
      ref={setNodeRef}
      style={{
        transform: CSS.Transform.toString(transform),
        transition,
        position: "relative",
        zIndex: isDragging ? 1 : undefined,
      }}
      disablePadding
      sx={{ display: "block", gridColumn: "1 / -1" }}
    >
      <Card variant="outlined" sx={{ p: 1.5, bgcolor: "action.hover" }}>
        <Box sx={{ display: "flex", alignItems: "center", gap: 0.5 }}>
          <IconButton aria-label={breakLabel} onClick={onBreak} size="small" edge="start">
            <LinkIcon fontSize="small" />
          </IconButton>
          <IconButton
            ref={setActivatorNodeRef}
            {...attributes}
            {...listeners}
            aria-label={handleLabel}
            disabled={disabled}
            size="small"
            sx={{ cursor: isDragging ? "grabbing" : "grab", touchAction: "none" }}
          >
            <DragIndicator fontSize="small" />
          </IconButton>
          <Typography variant="subtitle2" component="span" sx={{ ml: 0.5 }}>
            {title}
          </Typography>
        </Box>
        <List
          aria-label={label}
          sx={{
            mt: 1,
            p: 0,
            display: "grid",
            gridTemplateColumns: EXERCISE_GRID_COLUMNS,
            gap: 2,
          }}
        >
          {children}
        </List>
      </Card>
    </ListItem>
  );
}
