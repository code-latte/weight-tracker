"use client";

import { useId } from "react";
import Box from "@mui/material/Box";
import Chip from "@/presentation/common/components/atoms/chip";
import Typography from "@/presentation/common/components/atoms/typography";
import type { AssignmentFilterValue } from "@/presentation/trainer-assignments/filter-trainer-assignments";

export interface AssignmentFilterOption {
  value: AssignmentFilterValue;
  label: string;
}

interface AssignmentFilterProps {
  /** Shown above the pills, and names their group. */
  label: string;
  options: AssignmentFilterOption[];
  selected: AssignmentFilterValue | null;
  onToggle: (value: AssignmentFilterValue) => void;
}

/**
 * A small label, then one pill per assignment state: outlined while unselected, filled while selected. Pills that do
 * not fit wrap onto the next line; the group never scrolls sideways.
 */
export default function AssignmentFilter({
  label,
  options,
  selected,
  onToggle,
}: AssignmentFilterProps) {
  const labelId = useId();

  return (
    <Box role="group" aria-labelledby={labelId}>
      <Typography
        id={labelId}
        variant="caption"
        component="p"
        color="text.secondary"
        sx={{ mb: 0.5, fontWeight: 600 }}
      >
        {label}
      </Typography>
      <Box sx={{ display: "flex", flexWrap: "wrap", gap: 1 }}>
        {options.map(({ value, label: text }) => (
          <Chip
            key={value}
            label={text}
            clickable
            color="primary"
            variant={selected === value ? "filled" : "outlined"}
            aria-pressed={selected === value}
            onClick={() => onToggle(value)}
          />
        ))}
      </Box>
    </Box>
  );
}
