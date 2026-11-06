"use client";

import { Suspense, useEffect, useId, useMemo, useRef, useState } from "react";
import { arrayMove } from "@dnd-kit/sortable";
import ArrowBackOutlined from "@mui/icons-material/ArrowBackOutlined";
import Box from "@mui/material/Box";
import Container from "@mui/material/Container";
import {
  validateWorkoutDetailsForm,
  type WorkoutDetailsFormValues,
  type WorkoutDetailsFormViolations,
} from "@weight-tracker/core/application";
import type {
  Exercise,
  UpdateWorkoutInput,
  WorkoutById,
  WorkoutExerciseDetail,
} from "@weight-tracker/core/domain";
import NextLink from "next/link";
import { useSearchParams } from "next/navigation";
import { useTranslation } from "react-i18next";
import Alert from "@/presentation/common/components/atoms/alert";
import Link from "@/presentation/common/components/atoms/link";
import Typography from "@/presentation/common/components/atoms/typography";
import { fullNameOf } from "@/presentation/common/full-name";
import { initialsOf } from "@/presentation/common/initials";
import type { PersonCardProps } from "@/presentation/common/components/molecules/person-card";
import HomeSkeleton from "@/presentation/common/components/molecules/home-skeleton";
import LoadError from "@/presentation/common/components/molecules/load-error";
import AppShell from "@/presentation/common/components/organisms/app-shell";
import useCurrentUser from "@/presentation/common/hooks/use-current-user";
import ExerciseCatalogDialog from "@/presentation/workouts/components/organisms/exercise-catalog-dialog";
import ExerciseConfigurationDialog from "@/presentation/workouts/components/organisms/exercise-configuration-dialog";
import WorkoutDetailsCard from "@/presentation/workouts/components/organisms/workout-details-card";
import WorkoutExercisesCard from "@/presentation/workouts/components/organisms/workout-exercises-card";
import WorkoutContextStrip, {
  type WorkoutContextMesocycle,
} from "@/presentation/workouts/components/organisms/workout-context-strip";
import WorkoutExercisesList, {
  type WorkoutExerciseItem,
} from "@/presentation/workouts/components/organisms/workout-exercises-list";
import useExerciseCatalogFilters from "@/presentation/workouts/hooks/use-exercise-catalog-filters";
import useExerciseFavorites, {
  type ExerciseFavoritesState,
} from "@/presentation/workouts/hooks/use-exercise-favorites";
import useExercises from "@/presentation/workouts/hooks/use-exercises";
import useRemoveWorkoutExercise from "@/presentation/workouts/hooks/use-remove-workout-exercise";
import useReorderWorkoutExercises from "@/presentation/workouts/hooks/use-reorder-workout-exercises";
import useUpdateWorkout from "@/presentation/workouts/hooks/use-update-workout";
import useWorkout from "@/presentation/workouts/hooks/use-workout";
import useWorkoutMesocycle, {
  type WorkoutMesocycleState,
} from "@/presentation/workouts/hooks/use-workout-mesocycle";
import { singleMuscleBodyPartsOf } from "@/presentation/workouts/catalog";
import { weekMiddleOf, weekStartOf } from "@/presentation/workouts/week";

function WorkoutNotFound() {
  const { t } = useTranslation("workouts");

  return (
    <Box sx={{ py: 2 }}>
      <Typography sx={{ mb: 1 }}>{t("workout.notFound")}</Typography>
      <Link component={NextLink} href="/assigned-workouts">
        {t("workout.backToList")}
      </Link>
    </Box>
  );
}

/** Where "Volver" leads: the mesocycle of a placed workout, or the list of workouts outside any mesocycle. */
function backHrefOf(workout: WorkoutById): string {
  return workout.mesocycleId
    ? `/mesocycle?id=${encodeURIComponent(workout.mesocycleId)}`
    : "/assigned-workouts";
}

/** The page heading and the way back to where the workout lives. */
function WorkoutHeader({ backHref }: { backHref: string }) {
  const { t } = useTranslation("workouts");

  return (
    <Box sx={{ mb: 3 }}>
      <Typography variant="h4" component="h1">
        {t("workout.editTitle")}
      </Typography>
      <Link
        component={NextLink}
        href={backHref}
        underline="hover"
        sx={{ display: "inline-flex", alignItems: "center", gap: 0.5, mt: 0.5 }}
      >
        <ArrowBackOutlined fontSize="small" />
        {t("workout.back")}
      </Link>
    </Box>
  );
}

