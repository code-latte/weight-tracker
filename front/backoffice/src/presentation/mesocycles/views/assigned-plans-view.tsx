"use client";

import AddOutlined from "@mui/icons-material/AddOutlined";
import Box from "@mui/material/Box";
import Container from "@mui/material/Container";
import NextLink from "next/link";
import Button from "@/presentation/common/components/atoms/button";
import Typography from "@/presentation/common/components/atoms/typography";
import { useTranslation } from "react-i18next";
import HomeSkeleton from "@/presentation/common/components/molecules/home-skeleton";
import LoadError from "@/presentation/common/components/molecules/load-error";
import AppShell from "@/presentation/common/components/organisms/app-shell";
import useCurrentUser from "@/presentation/common/hooks/use-current-user";
import MesocyclesTable from "@/presentation/mesocycles/components/organisms/mesocycles-table";
import useMesocycles from "@/presentation/mesocycles/hooks/use-mesocycles";

function AssignedPlansList() {
  const current = useCurrentUser();
  const { status, mesocycles, errorCode, reload } = useMesocycles();

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

  return <MesocyclesTable mesocycles={mesocycles} showTrainer={current.profile.role === "ADMIN"} />;
}

function AssignedPlansContent() {
  const { t } = useTranslation("mesocycles");

  return (
    <Container sx={{ py: 4 }}>
      <Box sx={{ display: "flex", alignItems: "center", justifyContent: "space-between", mb: 3 }}>
        <Typography variant="h4" component="h1">
          {t("assignedPlans.title")}
        </Typography>
        <Button
          variant="contained"
          startIcon={<AddOutlined />}
          component={NextLink}
          href="/mesocycle"
        >
          {t("assignedPlans.newMesocycle")}
        </Button>
      </Box>
      <AssignedPlansList />
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
