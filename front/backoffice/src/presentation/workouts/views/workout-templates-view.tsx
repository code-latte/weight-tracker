"use client";

import { useMemo, useState } from "react";
import AddOutlined from "@mui/icons-material/AddOutlined";
import Box from "@mui/material/Box";
import Container from "@mui/material/Container";
import { useTranslation } from "react-i18next";
import Button from "@/presentation/common/components/atoms/button";
import Typography from "@/presentation/common/components/atoms/typography";
import { fullNameOf } from "@/presentation/common/full-name";
import HomeSkeleton from "@/presentation/common/components/molecules/home-skeleton";
import LoadError from "@/presentation/common/components/molecules/load-error";
import AppShell from "@/presentation/common/components/organisms/app-shell";
import useAssignees from "@/presentation/common/hooks/use-assignees";
import useCurrentUser from "@/presentation/common/hooks/use-current-user";
import CreateWorkoutTemplateDialog from "@/presentation/workouts/components/organisms/create-workout-template-dialog";
import WorkoutTemplatesTable from "@/presentation/workouts/components/organisms/workout-templates-table";
import useWorkoutTemplates from "@/presentation/workouts/hooks/use-workout-templates";

function WorkoutTemplatesList() {
  const current = useCurrentUser();
  const { status, templates, errorCode, reload } = useWorkoutTemplates();
  const isAdmin = current.profile?.role === "ADMIN";
  // A template carries only its trainer's id: an admin reads the users to name them. A trainer reads nobody.
  const assignees = useAssignees(isAdmin ? "ADMIN" : null);
  const trainerNames = useMemo(
    () =>
      new Map(
        assignees.trainers.map((trainer) => [trainer.id, fullNameOf(trainer) ?? trainer.email]),
      ),
    [assignees.trainers],
  );

  if (current.status === "failed" && current.errorCode) {
    return <LoadError code={current.errorCode} onRetry={current.reload} />;
  }
  if (status === "failed" && errorCode) {
    return <LoadError code={errorCode} onRetry={reload} />;
  }
  // Waits for the role too, so the trainer column never appears or disappears after rendering.
  if (status !== "loaded" || current.status !== "loaded" || current.profile === null) {
    return <HomeSkeleton />;
  }

  return (
    <WorkoutTemplatesTable
      templates={templates}
      showTrainer={isAdmin}
      trainerNameOf={(trainerId) => trainerNames.get(trainerId) ?? null}
    />
  );
}

function WorkoutTemplatesContent() {
  const { t } = useTranslation("workouts");
  const [creating, setCreating] = useState(false);

  return (
    <Container sx={{ py: 4 }}>
      <Box sx={{ display: "flex", alignItems: "center", justifyContent: "space-between", mb: 3 }}>
        <Typography variant="h4" component="h1">
          {t("workoutTemplates.title")}
        </Typography>
        <Button variant="contained" startIcon={<AddOutlined />} onClick={() => setCreating(true)}>
          {t("workoutTemplates.new")}
        </Button>
      </Box>
      <WorkoutTemplatesList />
      <CreateWorkoutTemplateDialog open={creating} onClose={() => setCreating(false)} />
    </Container>
  );
}

export default function WorkoutTemplatesView() {
  return (
    <AppShell>
      <WorkoutTemplatesContent />
    </AppShell>
  );
}