/** The person's full name, or their email when they have no name. */
function displayNameOf(person: WorkoutById["athlete"]): string {
  return fullNameOf(person) ?? person.email;
}

/** The person as their card in the strip shows them. */
function personCardOf(person: WorkoutById["athlete"], role: string): PersonCardProps {
  return { name: displayNameOf(person), initials: initialsOf(person), role };
}

/** The mesocycle entry of the strip: left out for a standalone workout or when its mesocycle cannot be read. */
function mesocycleEntryOf(
  workout: WorkoutById,
  state: WorkoutMesocycleState,
  weekLabel: (number: number) => string,
): WorkoutContextMesocycle | null {
  if (state.status === "loading") {
    return "loading";
  }
  if (state.status !== "loaded" || !workout.mesocycleId) {
    return null;
  }
  return {
    name: state.mesocycle.name,
    week: weekLabel(Number(workout.mesocycleWeekNumber)),
  };
}

/**
 * Builds the update from valid values. The API replaces every editable property, so athlete, trainer and placement go
 * back as read: an omitted placement would leave the workout standalone. A placed workout is dated by its week, so it
 * sends no date.
 */
function toUpdate(workout: WorkoutById, values: WorkoutDetailsFormValues): UpdateWorkoutInput {
  const input: UpdateWorkoutInput = {
    id: workout.id,
    name: values.name.trim(),
    userId: workout.userId,
    trainerId: workout.trainerId,
  };
  const description = values.description.trim();

  if (workout.mesocycleId) {
    input.mesocycleId = workout.mesocycleId;
    input.mesocycleWeekNumber = workout.mesocycleWeekNumber;
  } else {
    // The form holds the week's Monday; the week is sent as its Wednesday.
    input.workoutDate = weekMiddleOf(values.workoutDate);
  }

  if (description !== "") {
    input.description = description;
  }
  return input;
}

/** The dialog open over the screen. Only one at a time: choosing from the catalog hands over to the configuration. */
type OpenDialog =
  | { kind: "none" }
  | { kind: "catalog" }
  | { kind: "configure"; item: WorkoutExerciseItem; adding: boolean };

/**
 * A catalog exercise chosen to be added: no effort, no rest, not per side and no sets yet; they are defined in the
 * configuration dialog.
 */
function newItemOf(exercise: Exercise): WorkoutExerciseItem {
  return {
    workoutExercise: {
      id: `new-${exercise.id}`,
      exerciseId: exercise.id,
      effort: "",
      perSide: false,
      restMinutes: null,
      sets: [],
      superSeriesId: null,
    },
    exercise,
  };
}

/** The entries sorted by their place in `ids`; any entry not in `ids` goes last, in its current order. */
function inOrderOf(entries: WorkoutExerciseDetail[], ids: string[]) {
  const placeOf = (entry: WorkoutExerciseDetail) => {
    const index = ids.indexOf(entry.id);
    return index === -1 ? ids.length : index;
  };
  return [...entries].sort((a, b) => placeOf(a) - placeOf(b));
}

