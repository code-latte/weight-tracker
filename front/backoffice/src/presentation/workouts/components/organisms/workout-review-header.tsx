"use client";

import ArrowBackOutlined from "@mui/icons-material/ArrowBackOutlined";
import SensorsOutlined from "@mui/icons-material/SensorsOutlined";
import Box from "@mui/material/Box";
import NextLink from "next/link";
import { useTranslation } from "react-i18next";
import Button from "@/presentation/common/components/atoms/button";
import Chip from "@/presentation/common/components/atoms/chip";
import Link from "@/presentation/common/components/atoms/link";
import Typography from "@/presentation/common/components/atoms/typography";
import { workoutStatusColorOf } from "@/presentation/common/workout-status";

/** A slow fade between full and half opacity; it stands still for people who ask for less motion. */
const liveChipSx = {
  animation: "review-live-pulse 2.4s ease-in-out infinite",
  "@keyframes review-live-pulse": { "0%, 100%": { opacity: 1 }, "50%": { opacity: 0.4 } },
  "@media (prefers-reduced-motion: reduce)": { animation: "none" },
};

interface WorkoutReviewHeaderProps {
  /** Where "Volver" leads: the workout's mesocycle, or the list of workouts. */
  backHref: string;
  /** The workout's name, with its mesocycle and week when it is placed in one. */
  subtitle: string;
  status: string;
  savingTemplate: boolean;
  onSaveAsTemplate: () => void;
}

/** The review's heading: the way back, the title and subtitle, the status chips and the template action. */
export default function WorkoutReviewHeader({
  backHref,
  subtitle,
  status,
  savingTemplate,
  onSaveAsTemplate,
}: WorkoutReviewHeaderProps) {
  const { t } = useTranslation("workouts");
  const { t: tc } = useTranslation();

  return (
    <Box
      sx={{
        display: "flex",
        flexWrap: "wrap",
        alignItems: "flex-start",
        justifyContent: "space-between",
        gap: 2,
        mb: 3,
      }}
    >
      <Box sx={{ minWidth: 0 }}>
        <Link
          component={NextLink}
          href={backHref}
          underline="hover"
          sx={{ display: "inline-flex", alignItems: "center", gap: 0.5, mb: 0.5 }}
        >
          <ArrowBackOutlined fontSize="small" />
          {t("workout.back")}
        </Link>
        <Typography variant="h4" component="h1">
          {t("review.title")}
        </Typography>
        <Typography color="text.secondary">{subtitle}</Typography>
      </Box>
      <Box sx={{ display: "flex", flexWrap: "wrap", alignItems: "center", gap: 1 }}>
        <Chip
          label={tc(`workoutStatus.${status}`, { defaultValue: status })}
          // A started workout is shown as live: an outlined red chip that fades in and out, slowly.
          {...(status === "STARTED"
            ? { color: "error", variant: "outlined", icon: <SensorsOutlined />, sx: liveChipSx }
            : { color: workoutStatusColorOf(status) })}
        />
        <Button variant="outlined" disabled={savingTemplate} onClick={onSaveAsTemplate}>
          {t("workout.saveAsTemplate")}
        </Button>
      </Box>
    </Box>
  );
}
