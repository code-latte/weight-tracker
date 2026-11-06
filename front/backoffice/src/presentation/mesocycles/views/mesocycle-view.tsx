"use client";

import { type ReactNode, Suspense, useId, useMemo, useState } from "react";
import ArrowBackOutlined from "@mui/icons-material/ArrowBackOutlined";
import Box from "@mui/material/Box";
import Container from "@mui/material/Container";
import {
  type CreateMesocycleFormValues,
  type CreateMesocycleFormViolations,
  type MesocycleWeekFormValues,
  validateCreateMesocycleForm,
  validateMesocycleDetailsForm,
  type WeekWorkoutFormValues,
} from "@weight-tracker/core/application";
import type {
  CreateMesocycleRequest,
  CreateWorkoutRequest,
  Mesocycle,
  MesocycleWeek,
  TrainingWeekType,
  UpdateMesocycleInput,
} from "@weight-tracker/core/domain";
import NextLink from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useTranslation } from "react-i18next";
import Link from "@/presentation/common/components/atoms/link";
import Typography from "@/presentation/common/components/atoms/typography";
import { fullNameOf } from "@/presentation/common/full-name";
import { initialsOf } from "@/presentation/common/initials";
import type { PersonCardProps } from "@/presentation/common/components/molecules/person-card";
import HomeSkeleton from "@/presentation/common/components/molecules/home-skeleton";
import LoadError from "@/presentation/common/components/molecules/load-error";
import AppShell from "@/presentation/common/components/organisms/app-shell";
import useAssignees from "@/presentation/common/hooks/use-assignees";
import useCurrentUser from "@/presentation/common/hooks/use-current-user";
import CreateWeekWorkoutDialog from "@/presentation/mesocycles/components/organisms/create-week-workout-dialog";
import MesocycleContextStrip from "@/presentation/mesocycles/components/organisms/mesocycle-context-strip";
import MesocycleDetailsCard from "@/presentation/mesocycles/components/organisms/mesocycle-details-card";
import MesocycleWeekDialog from "@/presentation/mesocycles/components/organisms/mesocycle-week-dialog";
import MesocycleWeeksCard from "@/presentation/mesocycles/components/organisms/mesocycle-weeks-card";
import useCreateMesocycle from "@/presentation/mesocycles/hooks/use-create-mesocycle";
import useCreateWeekWorkout from "@/presentation/mesocycles/hooks/use-create-week-workout";
import useMesocycle from "@/presentation/mesocycles/hooks/use-mesocycle";
import useMesocycleWorkouts from "@/presentation/mesocycles/hooks/use-mesocycle-workouts";
import useSaveMesocycleWeek from "@/presentation/mesocycles/hooks/use-save-mesocycle-week";
import useUpdateMesocycle from "@/presentation/mesocycles/hooks/use-update-mesocycle";
import { nextFreeWeekOf, sortedWeeks, weekNumberOf } from "@/presentation/mesocycles/weeks";

type Role = "ADMIN" | "TRAINER";

/** A new mesocycle starts empty, four weeks long. */
const emptyValues: CreateMesocycleFormValues = {
  name: "",
  objective: "",
  startDate: "",
  durationInWeeks: "4",
  athleteId: "",
  trainerId: "",
};

/** The person's full name, or their email when they have no name. */
function displayNameOf(person: Mesocycle["athlete"]): string {
  return fullNameOf(person) ?? person.email;
}

/** The person as their card in the strip shows them. */
function personCardOf(person: Mesocycle["athlete"], role: string): PersonCardProps {
  return { name: displayNameOf(person), initials: initialsOf(person), role };
}

/** The form values of a stored mesocycle; who it is for is not edited. */
function valuesOf(mesocycle: Mesocycle): CreateMesocycleFormValues {
  return {
    ...emptyValues,
    name: mesocycle.name,
    objective: mesocycle.objective,
    startDate: mesocycle.startDate,
    durationInWeeks: String(mesocycle.durationInWeeks),
  };
}

