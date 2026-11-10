"use client";

import { useMemo, useState } from "react";
import AddOutlined from "@mui/icons-material/AddOutlined";
import Box from "@mui/material/Box";
import Container from "@mui/material/Container";
import type { WorkoutTemplate } from "@weight-tracker/core/domain";
import { useTranslation } from "react-i18next";
import Alert from "@/presentation/common/components/atoms/alert";
import Button from "@/presentation/common/components/atoms/button";
import Typography from "@/presentation/common/components/atoms/typography";
import { fullNameOf } from "@/presentation/common/full-name";
import HomeSkeleton from "@/presentation/common/components/molecules/home-skeleton";
import LoadError from "@/presentation/common/components/molecules/load-error";
import AppShell from "@/presentation/common/components/organisms/app-shell";
import useAssignees from "@/presentation/common/hooks/use-assignees";
import useCurrentUser from "@/presentation/common/hooks/use-current-user";
import CreateWorkoutTemplateDialog from "@/presentation/workouts/components/organisms/create-workout-template-dialog";
import DeleteWorkoutTemplateDialog from "@/presentation/workouts/components/organisms/delete-workout-template-dialog";
import WorkoutTemplatesTable from "@/presentation/workouts/components/organisms/workout-templates-table";
import useDeleteWorkoutTemplate from "@/presentation/workouts/hooks/use-delete-workout-template";
import useWorkoutTemplates from "@/presentation/workouts/hooks/use-workout-templates";

function WorkoutTemplatesList() {
  const { t: tc } = useTranslation();
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
  const deletion = useDeleteWorkoutTemplate();
  // The template whose deletion is waiting for confirmation.
  const [pendingDelete, setPendingDelete] = useState<WorkoutTemplate | null>(null);

  async function handleDelete(id: string) {
    const deleted = await deletion.remove(id);
    setPendingDelete(null);

    if (deleted) {
      reload();
    }
  }

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
    <>
      {deletion.errorCode && (
        <Alert severity="error" sx={{ mb: 3 }}>
          {tc(`errors:${deletion.errorCode}`)}
        </Alert>
      )}
      <WorkoutTemplatesTable
        templates={templates}
        showTrainer={isAdmin}
        trainerNameOf={(trainerId) => trainerNames.get(trainerId) ?? null}
        onDelete={setPendingDelete}
      />
      <DeleteWorkoutTemplateDialog
        open={pendingDelete !== null}
        pending={deletion.deleting}
        onCancel={() => setPendingDelete(null)}
        onConfirm={() => pendingDelete && void handleDelete(pendingDelete.id)}
      />
    </>
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
