"use client";

import CalendarTodayOutlined from "@mui/icons-material/CalendarTodayOutlined";
import UpdateOutlined from "@mui/icons-material/UpdateOutlined";
import Box from "@mui/material/Box";
import { useState, type ReactNode } from "react";
import { useTranslation } from "react-i18next";
import Typography from "@/presentation/common/components/atoms/typography";
import PersonCard, {
  type PersonCardProps,
} from "@/presentation/common/components/molecules/person-card";

interface WorkoutReviewPersonStripProps {
  athlete: PersonCardProps;
  /** `null` leaves the trainer out. */
  trainer: PersonCardProps | null;
  /** When the athlete started the workout, or `null` when it has no start time. */
  startedAt: string | null;
  /** When the athlete's results last changed, or `null` when they never did. */
  resultsUpdatedAt: string | null;
}

const RELATIVE_UNITS: [Intl.RelativeTimeFormatUnit, number][] = [
  ["day", 86_400],
  ["hour", 3_600],
  ["minute", 60],
  ["second", 1],
];

/** "hace 30 segundos": the largest unit the elapsed time reaches, in the active language. */
function relativeTimeOf(timestamp: string, now: number, language: string): string {
  const seconds = Math.min(Math.round((new Date(timestamp).getTime() - now) / 1000), 0);
  const [unit, size] =
    RELATIVE_UNITS.find(([, unitSeconds]) => Math.abs(seconds) >= unitSeconds) ??
    RELATIVE_UNITS[RELATIVE_UNITS.length - 1]!;

  return new Intl.RelativeTimeFormat(language, { numeric: "auto" }).format(
    Math.round(seconds / size),
    unit,
  );
}

function Fact({ icon, children }: { icon: ReactNode; children: ReactNode }) {
  return (
    <Box sx={{ display: "inline-flex", alignItems: "center", gap: 0.75, color: "text.secondary" }}>
      {icon}
      <Typography variant="body2" component="span">
        {children}
      </Typography>
    </Box>
  );
}

/** Whose workout it is, when the athlete started it and when their results last changed. */
export default function WorkoutReviewPersonStrip({
  athlete,
  trainer,
  startedAt,
  resultsUpdatedAt,
}: WorkoutReviewPersonStripProps) {
  const { t, i18n } = useTranslation("workouts");
  // Read once: the screen does not refresh, so the text does not tick.
  const [now] = useState(() => Date.now());

  return (
    <Box
      component="section"
      aria-label={t("workout.context.label")}
      sx={{ display: "flex", flexWrap: "wrap", alignItems: "center", gap: 2, mb: 3 }}
    >
      <PersonCard {...athlete} />
      {trainer !== null && <PersonCard {...trainer} />}
      <Box
        sx={{ display: "flex", flexWrap: "wrap", alignItems: "center", gap: 3, ml: { md: "auto" } }}
      >
        <Fact icon={<CalendarTodayOutlined fontSize="small" />}>
          {t("review.startedAt", {
            time:
              startedAt === null
                ? t("review.notAvailable")
                : new Intl.DateTimeFormat(i18n.language, {
                    dateStyle: "medium",
                    timeStyle: "short",
                  }).format(new Date(startedAt)),
          })}
        </Fact>
        {resultsUpdatedAt !== null && (
          <Fact icon={<UpdateOutlined fontSize="small" />}>
            {t("review.lastUpdate", {
              when: relativeTimeOf(resultsUpdatedAt, now, i18n.language),
            })}
          </Fact>
        )}
      </Box>
    </Box>
  );
}