/** Builds the creation from valid values: trimmed, with the trainer only when the caller chooses one. */
function toRequest(
  values: CreateMesocycleFormValues,
  withTrainer: boolean,
): CreateMesocycleRequest {
  const request: CreateMesocycleRequest = {
    athleteId: values.athleteId.trim(),
    name: values.name.trim(),
    objective: values.objective.trim(),
    startDate: values.startDate,
    durationInWeeks: Number(values.durationInWeeks.trim()),
  };

  if (withTrainer) {
    request.trainerId = values.trainerId.trim();
  }
  return request;
}

/** Builds the update from valid values. The API replaces every editable property, so all four are sent. */
function toUpdate(id: string, values: CreateMesocycleFormValues): UpdateMesocycleInput {
  return {
    id,
    name: values.name.trim(),
    objective: values.objective.trim(),
    startDate: values.startDate,
    durationInWeeks: Number(values.durationInWeeks.trim()),
  };
}

/**
 * Builds a workout creation placed in one week of the mesocycle: who it is for comes from the mesocycle, the trainer
 * only when the caller chooses one, and no date, since the API dates it from the week.
 */
function toWeekWorkoutRequest(
  mesocycle: Mesocycle,
  weekNumber: number,
  values: WeekWorkoutFormValues,
  withTrainer: boolean,
): CreateWorkoutRequest {
  const request: CreateWorkoutRequest = {
    name: values.name,
    userId: mesocycle.athleteId,
    mesocycleId: mesocycle.id,
    mesocycleWeekNumber: weekNumber,
  };

  if (values.description !== "") {
    request.description = values.description;
  }
  if (withTrainer) {
    request.trainerId = mesocycle.trainerId;
  }
  return request;
}

/** The page heading, its line and the way back to the list. */
function MesocycleHeader({ creating }: { creating: boolean }) {
  const { t } = useTranslation("mesocycles");

  return (
    <Box sx={{ mb: 3 }}>
      <Link
        component={NextLink}
        href="/assigned-plans"
        underline="hover"
        sx={{ display: "inline-flex", alignItems: "center", gap: 0.5, mb: 1 }}
      >
        <ArrowBackOutlined fontSize="small" />
        {t("mesocycle.back")}
      </Link>
      <Typography variant="h4" component="h1">
        {t(creating ? "mesocycle.createTitle" : "mesocycle.editTitle")}
      </Typography>
      <Typography color="text.secondary" sx={{ mt: 0.5 }}>
        {t("mesocycle.subtitle")}
      </Typography>
    </Box>
  );
}

function MesocycleNotFound() {
  const { t } = useTranslation("mesocycles");

  return (
    <Box sx={{ py: 2 }}>
      <Typography sx={{ mb: 1 }}>{t("mesocycle.notFound")}</Typography>
      <Link component={NextLink} href="/assigned-plans">
        {t("mesocycle.backToList")}
      </Link>
    </Box>
  );
}

/** The two cards, one above the other at full width, each labelled by its title. */
function MesocycleCards({
  details,
  weeks,
}: {
  details: (titleId: string) => ReactNode;
  weeks: (titleId: string) => ReactNode;
}) {
  const detailsTitleId = useId();
  const weeksTitleId = useId();

  return (
    <Box sx={{ display: "grid", gridTemplateColumns: "minmax(0, 1fr)", gap: 3 }}>
      <Box component="section" aria-labelledby={detailsTitleId} sx={{ minWidth: 0 }}>
        {details(detailsTitleId)}
      </Box>
      <Box component="section" aria-labelledby={weeksTitleId} sx={{ minWidth: 0 }}>
        {weeks(weeksTitleId)}
      </Box>
    </Box>
  );
}

