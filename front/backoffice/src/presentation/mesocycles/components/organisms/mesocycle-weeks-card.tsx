"use client";

import AddOutlined from "@mui/icons-material/AddOutlined";
import Box from "@mui/material/Box";
import type { DomainErrorCode, MesocycleWeek, WorkoutListItem } from "@weight-tracker/core/domain";
import { useTranslation } from "react-i18next";
import Button from "@/presentation/common/components/atoms/button";
import Card from "@/presentation/common/components/atoms/card";
import CardContent from "@/presentation/common/components/atoms/card-content";
import List from "@/presentation/common/components/atoms/list";
import LoadError from "@/presentation/common/components/molecules/load-error";
import Typography from "@/presentation/common/components/atoms/typography";
import MesocycleWeekListItem from "@/presentation/mesocycles/components/molecules/mesocycle-week-list-item";
import MesocycleWeekWorkoutsTable from "@/presentation/mesocycles/components/molecules/mesocycle-week-workouts-table";
import { weekNumberOf } from "@/presentation/mesocycles/weeks";

/** The mesocycle's workouts as read: grouped by week once loaded, with the way to read them again. */
interface WeeksCardWorkouts {
  status: "loading" | "loaded" | "failed";
  byWeek: ReadonlyMap<number, WorkoutListItem[]>;
  errorCode: DomainErrorCode | null;
  reload: () => void;
}

interface MesocycleWeeksCardProps {
  titleId: string;
  /** The defined weeks, already in the order to show them. */
  weeks: MesocycleWeek[];
  /** The mesocycle does not exist yet: nothing can be planned. */
  locked: boolean;
  /** Whether a week is left to define. */
  canAdd: boolean;
  onAdd: () => void;
  onEdit: (week: MesocycleWeek) => void;
  /** The mesocycle's workouts; absent while the mesocycle does not exist. */
  workouts?: WeeksCardWorkouts;
  onCreateWorkout?: (weekNumber: number) => void;
}

/**
 * The mesocycle's weeks under a "Planificación semanal" header, with the action that defines the next one. Once the
 * mesocycle's workouts are read, each week lists its own and offers to create one.
 */
export default function MesocycleWeeksCard({
  titleId,
  weeks,
  locked,
  canAdd,
  onAdd,
  onEdit,
  workouts,
  onCreateWorkout,
}: MesocycleWeeksCardProps) {
  const { t } = useTranslation("mesocycles");

  function body() {
    if (locked) {
      return <Typography color="text.secondary">{t("weeks.saveFirst")}</Typography>;
    }
    if (weeks.length === 0) {
      return <Typography color="text.secondary">{t("weeks.empty")}</Typography>;
    }
    return (
      <List sx={{ display: "flex", flexDirection: "column", gap: 1, p: 0 }}>
        {weeks.map((week) => {
          const number = weekNumberOf(week);
          const entries = workouts?.byWeek.get(number) ?? [];

          return (
            <MesocycleWeekListItem
              key={number}
              type={week.trainingWeekType}
              title={t("weeks.week", { number })}
              objective={week.objective}
              editLabel={t("weeks.edit", { number })}
              onEdit={() => onEdit(week)}
              workouts={
                workouts?.status === "loaded" && onCreateWorkout
                  ? {
                      hasEntries: entries.length > 0,
                      countLabel: t("weeks.workoutCount", { count: entries.length }),
                      showLabel: t("weeks.showWorkouts", { number }),
                      hideLabel: t("weeks.hideWorkouts", { number }),
                      createLabel: t("weeks.newWorkout"),
                      onCreate: () => onCreateWorkout(number),
                    }
                  : undefined
              }
            >
              <MesocycleWeekWorkoutsTable
                rows={entries.map((workout) => ({
                  id: workout.id,
                  name: workout.name,
                  exerciseCount: workout.exercises.length,
                  href: `/workout?id=${encodeURIComponent(workout.id)}`,
                  editLabel: t("weeks.editWorkout", { name: workout.name }),
                }))}
                nameLabel={t("weeks.workoutsTable.name")}
                exercisesLabel={t("weeks.workoutsTable.exercises")}
              />
            </MesocycleWeekListItem>
          );
        })}
      </List>
    );
  }

  return (
    <Card variant="outlined">
      <CardContent>
        <Box
          sx={{
            display: "flex",
            alignItems: "center",
            justifyContent: "space-between",
            flexWrap: "wrap",
            gap: 1,
            mb: 2,
          }}
        >
          <Box>
            <Typography id={titleId} variant="h6" component="h2">
              {t("weeks.title")}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {t("weeks.subtitle")}
            </Typography>
          </Box>
          <Button
            variant="contained"
            startIcon={<AddOutlined />}
            onClick={onAdd}
            disabled={locked || !canAdd}
          >
            {t("weeks.add")}
          </Button>
        </Box>
        {workouts?.status === "failed" && workouts.errorCode && (
          <Box sx={{ mb: 2 }}>
            <LoadError code={workouts.errorCode} onRetry={workouts.reload} />
          </Box>
        )}
        {body()}
      </CardContent>
    </Card>
  );
}
