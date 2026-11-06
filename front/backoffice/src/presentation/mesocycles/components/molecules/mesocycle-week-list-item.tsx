"use client";

import EditOutlined from "@mui/icons-material/EditOutlined";
import Box from "@mui/material/Box";
import IconButton from "@/presentation/common/components/atoms/icon-button";
import ListItem from "@/presentation/common/components/atoms/list-item";
import Typography from "@/presentation/common/components/atoms/typography";
import TrainingWeekTypeChip from "@/presentation/mesocycles/components/molecules/training-week-type-chip";

interface MesocycleWeekListItemProps {
  /** The week type, such as "ADAPTATION". */
  type: string;
  /** "Semana 1". */
  title: string;
  objective: string;
  /** The edit action's accessible name, such as "Editar semana 1". */
  editLabel: string;
  onEdit: () => void;
}

/** One defined week as a bordered row: its type, its title, its objective and the edit action. */
export default function MesocycleWeekListItem({
  type,
  title,
  objective,
  editLabel,
  onEdit,
}: MesocycleWeekListItemProps) {
  return (
    <ListItem
      sx={{
        border: 1,
        borderColor: "divider",
        borderRadius: 1,
        gap: 2,
        alignItems: "center",
      }}
    >
      <Box sx={{ flexShrink: 0, width: 140 }}>
        <TrainingWeekTypeChip type={type} />
      </Box>
      <Typography sx={{ flexShrink: 0, width: 100, fontWeight: 600 }}>{title}</Typography>
      <Typography sx={{ flexGrow: 1, minWidth: 0, overflowWrap: "anywhere" }}>
        {objective}
      </Typography>
      <IconButton aria-label={editLabel} onClick={onEdit}>
        <EditOutlined />
      </IconButton>
    </ListItem>
  );
}
