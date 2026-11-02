"use client";

import { useState } from "react";
import Box from "@mui/material/Box";
import Container from "@mui/material/Container";
import type { UserSummary } from "@weight-tracker/core/domain";
import { useTranslation } from "react-i18next";
import Alert from "@/presentation/common/components/atoms/alert";
import TextField from "@/presentation/common/components/atoms/text-field";
import Typography from "@/presentation/common/components/atoms/typography";
import HomeSkeleton from "@/presentation/common/components/molecules/home-skeleton";
import LoadError from "@/presentation/common/components/molecules/load-error";
import AppShell from "@/presentation/common/components/organisms/app-shell";
import EndTrainerAthleteDialog, {
  type EndRelationshipTarget,
} from "@/presentation/common/components/organisms/end-trainer-athlete-dialog";
import { fullNameOf } from "@/presentation/common/full-name";
import AssignmentFilter from "@/presentation/trainer-assignments/components/molecules/assignment-filter";
import AssignTrainerDialog from "@/presentation/trainer-assignments/components/organisms/assign-trainer-dialog";
import TrainerAssignmentsTable from "@/presentation/trainer-assignments/components/organisms/trainer-assignments-table";
import {
  type AssignmentFilterValue,
  filterTrainerAssignments,
} from "@/presentation/trainer-assignments/filter-trainer-assignments";
import useTrainerAssignments from "@/presentation/trainer-assignments/hooks/use-trainer-assignments";

/** What the last finished action was, for the success message. */
type Outcome = "assigned" | "ended";

function TrainerAssignmentsContent() {
  const { t } = useTranslation("trainerAssignments");
  const { t: tc } = useTranslation();
  const { status, athletes, trainers, errorCode, reload } = useTrainerAssignments();
  const [search, setSearch] = useState("");
  const [assignment, setAssignment] = useState<AssignmentFilterValue | null>(null);
  const [assignTarget, setAssignTarget] = useState<UserSummary | null>(null);
  const [endTarget, setEndTarget] = useState<EndRelationshipTarget | null>(null);
  const [outcome, setOutcome] = useState<Outcome | null>(null);

  function openAssign(athlete: UserSummary) {
    setOutcome(null);
    setAssignTarget(athlete);
  }

  function openEnd(athlete: UserSummary) {
    if (!athlete.trainer) {
      return;
    }
    setOutcome(null);
    setEndTarget({
      trainerId: athlete.trainer.id,
      athleteId: athlete.id,
      athleteName: fullNameOf(athlete) ?? athlete.email,
    });
  }

  function handleDone(done: Outcome) {
    setAssignTarget(null);
    setEndTarget(null);
    setOutcome(done);
    reload();
  }

  function handleClosed(stale: boolean) {
    setAssignTarget(null);
    setEndTarget(null);
    if (stale) {
      reload();
    }
  }

  function list() {
    if (status === "failed" && errorCode) {
      return <LoadError code={errorCode} onRetry={reload} />;
    }
    if (status !== "loaded") {
      return <HomeSkeleton />;
    }
    return (
      <TrainerAssignmentsTable
        athletes={filterTrainerAssignments(athletes, search, assignment)}
        filtered={search.trim() !== "" || assignment !== null}
        onAssign={openAssign}
        onEnd={openEnd}
      />
    );
  }

  return (
    <Container sx={{ py: 4 }}>
      <Typography variant="h4" component="h1" sx={{ mb: 3 }}>
        {t("list.title")}
      </Typography>
      <Box sx={{ display: "flex", flexDirection: "column", gap: 2, mb: 3 }}>
        <TextField
          type="search"
          label={t("list.search")}
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          sx={{ maxWidth: 480 }}
        />
        <AssignmentFilter
          label={t("list.assignmentFilter")}
          options={[
            { value: "with", label: t("list.filter.withTrainer") },
            { value: "without", label: t("list.filter.withoutTrainer") },
          ]}
          selected={assignment}
          onToggle={(value) => setAssignment((selected) => (selected === value ? null : value))}
        />
      </Box>
      {outcome && (
        <Alert severity="success" onClose={() => setOutcome(null)} sx={{ mb: 2 }}>
          {outcome === "assigned" ? t("assign.success") : tc("endRelationship.success")}
        </Alert>
      )}
      {list()}
      <AssignTrainerDialog
        athlete={assignTarget}
        trainers={trainers}
        onClose={handleClosed}
        onAssigned={() => handleDone("assigned")}
      />
      <EndTrainerAthleteDialog
        target={endTarget}
        onClose={handleClosed}
        onEnded={() => handleDone("ended")}
      />
    </Container>
  );
}

export default function TrainerAssignmentsView() {
  return (
    <AppShell>
      <TrainerAssignmentsContent />
    </AppShell>
  );
}
