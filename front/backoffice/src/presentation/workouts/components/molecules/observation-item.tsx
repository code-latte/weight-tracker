"use client";

import InfoOutlined from "@mui/icons-material/InfoOutlined";
import TrendingUpOutlined from "@mui/icons-material/TrendingUpOutlined";
import WarningAmberOutlined from "@mui/icons-material/WarningAmberOutlined";
import Box from "@mui/material/Box";
import { alpha } from "@mui/material/styles";
import type { ReactNode } from "react";
import Card from "@/presentation/common/components/atoms/card";
import Typography from "@/presentation/common/components/atoms/typography";

type SeverityLook = { color: "success" | "info" | "warning"; icon: ReactNode };

/** What each severity the API sends looks like. */
const SEVERITIES: Record<string, SeverityLook> = {
  SUCCESS: { color: "success", icon: <TrendingUpOutlined /> },
  INFO: { color: "info", icon: <InfoOutlined /> },
  WARNING: { color: "warning", icon: <WarningAmberOutlined /> },
};

/** A severity the front does not know reads as plain information. */
const FALLBACK_SEVERITY = SEVERITIES.INFO;

interface ObservationItemProps {
  /** The API's severity: `SUCCESS`, `INFO` or `WARNING`. */
  severity: string;
  title: string;
  /** Left out when there is nothing to say beyond the title. */
  text?: string | null;
  /** Something after the title, such as the "Destacada" chip. */
  mark?: ReactNode;
}

/** One observation of the session: an icon tinted with its severity, a title in that colour and its text. */
export default function ObservationItem({ severity, title, text, mark }: ObservationItemProps) {
  const { color, icon } = SEVERITIES[severity] ?? FALLBACK_SEVERITY;

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
        <Box sx={{ display: "flex", alignItems: "center", flexWrap: "wrap", gap: 1 }}>
          <Typography
            variant="subtitle2"
            component="h3"
            sx={{ color: `${color}.main`, fontWeight: 700 }}
          >
            {title}
          </Typography>
          {mark}
        </Box>
        {text ? (
          <Typography variant="body2" color="text.secondary">
            {text}
          </Typography>
        ) : null}
      </Box>
    </Card>
  );
}