/** Create mode: who the mesocycle is for and its data; the weeks wait until it exists. */
function CreateMesocycle({ role }: { role: Role }) {
  const router = useRouter();
  const { creating, errorCode, create } = useCreateMesocycle();
  const assignees = useAssignees(role);
  const [values, setValues] = useState<CreateMesocycleFormValues>(emptyValues);
  const [errors, setErrors] = useState<CreateMesocycleFormViolations>({});
  // Mirrors whom the API requires a trainer from; the API keeps the rule.
  const withTrainer = role === "ADMIN";

  function handleChange(field: keyof CreateMesocycleFormValues, value: string) {
    setValues((current) => ({ ...current, [field]: value }));
    setErrors((current) => ({ ...current, [field]: undefined }));
  }

  async function handleSave() {
    const violations = validateCreateMesocycleForm(values, { withTrainer });
    setErrors(violations);
    if (Object.keys(violations).length > 0) {
      return;
    }

    const mesocycle = await create(toRequest(values, withTrainer));
    if (mesocycle) {
      router.replace(`/mesocycle?id=${encodeURIComponent(mesocycle.id)}`);
    }
  }

  return (
    <>
      <MesocycleHeader creating />
      <MesocycleCards
        details={(titleId) => (
          <MesocycleDetailsCard
            titleId={titleId}
            values={values}
            errors={errors}
            onChange={handleChange}
            onSave={() => void handleSave()}
            saving={creating}
            people={{
              kind: "pickers",
              pickers: {
                athletes: assignees.athletes,
                trainers: withTrainer ? assignees.trainers : null,
                ready: assignees.status === "loaded",
                errorCode: assignees.status === "failed" ? assignees.errorCode : null,
                onRetry: assignees.reload,
              },
            }}
            errorCode={errorCode}
            saved={false}
          />
        )}
        weeks={(titleId) => (
          <MesocycleWeeksCard
            titleId={titleId}
            weeks={[]}
            locked
            canAdd={false}
            onAdd={() => {}}
            onEdit={() => {}}
          />
        )}
      />
    </>
  );
}

/** The dialog open over the screen: a new week to define, a stored one to redefine, or a workout for a week. */
type OpenDialog =
  | { kind: "none" }
  | { kind: "define"; weekNumber: number }
  | { kind: "redefine"; week: MesocycleWeek }
  | { kind: "workout"; weekNumber: number };

/** Edit mode once the mesocycle is read: its form, its save, its weeks and the open week dialog. */
function LoadedMesocycle({
  mesocycle,
  role,
  onReplace,
}: {
  mesocycle: Mesocycle;
  role: Role;
  onReplace: (mesocycle: Mesocycle) => void;
}) {
  const router = useRouter();
  const { t: tc } = useTranslation();
  const { saving, errorCode, saved, save, clearSaved } = useUpdateMesocycle();
  const week = useSaveMesocycleWeek();
  const workouts = useMesocycleWorkouts(mesocycle.id);
  const weekWorkout = useCreateWeekWorkout();
  const [values, setValues] = useState<CreateMesocycleFormValues>(() => valuesOf(mesocycle));
  const [errors, setErrors] = useState<CreateMesocycleFormViolations>({});
  const [dialog, setDialog] = useState<OpenDialog>({ kind: "none" });
  const weeks = useMemo(() => sortedWeeks(mesocycle.weeks), [mesocycle.weeks]);
  const nextWeek = nextFreeWeekOf(mesocycle.weeks, Number(mesocycle.durationInWeeks));

  function handleChange(field: keyof CreateMesocycleFormValues, value: string) {
    setValues((current) => ({ ...current, [field]: value }));
    setErrors((current) => ({ ...current, [field]: undefined }));
    clearSaved();
  }

  async function handleSave() {
    const violations = validateMesocycleDetailsForm(values);
    setErrors(violations);
    if (Object.keys(violations).length > 0) {
      return;
    }

    const updated = await save(toUpdate(mesocycle.id, values));
    if (updated) {
      onReplace(updated);
      setValues(valuesOf(updated));
    }
  }

  function closeDialog() {
    week.reset();
    weekWorkout.reset();
    setDialog({ kind: "none" });
  }

  async function handleWorkoutCreate(weekNumber: number, workoutValues: WeekWorkoutFormValues) {
    // Mirrors whom the API requires a trainer from; the API keeps the rule.
    const request = toWeekWorkoutRequest(mesocycle, weekNumber, workoutValues, role === "ADMIN");
    const workout = await weekWorkout.create(request);
    if (workout) {
      router.push(`/workout?id=${encodeURIComponent(workout.id)}`);
    }
  }

  async function handleWeekSave(weekValues: MesocycleWeekFormValues) {
    if (dialog.kind !== "define" && dialog.kind !== "redefine") {
      return;
    }
    const body = {
      mesocycleId: mesocycle.id,
      trainingWeekType: weekValues.trainingWeekType as TrainingWeekType,
      objective: weekValues.objective,
    };
    const updated = await week.save(
      dialog.kind === "define"
        ? { kind: "define", input: { ...body, weekNumber: dialog.weekNumber } }
        : { kind: "redefine", input: { ...body, weekNumber: weekNumberOf(dialog.week) } },
    );
    if (updated) {
      onReplace(updated);
      closeDialog();
    }
  }

  return (
    <>
      <MesocycleHeader creating={false} />
      <MesocycleContextStrip
        athlete={personCardOf(mesocycle.athlete, tc("person.role.athlete"))}
        // Only an admin is shown who prepares the mesocycle.
        trainer={
          role === "ADMIN" ? personCardOf(mesocycle.trainer, tc("person.role.trainer")) : null
        }
      />
      <MesocycleCards
        details={(titleId) => (
          <MesocycleDetailsCard
            titleId={titleId}
            values={values}
            errors={errors}
            onChange={handleChange}
            onSave={() => void handleSave()}
            saving={saving}
            people={{ kind: "none" }}
            errorCode={errorCode}
            saved={saved}
          />
        )}
        weeks={(titleId) => (
          <MesocycleWeeksCard
            titleId={titleId}
            weeks={weeks}
            locked={false}
            canAdd={nextWeek !== null}
            onAdd={() => nextWeek !== null && setDialog({ kind: "define", weekNumber: nextWeek })}
            onEdit={(stored) => setDialog({ kind: "redefine", week: stored })}
            workouts={workouts}
            onCreateWorkout={(weekNumber) => setDialog({ kind: "workout", weekNumber })}
          />
        )}
      />
      {dialog.kind === "workout" && (
        // Mounted per opening, so each one starts empty.
        <CreateWeekWorkoutDialog
          weekNumber={dialog.weekNumber}
          creating={weekWorkout.creating}
          errorCode={weekWorkout.errorCode}
          onCreate={(workoutValues) => void handleWorkoutCreate(dialog.weekNumber, workoutValues)}
          onClose={closeDialog}
        />
      )}
      {(dialog.kind === "define" || dialog.kind === "redefine") && (
        // Mounted per opening, so each one starts from the stored values.
        <MesocycleWeekDialog
          weekNumber={dialog.kind === "define" ? dialog.weekNumber : weekNumberOf(dialog.week)}
          initial={
            dialog.kind === "define"
              ? { trainingWeekType: "", objective: "" }
              : { trainingWeekType: dialog.week.trainingWeekType, objective: dialog.week.objective }
          }
          saving={week.saving}
          errorCode={week.errorCode}
          onSave={(weekValues) => void handleWeekSave(weekValues)}
          onClose={closeDialog}
        />
      )}
    </>
  );
}

