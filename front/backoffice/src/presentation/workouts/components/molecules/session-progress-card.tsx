"use client";

import Box from "@mui/material/Box";
import { useTranslation } from "react-i18next";
import Card from "@/presentation/common/components/atoms/card";
import LinearProgress from "@/presentation/common/components/atoms/linear-progress";
import Typography from "@/presentation/common/components/atoms/typography";

interface SessionProgressCardProps {
  /** Sets recorded over sets planned, rounded. */
  percent: number;
  done: number;
  pending: number;
}

/** How far the athlete got: the percentage, a bar, and the sets done and still pending. */
export default function SessionProgressCard({ percent, done, pending }: SessionProgressCardProps) {
  const { t } = useTranslation("workouts");
  const title = t("review.progress.title");

  return (
    <Card variant="outlined" sx={{ p: 2 }}>
      <Box sx={{ display: "flex", justifyContent: "space-between", alignItems: "baseline" }}>
        <Typography variant="h6" component="h2">
          {title}
        </Typography>
        <Typography variant="h6" component="p" sx={{ fontWeight: 700 }}>
          {`${percent}%`}
        </Typography>
      </Box>
      <LinearProgress
        variant="determinate"
        value={percent}
        aria-label={title}
        sx={{ my: 1.5, height: 10, borderRadius: 5 }}
      />
      <Box sx={{ display: "flex", justifyContent: "space-between" }}>
        <Typography variant="body2" color="text.secondary">
          {t("review.progress.done", { count: done })}
        </Typography>
        <Typography variant="body2" color="text.secondary">
          {t("review.progress.pending", { count: pending })}
        </Typography>
      </Box>
    </Card>
  );
}
