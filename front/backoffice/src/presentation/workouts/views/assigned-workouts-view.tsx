"use client";

import { useState } from "react";
import AddOutlined from "@mui/icons-material/AddOutlined";
import Box from "@mui/material/Box";
import Container from "@mui/material/Container";
import Button from "@/presentation/common/components/atoms/button";
import Typography from "@/presentation/common/components/atoms/typography";
import { useTranslation } from "react-i18next";
import HomeSkeleton from "@/presentation/common/components/molecules/home-skeleton";
import LoadError from "@/presentation/common/components/molecules/load-error";
import AppShell from "@/presentation/common/components/organisms/app-shell";
import useCurrentUser from "@/presentation/common/hooks/use-current-user";
import CreateWorkoutDialog from "@/presentation/workouts/components/organisms/create-workout-dialog";
import WorkoutsTable from "@/presentation/workouts/components/organisms/workouts-table";
import useWorkouts from "@/presentation/workouts/hooks/use-workouts";

function AssignedPlansList() {
  const current = useCurrentUser();
  const { status, workouts, errorCode, reload } = useWorkouts();

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

  return <WorkoutsTable workouts={workouts} showTrainer={current.profile.role === "ADMIN"} />;
}

function AssignedPlansContent() {
  const { t } = useTranslation("workouts");
  const [creating, setCreating] = useState(false);

  return (
    <Container sx={{ py: 4 }}>
      <Box sx={{ display: "flex", alignItems: "center", justifyContent: "space-between", mb: 3 }}>
        <Typography variant="h4" component="h1">
          {t("assignedPlans.title")}
        </Typography>
        <Button variant="contained" startIcon={<AddOutlined />} onClick={() => setCreating(true)}>
          {t("assignedPlans.newWorkout")}
        </Button>
      </Box>
      <AssignedPlansList />
      <CreateWorkoutDialog open={creating} onClose={() => setCreating(false)} />
    </Container>
  );
}

export default function AssignedPlansView() {
  return (
    <AppShell>
      <AssignedPlansContent />
    </AppShell>
  );
}