function EditMesocycle({ id, role }: { id: string; role: Role }) {
  const { status, mesocycle, errorCode, reload, replace } = useMesocycle(id);

  if (status === "failed" && errorCode === "NOT_FOUND") {
    return <MesocycleNotFound />;
  }
  if (status === "failed" && errorCode) {
    return <LoadError code={errorCode} onRetry={reload} />;
  }
  if (status !== "loaded" || mesocycle === null) {
    return <HomeSkeleton />;
  }

  return <LoadedMesocycle mesocycle={mesocycle} role={role} onReplace={replace} />;
}

/** Reads the id from the query string, which needs a `Suspense` boundary in a static export. */
function MesocycleScreen() {
  const id = useSearchParams().get("id");
  const current = useCurrentUser();

  if (current.status === "failed" && current.errorCode) {
    return <LoadError code={current.errorCode} onRetry={current.reload} />;
  }
  // Waits for the role, so role-dependent fields never appear or disappear after rendering.
  const role = current.profile?.role;
  if (current.status !== "loaded" || (role !== "ADMIN" && role !== "TRAINER")) {
    return <HomeSkeleton />;
  }

  // Keyed by the id, so opening another mesocycle, or the one just created, starts from a clean screen.
  return id === null ? (
    <CreateMesocycle key="new" role={role} />
  ) : (
    <EditMesocycle key={id} id={id} role={role} />
  );
}

export default function MesocycleView() {
  return (
    <AppShell>
      <Container maxWidth={false} sx={{ py: 4 }}>
        <Suspense fallback={<HomeSkeleton />}>
          <MesocycleScreen />
        </Suspense>
      </Container>
    </AppShell>
  );
}
