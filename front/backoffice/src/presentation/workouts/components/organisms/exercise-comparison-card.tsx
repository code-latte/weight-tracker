"use client";

import BalanceOutlined from "@mui/icons-material/BalanceOutlined";
import Box from "@mui/material/Box";
import type { LastPerformanceSet } from "@weight-tracker/core/domain";
import { useTranslation } from "react-i18next";
import Card from "@/presentation/common/components/atoms/card";
import Table from "@/presentation/common/components/atoms/table";
import TableBody from "@/presentation/common/components/atoms/table-body";
import TableCell from "@/presentation/common/components/atoms/table-cell";
import TableContainer from "@/presentation/common/components/atoms/table-container";
import TableHead from "@/presentation/common/components/atoms/table-head";
import TableRow from "@/presentation/common/components/atoms/table-row";
import Typography from "@/presentation/common/components/atoms/typography";
import ComplianceChip from "@/presentation/workouts/components/molecules/compliance-chip";
import ReviewSectionHeader from "@/presentation/workouts/components/molecules/review-section-header";
import WeightChangeChip from "@/presentation/workouts/components/molecules/weight-change-chip";
import useExerciseLabels from "@/presentation/workouts/hooks/use-exercise-labels";
import { type WeightedExercise, weightChangeOf } from "@/presentation/workouts/workout-review";

interface ExerciseComparisonCardProps {
  weighted: readonly WeightedExercise[];
}

/** Each weighted exercise's top set against the one of its previous execution, with its volume and compliance. */
export default function ExerciseComparisonCard({ weighted }: ExerciseComparisonCardProps) {
  const { t, i18n } = useTranslation("workouts");
  const { nameOf } = useExerciseLabels();
  const notAvailable = t("review.notAvailable");
  const kg = new Intl.NumberFormat(i18n.language, {
    useGrouping: "always",
    maximumFractionDigits: 1,
  });

  function topSetText(set: LastPerformanceSet | null | undefined): string {
    if (!set) {
      return notAvailable;
    }
    if (set.reps != null && set.weight != null) {
      return t("review.done.weighted", { reps: set.reps, weight: Number(set.weight) });
    }
    return set.reps != null ? t("review.done.reps", { reps: set.reps }) : notAvailable;
  }

  return (
    <Card variant="outlined" sx={{ p: 2 }}>
      <ReviewSectionHeader
        icon={<BalanceOutlined />}
        title={t("review.metrics.comparison.title")}
        subtitle={t("review.metrics.comparison.subtitle")}
      />
      {weighted.length === 0 ? (
        <Typography color="text.secondary" sx={{ py: 3, textAlign: "center" }}>
          {t("review.metrics.comparison.empty")}
        </Typography>
      ) : (
        <TableContainer>
          <Table size="small">
            <TableHead sx={{ bgcolor: "action.hover" }}>
              <TableRow>
                <TableCell>{t("review.metrics.comparison.exercise")}</TableCell>
                <TableCell>{t("review.metrics.comparison.previous")}</TableCell>
                <TableCell>{t("review.metrics.comparison.current")}</TableCell>
                <TableCell>{t("review.metrics.comparison.change")}</TableCell>
                <TableCell>{t("review.metrics.comparison.volume")}</TableCell>
                <TableCell>{t("review.metrics.comparison.compliance")}</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {weighted.map(({ summary, exercise, position }) => (
                <TableRow key={summary.workoutExerciseId}>
                  <TableCell>
                    <Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
                      <Box
                        aria-hidden
                        sx={{
                          display: "grid",
                          placeItems: "center",
                          flexShrink: 0,
                          width: 24,
                          height: 24,
                          borderRadius: "50%",
                          bgcolor: "primary.main",
                          color: "primary.contrastText",
                          fontSize: "0.75rem",
                          fontWeight: 700,
                        }}
                      >
                        {position}
                      </Box>
                      {nameOf({ id: exercise.exerciseId, translations: exercise.translations })}
                    </Box>
                  </TableCell>
                  <TableCell>{topSetText(summary.previous?.topSet)}</TableCell>
                  <TableCell sx={{ fontWeight: 700 }}>{topSetText(summary.topSet)}</TableCell>
                  <TableCell>
                    <WeightChangeChip
                      change={weightChangeOf(
                        summary.previous?.topSet.weight,
                        summary.topSet?.weight,
                      )}
                    />
                  </TableCell>
                  <TableCell>
                    {t("review.volumeValue", { value: kg.format(Number(summary.volume)) })}
                  </TableCell>
                  <TableCell>
                    <ComplianceChip
                      inTarget={summary.setsInTarget.inTarget}
                      evaluated={summary.setsInTarget.evaluated}
                    />
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      )}
    </Card>
  );
}
