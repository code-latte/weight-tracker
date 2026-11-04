"use client";

import { useMemo } from "react";
import AddOutlined from "@mui/icons-material/AddOutlined";
import AppsOutlined from "@mui/icons-material/AppsOutlined";
import PersonOutlined from "@mui/icons-material/PersonOutlined";
import PlayCircleOutlined from "@mui/icons-material/PlayCircleOutlined";
import Star from "@mui/icons-material/Star";
import StarBorder from "@mui/icons-material/StarBorder";
import Box from "@mui/material/Box";
import type { DomainErrorCode, Exercise } from "@weight-tracker/core/domain";
import { useTranslation } from "react-i18next";
import Alert from "@/presentation/common/components/atoms/alert";
import Button from "@/presentation/common/components/atoms/button";
import IconButton from "@/presentation/common/components/atoms/icon-button";
import List from "@/presentation/common/components/atoms/list";
import TextField from "@/presentation/common/components/atoms/text-field";
import Typography from "@/presentation/common/components/atoms/typography";
import ExerciseListItem, {
  EXERCISE_GRID_COLUMNS,
} from "@/presentation/workouts/components/molecules/exercise-list-item";
import ExerciseSectionList, {
  type ExerciseSectionOption,
} from "@/presentation/workouts/components/molecules/exercise-section-list";
import LabelFilter from "@/presentation/workouts/components/molecules/label-filter";
import type { ExerciseCatalogFilters } from "@/presentation/workouts/hooks/use-exercise-catalog-filters";
import useExerciseLabels, {
  EXERCISE_TYPES,
} from "@/presentation/workouts/hooks/use-exercise-labels";

interface ExerciseCatalogBrowserProps {
  /** The catalog exercises to browse; the section and the filters narrow them. */
  exercises: Exercise[];
  filters: ExerciseCatalogFilters;
  /** The signed-in user, whose own exercises "Mis ejercicios" lists. */
  currentUserId: string | null;
  /** Body parts whose catalog exercises all work one muscle: their exercises show no muscle label. */
  singleMuscleBodyParts: ReadonlySet<string>;
  /** The ids of the user's favorite exercises, and whether they have been read yet. */
  favoriteIds: ReadonlySet<string>;
  favoritesStatus: "loading" | "loaded" | "failed";
  /** The exercises whose star is waiting for the API. */
  pendingFavoriteIds: ReadonlySet<string>;
  /** Why the last star was refused, if it was. */
  favoriteErrorCode: DomainErrorCode | null;
  onToggleFavorite: (exerciseId: string) => void;
  /** Shows the plus button on each card. Without it nothing can be chosen, as on the catalog screen. */
  onChoose?: (exercise: Exercise) => void;
  /** Shows "Ver vídeo" on each card that has a video. Without it no card offers the video, as in the dialog. */
  onWatchVideo?: (exercise: Exercise) => void;
  /**
   * Fills its parent and scrolls the cards inside it, as in the dialog; otherwise the cards flow with the page. The
   * parent must be a column flex container with a height.
   */
  contained: boolean;
}

/** Lowercased and without accents, so "sentadi" finds "Sentadilla" and "cuadriceps" finds "Cuádriceps". */
function normalize(text: string): string {
  return text.normalize("NFD").replace(/\p{M}/gu, "").toLowerCase();
}

function matchesWords(text: string, words: string[]): boolean {
  return words.every((word) => text.includes(word));
}

/**
 * The catalog, narrowed by a section (all, the user's own, favorites) and filtered locally by name, type, body part
 * and, once a body part is selected, muscle. The star marks or unmarks a favorite; the plus, when `onChoose` is given,
 * chooses an exercise. The screen keeps the section and the filters, so a dialog's survive a reopening.
 */