/** The editor once both reads are in: it owns the workout's form, its save, the exercises and the open dialog. */
function LoadedWorkout({
  workout,
  exercises,
  favorites,
}: {
  workout: WorkoutById;
  exercises: Exercise[];
  favorites: ExerciseFavoritesState;
}) {
  const { t: tc } = useTranslation();
  const { t } = useTranslation("workouts");
  const { profile } = useCurrentUser();
  const propertiesTitleId = useId();
  const exercisesTitleId = useId();
  const { saving, errorCode, saved, save, clearSaved } = useUpdateWorkout();
  const mesocycle = useWorkoutMesocycle(workout.mesocycleId);
  const [values, setValues] = useState<WorkoutDetailsFormValues>({
    name: workout.name,
    description: workout.description ?? "",
    workoutDate: weekStartOf(workout.workoutDate),
  });
  const [errors, setErrors] = useState<WorkoutDetailsFormViolations>({});
  const filters = useExerciseCatalogFilters();
  const [dialog, setDialog] = useState<OpenDialog>({ kind: "none" });
  // The workout's saved exercises, kept up to date with what each exercise save answers.
  const [savedExercises, setSavedExercises] = useState<WorkoutExerciseDetail[]>(workout.exercises);
  const removal = useRemoveWorkoutExercise();
  const reordering = useReorderWorkoutExercises();
  const addButtonRef = useRef<HTMLButtonElement>(null);
  // The button that started an add flow is gone once it ends, so the focus goes back to Añadir ejercicio.
  const addFlowEnded = useRef(false);

  useEffect(() => {
    if (dialog.kind === "none" && addFlowEnded.current) {
      addFlowEnded.current = false;
      addButtonRef.current?.focus();
    }
  }, [dialog]);

  const catalogById = useMemo(
    () => new Map<string, Exercise>(exercises.map((exercise) => [exercise.id, exercise])),
    [exercises],
  );
  const singleMuscleBodyParts = useMemo(() => singleMuscleBodyPartsOf(exercises), [exercises]);
  const items = useMemo<WorkoutExerciseItem[]>(
    () =>
      savedExercises.map((workoutExercise) => ({
        workoutExercise,
        exercise: catalogById.get(workoutExercise.exerciseId),
      })),
    [savedExercises, catalogById],
  );
  // An exercise already in the workout leaves the catalog: it cannot be added twice.
  const available = useMemo(() => {
    const inWorkout = new Set(savedExercises.map((entry) => entry.exerciseId));
    return exercises.filter((exercise) => !inWorkout.has(exercise.id));
  }, [exercises, savedExercises]);

  function closeDialog() {
    if (dialog.kind === "configure" && dialog.adding) {
      addFlowEnded.current = true;
    }
    setDialog({ kind: "none" });
  }

  /** An added exercise joins the end of the list; an edited one replaces its entry. Either way the dialog closes. */
  function handleExerciseSaved(entry: WorkoutExerciseDetail, adding: boolean) {
    if (adding) {
      setSavedExercises((current) => [...current, entry]);
    } else {
      setSavedExercises((current) =>
        current.map((other) => (other.exerciseId === entry.exerciseId ? entry : other)),
      );
    }
    closeDialog();
  }

  /** The exercise is removed through the API; on success it returns to the catalog. */
  async function handleRemove(item: WorkoutExerciseItem) {
    const { id, exerciseId } = item.workoutExercise;

    if (await removal.remove(workout.id, id, exerciseId)) {
      setSavedExercises((current) => current.filter((other) => other.id !== id));
    }
  }

  /**
   * The dropped exercise moves at once and the full saved order is sent. A refused order is put back by entry id, so
   * an exercise saved or removed while the request was in flight is kept as it is.
   */
  async function handleReorder(activeId: string, overId: string) {
    const from = savedExercises.findIndex((entry) => entry.id === activeId);
    const to = savedExercises.findIndex((entry) => entry.id === overId);
    if (from === -1 || to === -1 || from === to) {
      return;
    }

    const previousIds = savedExercises.map((entry) => entry.id);
    const next = arrayMove(savedExercises, from, to);
    setSavedExercises(next);

    if (
      !(await reordering.reorder(
        workout.id,
        next.map((entry) => entry.exerciseId),
      ))
    ) {
      setSavedExercises((current) => inOrderOf(current, previousIds));
    }
  }

  function handleChange(field: keyof WorkoutDetailsFormValues, value: string) {
    setValues((current) => ({ ...current, [field]: value }));
    clearSaved();
  }

  async function handleSave() {
    const violations = validateWorkoutDetailsForm(values);
    setErrors(violations);
    if (Object.keys(violations).length > 0) {
      return;
    }

    await save(toUpdate(workout, values));
  }

  return (
    <>
      <WorkoutHeader backHref={backHrefOf(workout)} />
      <WorkoutContextStrip
        athlete={personCardOf(workout.athlete, tc("person.role.athlete"))}
        // Only an admin is shown who prepares the workout.
        trainer={
          profile?.role === "ADMIN" && workout.trainer
            ? personCardOf(workout.trainer, tc("person.role.trainer"))
            : null
        }
        mesocycle={mesocycleEntryOf(workout, mesocycle, (number) =>
          t("workout.context.week", { number }),
        )}
      />
      {errorCode && (
        <Alert severity="error" sx={{ mb: 3 }}>
          {tc(`errors:${errorCode}`)}
        </Alert>
      )}
      {saved && (
        <Alert severity="success" sx={{ mb: 3 }}>
          {t("workout.details.saved")}
        </Alert>
      )}
      <Box sx={{ display: "grid", gridTemplateColumns: "minmax(0, 1fr)", gap: 3 }}>
        <Box component="section" aria-labelledby={propertiesTitleId} sx={{ minWidth: 0 }}>
          <WorkoutDetailsCard
            values={values}
            errors={errors}
            saving={saving}
            onSave={() => void handleSave()}
            disabled={saving}
            weekLocked={Boolean(workout.mesocycleId)}
            onChange={handleChange}
            titleId={propertiesTitleId}
          />
        </Box>
        <Box component="section" aria-labelledby={exercisesTitleId} sx={{ minWidth: 0 }}>
          <WorkoutExercisesCard
            titleId={exercisesTitleId}
            onAdd={() => setDialog({ kind: "catalog" })}
            addButtonRef={addButtonRef}
          >
            <WorkoutExercisesList
              items={items}
              singleMuscleBodyParts={singleMuscleBodyParts}
              onEdit={(item) => setDialog({ kind: "configure", item, adding: false })}
              onRemove={(item) => void handleRemove(item)}
              removingId={removal.removingId}
              removeErrorCode={removal.errorCode}
              onReorder={(activeId, overId) => void handleReorder(activeId, overId)}
              reordering={reordering.reordering}
              reorderErrorCode={reordering.errorCode}
            />
          </WorkoutExercisesCard>
        </Box>
      </Box>
      <ExerciseCatalogDialog
        open={dialog.kind === "catalog"}
        exercises={available}
        filters={filters}
        currentUserId={profile?.id ?? null}
        singleMuscleBodyParts={singleMuscleBodyParts}
        favoriteIds={favorites.favoriteIds}
        favoritesStatus={favorites.status}
        pendingFavoriteIds={favorites.pendingIds}
        favoriteErrorCode={favorites.toggleErrorCode}
        onToggleFavorite={(exerciseId) => void favorites.toggle(exerciseId)}
        onChoose={(exercise) =>
          setDialog({ kind: "configure", item: newItemOf(exercise), adding: true })
        }
        onClose={closeDialog}
      />
      {dialog.kind === "configure" && (
        // Mounted per opening and keyed by the item, so each one starts from the stored values.
        <ExerciseConfigurationDialog
          key={dialog.item.workoutExercise.id}
          open
          item={dialog.item}
          adding={dialog.adding}
          workoutId={workout.id}
          onSaved={(entry) => handleExerciseSaved(entry, dialog.adding)}
          onClose={closeDialog}
        />
      )}
    </>
  );
}

