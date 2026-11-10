"use client";

import { useState } from "react";
import AddOutlined from "@mui/icons-material/AddOutlined";
import Box from "@mui/material/Box";
import Container from "@mui/material/Container";
import type { WorkoutListItem } from "@weight-tracker/core/domain";
import Alert from "@/presentation/common/components/atoms/alert";
import Button from "@/presentation/common/components/atoms/button";
import Typography from "@/presentation/common/components/atoms/typography";
import { useTranslation } from "react-i18next";
import HomeSkeleton from "@/presentation/common/components/molecules/home-skeleton";
import LoadError from "@/presentation/common/components/molecules/load-error";
import AppShell from "@/presentation/common/components/organisms/app-shell";
import DeleteWorkoutDialog from "@/presentation/common/components/organisms/delete-workout-dialog";
import useCurrentUser from "@/presentation/common/hooks/use-current-user";
import useDeleteWorkout from "@/presentation/common/hooks/use-delete-workout";
import CreateWorkoutDialog from "@/presentation/workouts/components/organisms/create-workout-dialog";
import WorkoutsTable from "@/presentation/workouts/components/organisms/workouts-table";
import useWorkouts from "@/presentation/workouts/hooks/use-workouts";

function AssignedWorkoutsList() {
  const { t: tc } = useTranslation();
  const current = useCurrentUser();
  const { status, workouts, errorCode, reload } = useWorkouts();
  const deletion = useDeleteWorkout();
  // The workout whose deletion is waiting for confirmation.
  const [pendingDelete, setPendingDelete] = useState<WorkoutListItem | null>(null);

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
          {/* Only a draft can be deleted: the API answers CONFLICT for any other status. */}
          {deletion.errorCode === "CONFLICT"
            ? tc("deleteWorkout.conflict")
            : tc(`errors:${deletion.errorCode}`)}
        </Alert>
      )}
      <WorkoutsTable
        workouts={workouts}
        showTrainer={current.profile.role === "ADMIN"}
        onDelete={setPendingDelete}
      />
      <DeleteWorkoutDialog
        open={pendingDelete !== null}
        pending={deletion.deleting}
        onCancel={() => setPendingDelete(null)}
        onConfirm={() => pendingDelete && void handleDelete(pendingDelete.id)}
      />
    </>
  );
}

function AssignedWorkoutsContent() {
  const { t } = useTranslation("workouts");
  const [creating, setCreating] = useState(false);

  return (
    <Container sx={{ py: 4 }}>
      <Box sx={{ display: "flex", alignItems: "center", justifyContent: "space-between", mb: 3 }}>
        <Typography variant="h4" component="h1">
          {t("assignedWorkouts.title")}
        </Typography>
        <Button variant="contained" startIcon={<AddOutlined />} onClick={() => setCreating(true)}>
          {t("assignedWorkouts.newWorkout")}
        </Button>
      </Box>
      <AssignedWorkoutsList />
      <CreateWorkoutDialog open={creating} onClose={() => setCreating(false)} />
    </Container>
  );
}

export default function AssignedWorkoutsView() {
  return (
    <AppShell>
      <AssignedWorkoutsContent />
    </AppShell>
  );
}
