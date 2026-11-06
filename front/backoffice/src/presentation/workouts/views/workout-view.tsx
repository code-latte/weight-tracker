"use client";

import { Suspense, useId, useState } from "react";
import ArrowBackOutlined from "@mui/icons-material/ArrowBackOutlined";
import Box from "@mui/material/Box";
import Container from "@mui/material/Container";
import {
  validateWorkoutDetailsForm,
  type WorkoutDetailsFormValues,
  type WorkoutDetailsFormViolations,
} from "@weight-tracker/core/application";
import type { Exercise, UpdateWorkoutInput, WorkoutById } from "@weight-tracker/core/domain";
import NextLink from "next/link";
import { useSearchParams } from "next/navigation";
import { useTranslation } from "react-i18next";
import Alert from "@/presentation/common/components/atoms/alert";
import Button from "@/presentation/common/components/atoms/button";
import Tooltip from "@/presentation/common/components/atoms/tooltip";
import Link from "@/presentation/common/components/atoms/link";
import Typography from "@/presentation/common/components/atoms/typography";
import { fullNameOf } from "@/presentation/common/full-name";
import { initialsOf } from "@/presentation/common/initials";
import type { PersonCardProps } from "@/presentation/common/components/molecules/person-card";
import HomeSkeleton from "@/presentation/common/components/molecules/home-skeleton";
import LoadError from "@/presentation/common/components/molecules/load-error";
import AppShell from "@/presentation/common/components/organisms/app-shell";
import useCurrentUser from "@/presentation/common/hooks/use-current-user";
import WorkoutDetailsCard from "@/presentation/workouts/components/organisms/workout-details-card";
import WorkoutContextStrip, {
  type WorkoutContextMesocycle,
} from "@/presentation/workouts/components/organisms/workout-context-strip";
import WorkoutExercisesEditor from "@/presentation/workouts/components/organisms/workout-exercises-editor";
import useExerciseFavorites, {
  type ExerciseFavoritesState,
} from "@/presentation/workouts/hooks/use-exercise-favorites";
import useExercises from "@/presentation/workouts/hooks/use-exercises";
import useExercisesEditor from "@/presentation/workouts/hooks/use-exercises-editor";
import useSaveWorkoutAsTemplate from "@/presentation/workouts/hooks/use-save-workout-as-template";
import useUpdateWorkout from "@/presentation/workouts/hooks/use-update-workout";
import useWorkout from "@/presentation/workouts/hooks/use-workout";
import useWorkoutMesocycle, {
  type WorkoutMesocycleState,
} from "@/presentation/workouts/hooks/use-workout-mesocycle";
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
  // The values as last stored: a template copies the stored workout, so it waits until the edits are saved.
  const [storedValues, setStoredValues] = useState(values);
  const asTemplate = useSaveWorkoutAsTemplate();
  const unsaved =
    values.name !== storedValues.name ||
    values.description !== storedValues.description ||
    values.workoutDate !== storedValues.workoutDate;
  const [errors, setErrors] = useState<WorkoutDetailsFormViolations>({});
  const editor = useExercisesEditor("workout", workout.id, workout.exercises, exercises);

  function handleChange(field: keyof WorkoutDetailsFormValues, value: string) {
    setValues((current) => ({ ...current, [field]: value }));
    clearSaved();
    asTemplate.clear();
  }

  async function handleSave() {
    const violations = validateWorkoutDetailsForm(values);
    setErrors(violations);
    if (Object.keys(violations).length > 0) {
      return;
    }

    if (await save(toUpdate(workout, values))) {
      setStoredValues(values);
    }
  }

  const saveAsTemplate = (
    <Tooltip title={unsaved ? t("workout.saveAsTemplateHint") : ""}>
      {/* A disabled button fires no events, so the hint hangs on its wrapper. */}
      <span>
        <Button
          variant="outlined"
          disabled={unsaved || asTemplate.saving || saving}
          onClick={() => void asTemplate.save(workout.id)}
        >
          {t("workout.saveAsTemplate")}
        </Button>
      </span>
    </Tooltip>
  );

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
      {asTemplate.errorCode && (
        <Alert severity="error" sx={{ mb: 3 }}>
          {tc(`errors:${asTemplate.errorCode}`)}
        </Alert>
      )}
      {asTemplate.createdId && (
        <Alert
          severity="success"
          sx={{ mb: 3 }}
          action={
            <Button
              component={NextLink}
              href={`/workout-template?id=${encodeURIComponent(asTemplate.createdId)}`}
              color="inherit"
              size="small"
            >
              {t("workout.viewTemplate")}
            </Button>
          }
        >
          {t("workout.savedAsTemplate")}
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
            secondaryAction={saveAsTemplate}
            titleId={propertiesTitleId}
          />
        </Box>
        <WorkoutExercisesEditor
          editor={editor}
          favorites={favorites}
          currentUserId={profile?.id ?? null}
          titleId={exercisesTitleId}
        />
      </Box>
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