function WorkoutEditor({ id }: { id: string | null }) {
  const workout = useWorkout(id);
  const catalog = useExercises();
  // Read beside the workout and the catalog, but it never blocks the screen: a failure leaves the stars off.
  const favorites = useExerciseFavorites();

  if (workout.status === "failed" && workout.errorCode === "NOT_FOUND") {
    return <WorkoutNotFound />;
  }
  if (workout.status === "failed" && workout.errorCode) {
    return <LoadError code={workout.errorCode} onRetry={workout.reload} />;
  }
  if (catalog.status === "failed" && catalog.errorCode) {
    return <LoadError code={catalog.errorCode} onRetry={catalog.reload} />;
  }
  if (workout.status !== "loaded" || catalog.status !== "loaded" || workout.workout === null) {
    return <HomeSkeleton />;
  }

  return (
    <LoadedWorkout workout={workout.workout} exercises={catalog.exercises} favorites={favorites} />
  );
}

/** Reads the id from the query string, which needs a `Suspense` boundary in a static export. */
function WorkoutScreen() {
  const id = useSearchParams().get("id");

  // Keyed by the id, so opening another workout starts from a clean screen.
  return <WorkoutEditor key={id ?? "none"} id={id} />;
}

export default function WorkoutView() {
  return (
    <AppShell>
      <Container maxWidth={false} sx={{ py: 4 }}>
        <Suspense fallback={<HomeSkeleton />}>
          <WorkoutScreen />
        </Suspense>
      </Container>
    </AppShell>
  );
}
