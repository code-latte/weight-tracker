"use client";

import { useMemo, useState } from "react";
import AddOutlined from "@mui/icons-material/AddOutlined";
import Box from "@mui/material/Box";
import Container from "@mui/material/Container";
import type { Exercise } from "@weight-tracker/core/domain";
import { useTranslation } from "react-i18next";
import Button from "@/presentation/common/components/atoms/button";
import Typography from "@/presentation/common/components/atoms/typography";
import HomeSkeleton from "@/presentation/common/components/molecules/home-skeleton";
import LoadError from "@/presentation/common/components/molecules/load-error";
import AppShell from "@/presentation/common/components/organisms/app-shell";
import useCurrentUser from "@/presentation/common/hooks/use-current-user";
import ExerciseVideoDialog from "@/presentation/workouts/components/organisms/exercise-video-dialog";
import CreateExerciseDialog from "@/presentation/workouts/components/organisms/create-exercise-dialog";
import ExerciseCatalogBrowser from "@/presentation/workouts/components/organisms/exercise-catalog-browser";
import { singleMuscleBodyPartsOf } from "@/presentation/workouts/catalog";
import useExerciseCatalogFilters from "@/presentation/workouts/hooks/use-exercise-catalog-filters";
import useExerciseFavorites from "@/presentation/workouts/hooks/use-exercise-favorites";
import useExerciseLabels from "@/presentation/workouts/hooks/use-exercise-labels";
import useExercises from "@/presentation/workouts/hooks/use-exercises";

function ExerciseCatalogContent() {
  const { t } = useTranslation("workouts");
  const current = useCurrentUser();
  const catalog = useExercises();
  // Read beside the catalog, but it never blocks the screen: a failure leaves the stars off.
  const favorites = useExerciseFavorites();
  const filters = useExerciseCatalogFilters();
  const [creating, setCreating] = useState(false);
  const [watching, setWatching] = useState<Exercise | null>(null);
  const { nameOf } = useExerciseLabels();
  const singleMuscleBodyParts = useMemo(
    () => singleMuscleBodyPartsOf(catalog.exercises),
    [catalog.exercises],
  );

  const profile = current.status === "loaded" ? current.profile : null;
  const loaded = catalog.status === "loaded" && profile !== null;
  // Mirrors the API, which answers 403 to an admin; the API keeps the rule.
  const canCreate = loaded && profile.role === "TRAINER";

  function body() {
    if (current.status === "failed" && current.errorCode) {
      return <LoadError code={current.errorCode} onRetry={current.reload} />;
    }
    if (catalog.status === "failed" && catalog.errorCode) {
      return <LoadError code={catalog.errorCode} onRetry={catalog.reload} />;
    }
    if (!loaded) {
      return <HomeSkeleton />;
    }

    return (
      // The finder, the pills and the sections stay put; only the list scrolls, as in the workout's dialog.
      <Box
        sx={{
          display: "flex",
          flexDirection: "column",
          height: "calc(100vh - 220px)",
          minHeight: 480,
        }}
      >
        <ExerciseCatalogBrowser
          contained
          exercises={catalog.exercises}
          filters={filters}
          currentUserId={profile.id}
          singleMuscleBodyParts={singleMuscleBodyParts}
          favoriteIds={favorites.favoriteIds}
          favoritesStatus={favorites.status}
          pendingFavoriteIds={favorites.pendingIds}
          favoriteErrorCode={favorites.toggleErrorCode}
          onToggleFavorite={(exerciseId) => void favorites.toggle(exerciseId)}
          onWatchVideo={setWatching}
        />
      </Box>
    );
  }

  return (
    <Container maxWidth={false} sx={{ py: 4 }}>
      <Box sx={{ display: "flex", alignItems: "center", justifyContent: "space-between", mb: 3 }}>
        <Typography variant="h4" component="h1">
          {t("exerciseCatalog.title")}
        </Typography>
        {canCreate && (
          <Button variant="contained" startIcon={<AddOutlined />} onClick={() => setCreating(true)}>
            {t("exerciseCatalog.add")}
          </Button>
        )}
      </Box>
      {body()}
      {watching?.videoUrl && (
        // Mounted per opening, so closing it stops the video.
        <ExerciseVideoDialog
          name={nameOf(watching)}
          url={watching.videoUrl}
          onClose={() => setWatching(null)}
        />
      )}
      <CreateExerciseDialog
        open={creating}
        onCreated={(exercise) => {
          catalog.add(exercise);
          setCreating(false);
        }}
        onClose={() => setCreating(false)}
      />
    </Container>
  );
}

export default function ExerciseCatalogView() {
  return (
    <AppShell>
      <ExerciseCatalogContent />
    </AppShell>
  );
}
