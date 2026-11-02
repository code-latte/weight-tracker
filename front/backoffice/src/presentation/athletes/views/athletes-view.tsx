"use client";

import { useState } from "react";
import Box from "@mui/material/Box";
import Container from "@mui/material/Container";
import type { TrainerAthlete } from "@weight-tracker/core/domain";
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
import useCurrentUser from "@/presentation/common/hooks/use-current-user";
import AthletesTable from "@/presentation/athletes/components/organisms/athletes-table";
import { filterAthletes } from "@/presentation/athletes/filter-athletes";
import useAthletes from "@/presentation/athletes/hooks/use-athletes";

function AthletesContent() {
  const { t } = useTranslation("athletes");
  const { t: tc } = useTranslation();
  const current = useCurrentUser();
  const { status, athletes, errorCode, reload } = useAthletes();
  const [search, setSearch] = useState("");
  const [target, setTarget] = useState<EndRelationshipTarget | null>(null);
  const [ended, setEnded] = useState(false);
  // The relationship is ended for the signed-in trainer, so the action waits for their id.
  const trainerId = current.status === "loaded" ? current.profile?.id : undefined;

  function openEnd(athlete: TrainerAthlete) {
    if (!trainerId) {
      return;
    }
    setEnded(false);
    setTarget({
      trainerId,
      athleteId: athlete.athleteId,
      athleteName: fullNameOf(athlete) ?? athlete.email,
      ownRelationship: true,
    });
  }

  function handleEnded() {
    setTarget(null);
    setEnded(true);
    reload();
  }

  function handleEndClosed(stale: boolean) {
    setTarget(null);
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
      <AthletesTable
        athletes={filterAthletes(athletes, search)}
        filtered={search.trim() !== ""}
        onEnd={trainerId ? openEnd : undefined}
      />
    );
  }

  return (
    <Container sx={{ py: 4 }}>
      <Typography variant="h4" component="h1" sx={{ mb: 3 }}>
        {t("list.title")}
      </Typography>
      <Box sx={{ mb: 3 }}>
        <TextField
          type="search"
          label={t("list.search")}
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          sx={{ maxWidth: 480 }}
        />
      </Box>
      {ended && (
        <Alert severity="success" onClose={() => setEnded(false)} sx={{ mb: 2 }}>
          {tc("endRelationship.success")}
        </Alert>
      )}
      {list()}
      <EndTrainerAthleteDialog target={target} onClose={handleEndClosed} onEnded={handleEnded} />
    </Container>
  );
}

export default function AthletesView() {
  return (
    <AppShell>
      <AthletesContent />
    </AppShell>
  );
}
