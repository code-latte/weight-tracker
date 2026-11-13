"use client";

import InfoOutlined from "@mui/icons-material/InfoOutlined";
import TrendingUpOutlined from "@mui/icons-material/TrendingUpOutlined";
import WarningAmberOutlined from "@mui/icons-material/WarningAmberOutlined";
import Box from "@mui/material/Box";
import { alpha } from "@mui/material/styles";
import type { ReactNode } from "react";
import Card from "@/presentation/common/components/atoms/card";
import Typography from "@/presentation/common/components/atoms/typography";
import type { ObservationSeverity } from "@/presentation/workouts/session-observations.mock";

const SEVERITIES: Record<
  ObservationSeverity,
  { color: "success" | "info" | "warning"; icon: ReactNode }
> = {
  positive: { color: "success", icon: <TrendingUpOutlined /> },
  info: { color: "info", icon: <InfoOutlined /> },
  warning: { color: "warning", icon: <WarningAmberOutlined /> },
};

interface ObservationItemProps {
  severity: ObservationSeverity;
  title: string;
  text: string;
}

/** One observation of the session: an icon tinted with its severity, a title in that colour and its text. */
export default function ObservationItem({ severity, title, text }: ObservationItemProps) {
  const { color, icon } = SEVERITIES[severity];

  return (
    <Card variant="outlined" sx={{ display: "flex", gap: 1.5, p: 1.5 }}>
      <Box
        sx={(theme) => ({
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          flexShrink: 0,
          width: 40,
          height: 40,
          borderRadius: 2,
          color: `${color}.main`,
          bgcolor: alpha(theme.palette[color].main, 0.14),
        })}
      >
        {icon}
      </Box>
      <Box sx={{ minWidth: 0 }}>
        <Typography
          variant="subtitle2"
          component="h3"
          sx={{ color: `${color}.main`, fontWeight: 700 }}
        >
          {title}
        </Typography>
        <Typography variant="body2" color="text.secondary">
          {text}
        </Typography>
      </Box>
    </Card>
  );
}