export default function ExerciseCatalogBrowser({
  exercises,
  filters,
  currentUserId,
  singleMuscleBodyParts,
  favoriteIds,
  favoritesStatus,
  pendingFavoriteIds,
  favoriteErrorCode,
  onToggleFavorite,
  onChoose,
  onWatchVideo,
  contained,
}: ExerciseCatalogBrowserProps) {
  const { t } = useTranslation("workouts");
  const { t: tc } = useTranslation();
  const {
    nameOf,
    searchTextOf,
    englishNameOf,
    muscleLabel,
    bodyPartLabel,
    bodyPartColor,
    typeLabel,
    typeColor,
    typeWatermark,
    exerciseLabelsOf,
  } = useExerciseLabels();
  const {
    section: selectedSection,
    text: filterText,
    type: selectedType,
    bodyPart: selectedBodyPart,
    muscle: selectedMuscle,
  } = filters;

  const bodyParts = useMemo(
    () =>
      [...new Set(exercises.map((exercise) => exercise.bodyPart))]
        .map((code) => ({ code, label: bodyPartLabel(code), color: bodyPartColor(code) }))
        .sort((a, b) => a.label.localeCompare(b.label)),
    [exercises, bodyPartLabel, bodyPartColor],
  );

  // A muscle belongs to its body part, so its pills take that body part's colour.
  const muscles = useMemo(
    () =>
      selectedBodyPart === null
        ? []
        : [
            ...new Set(
              exercises
                .filter((exercise) => exercise.bodyPart === selectedBodyPart)
                .map((exercise) => exercise.muscle),
            ),
          ]
            .map((code) => ({
              code,
              label: muscleLabel(code),
              color: bodyPartColor(selectedBodyPart),
            }))
            .sort((a, b) => a.label.localeCompare(b.label)),
    [exercises, selectedBodyPart, muscleLabel, bodyPartColor],
  );

  const visible = useMemo(() => {
    // Every typed word must appear somewhere in the names, in any order: "curl biceps" finds "Curl de bíceps".
    const words = normalize(filterText).split(/\s+/).filter(Boolean);

    // The section narrows the catalog first; the text and the pills apply on top of it.
    const inSection = (exercise: Exercise) => {
      if (selectedSection === "MINE") {
        return currentUserId !== null && exercise.ownerId === currentUserId;
      }
      if (selectedSection === "FAVORITES") {
        return favoriteIds.has(exercise.id);
      }
      return true;
    };

    return exercises.filter(
      (exercise) =>
        inSection(exercise) &&
        (selectedType === null || exercise.type === selectedType) &&
        (selectedBodyPart === null || exercise.bodyPart === selectedBodyPart) &&
        (selectedMuscle === null || exercise.muscle === selectedMuscle) &&
        matchesWords(normalize(searchTextOf(exercise)), words),
    );
  }, [
    exercises,
    selectedSection,
    currentUserId,
    favoriteIds,
    filterText,
    selectedType,
    selectedBodyPart,
    selectedMuscle,
    searchTextOf,
  ]);

  // Every known type, always in the same order, whether or not the catalog has exercises of it.
  const types = useMemo(
    () => EXERCISE_TYPES.map((code) => ({ code, label: typeLabel(code), color: typeColor(code) })),
    [typeLabel, typeColor],
  );

  const sections: ExerciseSectionOption[] = [
    { value: "ALL", label: t("workout.catalog.sections.ALL"), icon: <AppsOutlined /> },
    { value: "MINE", label: t("workout.catalog.sections.MINE"), icon: <PersonOutlined /> },
    {
      value: "FAVORITES",
      label: t("workout.catalog.sections.FAVORITES"),
      icon: <StarBorder />,
      // Without the favorites there is nothing to list in this section.
      disabled: favoritesStatus === "failed",
    },
  ];

  return (
    <Box
      sx={{
        pt: 1,
        display: "flex",
        // Side by side on wide screens; the sections stack above the catalog on narrow ones.
        flexDirection: { xs: "column", md: "row" },
        gap: { xs: 0, md: 3 },
        ...(contained && { flex: 1, minHeight: 0 }),
      }}
    >
      <Box sx={{ width: { md: 200 }, flexShrink: 0, mb: { xs: 2, md: 0 } }}>
        <ExerciseSectionList
          aria-label={t("workout.catalog.sections.label")}
          sections={sections}
          selected={selectedSection}
          onChange={filters.setSection}
        />
      </Box>
      <Box sx={{ flex: 1, minWidth: 0, minHeight: 0, display: "flex", flexDirection: "column" }}>
        {favoritesStatus === "failed" && (
          <Alert severity="warning" sx={{ mb: 2 }}>
            {t("workout.catalog.favorite.loadError")}
          </Alert>
        )}
        {favoriteErrorCode && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {tc(`errors:${favoriteErrorCode}`)}
          </Alert>
        )}
        <TextField
          label={t("workout.catalog.filter")}
          type="search"
          size="small"
          fullWidth
          value={filterText}
          onChange={(event) => filters.setText(event.target.value)}
          sx={{ mb: 2 }}
        />
        <Box sx={{ mb: 1 }}>
          <LabelFilter
            label={t("workout.catalog.types")}
            options={types}
            selected={selectedType}
            onToggle={filters.toggleType}
          />
        </Box>
        <LabelFilter
          label={t("workout.catalog.bodyParts")}
          options={bodyParts}
          selected={selectedBodyPart}
          onToggle={filters.toggleBodyPart}
        />
        {/* A single muscle has nothing to choose between. */}
        {muscles.length > 1 && (
          <Box sx={{ mt: 1 }}>
            <LabelFilter
              label={t("workout.catalog.muscles")}
              options={muscles}
              selected={selectedMuscle}
              onToggle={filters.toggleMuscle}
            />
          </Box>
        )}
        {visible.length === 0 ? (
          <Typography color="text.secondary" sx={{ mt: 2 }}>
            {t("workout.catalog.empty")}
          </Typography>
        ) : (
          <List
            aria-label={t("workout.catalog.title")}
            sx={{
              mt: 1,
              display: "grid",
              gridTemplateColumns: EXERCISE_GRID_COLUMNS,
              gap: 2,
              ...(contained && { flex: 1, minHeight: 0, overflowY: "auto", alignContent: "start" }),
            }}
          >
            {visible.map((exercise) => {
              const favorite = favoriteIds.has(exercise.id);

              return (
                <ExerciseListItem
                  key={exercise.id}
                  name={nameOf(exercise)}
                  subtitle={englishNameOf(exercise) ?? undefined}
                  footer={
                    onWatchVideo && exercise.videoUrl ? (
                      <Button
                        size="small"
                        startIcon={<PlayCircleOutlined />}
                        onClick={() => onWatchVideo(exercise)}
                      >
                        {t("exerciseCatalog.watchVideo")}
                      </Button>
                    ) : undefined
                  }
                  labels={exerciseLabelsOf(exercise, !singleMuscleBodyParts.has(exercise.bodyPart))}
                  watermark={typeWatermark(exercise.type)}
                  // Half the width of the buttons (star, and plus when choosing), so they do not sit over the type icon.
                  watermarkInsetRight={onChoose ? 5 : 2.5}
                  actions={
                    <>
                      <IconButton
                        edge={onChoose ? undefined : "end"}
                        aria-label={t(
                          favorite
                            ? "workout.catalog.favorite.remove"
                            : "workout.catalog.favorite.add",
                        )}
                        color={favorite ? "warning" : "default"}
                        disabled={
                          favoritesStatus !== "loaded" || pendingFavoriteIds.has(exercise.id)
                        }
                        onClick={() => onToggleFavorite(exercise.id)}
                      >
                        {favorite ? <Star /> : <StarBorder />}
                      </IconButton>
                      {onChoose && (
                        <IconButton
                          edge="end"
                          aria-label={t("workout.catalog.choose")}
                          onClick={() => onChoose(exercise)}
                        >
                          <AddOutlined />
                        </IconButton>
                      )}
                    </>
                  }
                />
              );
            })}
          </List>
        )}
      </Box>
    </Box>
  );
}
