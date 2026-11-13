"use client";

import type { ReactNode } from "react";
import Box from "@mui/material/Box";
import Card from "@/presentation/common/components/atoms/card";
import Typography from "@/presentation/common/components/atoms/typography";

interface ReviewStatCardProps {
  icon: ReactNode;
  label: string;
  /** The figure, already formatted; "—" while the API cannot provide it. */
  value: string;
}

/** One figure of the review: its icon, its label and the value in large type. */
export default function ReviewStatCard({ icon, label, value }: ReviewStatCardProps) {
  return (
    <Card
      variant="outlined"
      sx={{ display: "flex", alignItems: "center", gap: 1.5, p: 2, flex: "1 1 200px", minWidth: 0 }}
    >
      <Box sx={{ display: "flex", color: "primary.main" }}>{icon}</Box>
      <Box sx={{ minWidth: 0 }}>
        <Typography variant="body2" color="text.secondary" component="p">
          {label}
        </Typography>
        <Typography variant="h5" component="p" sx={{ fontWeight: 700 }}>
          {value}
        </Typography>
      </Box>
    </Card>
  );
}
