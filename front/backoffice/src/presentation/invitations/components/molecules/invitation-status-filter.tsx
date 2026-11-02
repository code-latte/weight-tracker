"use client";

import { useId } from "react";
import Box from "@mui/material/Box";
import Chip from "@/presentation/common/components/atoms/chip";
import Typography from "@/presentation/common/components/atoms/typography";

export interface InvitationStatusFilterOption {
  code: string;
  label: string;
}

interface InvitationStatusFilterProps {
  /** Shown above the pills, and names their group. */
  label: string;
  options: InvitationStatusFilterOption[];
  selected: string | null;
  onToggle: (code: string) => void;
}

/**
 * A small label, then one pill per status: outlined while unselected, filled while selected. Pills that do not fit wrap
 * onto the next line; the group never scrolls sideways.
 */
export default function InvitationStatusFilter({
  label,
  options,
  selected,
  onToggle,
}: InvitationStatusFilterProps) {
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
        {options.map(({ code, label: text }) => (
          <Chip
            key={code}
            label={text}
            clickable
            color="primary"
            variant={selected === code ? "filled" : "outlined"}
            aria-pressed={selected === code}
            onClick={() => onToggle(code)}
          />
        ))}
      </Box>
    </Box>
  );
}
