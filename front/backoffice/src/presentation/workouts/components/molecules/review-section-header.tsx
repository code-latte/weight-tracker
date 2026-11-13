"use client";

import Box from "@mui/material/Box";
import type { ReactNode } from "react";
import Typography from "@/presentation/common/components/atoms/typography";

interface ReviewSectionHeaderProps {
  icon: ReactNode;
  title: string;
  subtitle: string;
  /** Something at the end of the title line, such as a mark or a selector. */
  action?: ReactNode;
}

/** The head of a card of the metrics tab: its icon, its title and subtitle, and an optional action. */
export default function ReviewSectionHeader({
  icon,
  title,
  subtitle,
  action,
}: ReviewSectionHeaderProps) {
  return (
    <Box sx={{ display: "flex", alignItems: "flex-start", gap: 1.5, flexWrap: "wrap", mb: 2 }}>
      <Box sx={{ display: "flex", color: "primary.main", pt: 0.25 }}>{icon}</Box>
      <Box sx={{ flex: "1 1 200px", minWidth: 0 }}>
        <Typography variant="h6" component="h2">
          {title}
        </Typography>
        <Typography variant="body2" color="text.secondary">
          {subtitle}
        </Typography>
      </Box>
      {action}
    </Box>
  );
}
