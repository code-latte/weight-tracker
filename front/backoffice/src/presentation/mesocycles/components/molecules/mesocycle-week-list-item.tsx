"use client";

import { type ReactNode, useId, useState } from "react";
import AddOutlined from "@mui/icons-material/AddOutlined";
import EditOutlined from "@mui/icons-material/EditOutlined";
import ExpandMoreOutlined from "@mui/icons-material/ExpandMoreOutlined";
import Box from "@mui/material/Box";
import Button from "@/presentation/common/components/atoms/button";
import Collapse from "@/presentation/common/components/atoms/collapse";
import IconButton from "@/presentation/common/components/atoms/icon-button";
import ListItem from "@/presentation/common/components/atoms/list-item";
import Typography from "@/presentation/common/components/atoms/typography";
import TrainingWeekTypeChip from "@/presentation/mesocycles/components/molecules/training-week-type-chip";

/** What the row offers about the week's workouts once they are known. */
export interface WeekWorkouts {
  /** Whether the week holds any workout: only then the row expands. */
  hasEntries: boolean;
  /** "2 entrenamientos". */
  countLabel: string;
  /** The expand action's accessible name in each state, such as "Mostrar entrenamientos de la semana 1". */
  showLabel: string;
  hideLabel: string;
  /** "Nuevo entrenamiento". */
  createLabel: string;
  onCreate: () => void;
}

interface MesocycleWeekListItemProps {
  /** The week type, such as "ADAPTATION". */
  type: string;
  /** "Semana 1". */
  title: string;
  objective: string;
  /** The edit action's accessible name, such as "Editar semana 1". */
  editLabel: string;
  onEdit: () => void;
  /** The week's workouts; while they are unknown the row offers neither the count nor the create action. */
  workouts?: WeekWorkouts;
  /** What the expanded row shows below the create action: the week's workouts. */
  children?: ReactNode;
}

/**
 * One defined week as a bordered row: its type, its title, its objective and the edit action. A week with workouts
 * expands to show the create action above its workouts; a week without them shows the create action in the row.
 */
export default function MesocycleWeekListItem({
  type,
  title,
  objective,
  editLabel,
  onEdit,
  workouts,
  children,
}: MesocycleWeekListItemProps) {
  const [expanded, setExpanded] = useState(false);
  const contentId = useId();
  const hasWorkouts = workouts !== undefined && workouts.hasEntries;

  const createButton = workouts && (
    <Button
      variant="contained"
      size="small"
      startIcon={<AddOutlined />}
      onClick={workouts.onCreate}
      sx={{ flexShrink: 0 }}
    >
      {workouts.createLabel}
    </Button>
  );

  return (
    <ListItem
      sx={{
        border: 1,
        borderColor: "divider",
        borderRadius: 1,
        flexDirection: "column",
        alignItems: "stretch",
      }}
    >
      <Box sx={{ display: "flex", alignItems: "center", gap: 2 }}>
        <Box sx={{ flexShrink: 0, width: 140 }}>
          <TrainingWeekTypeChip type={type} />
        </Box>
        <Typography sx={{ flexShrink: 0, width: 100, fontWeight: 600 }}>{title}</Typography>
        <Typography sx={{ flexGrow: 1, minWidth: 0, overflowWrap: "anywhere" }}>
          {objective}
        </Typography>
        {hasWorkouts && (
          <Typography variant="body2" color="text.secondary" sx={{ flexShrink: 0 }}>
            {workouts.countLabel}
          </Typography>
        )}
        {workouts && !hasWorkouts && createButton}
        {hasWorkouts && (
          <IconButton
            aria-label={expanded ? workouts.hideLabel : workouts.showLabel}
            aria-expanded={expanded}
            // The collapsed list is unmounted, so it is only referenced while it exists.
            aria-controls={expanded ? contentId : undefined}
            onClick={() => setExpanded((current) => !current)}
          >
            <ExpandMoreOutlined
              sx={{
                transform: expanded ? "rotate(180deg)" : "none",
                transition: "transform 150ms",
              }}
            />
          </IconButton>
        )}
        <IconButton aria-label={editLabel} onClick={onEdit}>
          <EditOutlined />
        </IconButton>
      </Box>
      {hasWorkouts && (
        <Collapse in={expanded} unmountOnExit id={contentId}>
          <Box
            sx={{
              display: "flex",
              flexDirection: "column",
              alignItems: "flex-start",
              gap: 1,
              pt: 1,
            }}
          >
            {createButton}
            <Box sx={{ alignSelf: "stretch" }}>{children}</Box>
          </Box>
        </Collapse>
      )}
    </ListItem>
  );
}
