"use client";

import { Suspense, useId, useState } from "react";
import ArrowBackOutlined from "@mui/icons-material/ArrowBackOutlined";
import DeleteOutlined from "@mui/icons-material/DeleteOutlined";
import Box from "@mui/material/Box";
import Container from "@mui/material/Container";
import {
  validateWorkoutTemplateDetailsForm,
  type WorkoutDetailsFormValues,
  type WorkoutTemplateDetailsFormViolations,
} from "@weight-tracker/core/application";
import type {
  Exercise,
  UpdateWorkoutTemplateInput,
  WorkoutTemplate,
} from "@weight-tracker/core/domain";
import NextLink from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useTranslation } from "react-i18next";
import Alert from "@/presentation/common/components/atoms/alert";
import Button from "@/presentation/common/components/atoms/button";
import Link from "@/presentation/common/components/atoms/link";
import Typography from "@/presentation/common/components/atoms/typography";
import HomeSkeleton from "@/presentation/common/components/molecules/home-skeleton";
import LoadError from "@/presentation/common/components/molecules/load-error";
import AppShell from "@/presentation/common/components/organisms/app-shell";
import useCurrentUser from "@/presentation/common/hooks/use-current-user";
import DeleteWorkoutTemplateDialog from "@/presentation/workouts/components/organisms/delete-workout-template-dialog";
import WorkoutDetailsCard from "@/presentation/workouts/components/organisms/workout-details-card";
import WorkoutExercisesEditor from "@/presentation/workouts/components/organisms/workout-exercises-editor";
import useDeleteWorkoutTemplate from "@/presentation/workouts/hooks/use-delete-workout-template";
import useExerciseFavorites, {
  type ExerciseFavoritesState,
} from "@/presentation/workouts/hooks/use-exercise-favorites";
import useExercises from "@/presentation/workouts/hooks/use-exercises";
import useExercisesEditor from "@/presentation/workouts/hooks/use-exercises-editor";
import useUpdateWorkoutTemplate from "@/presentation/workouts/hooks/use-update-workout-template";
import useWorkoutTemplate from "@/presentation/workouts/hooks/use-workout-template";

const LIST_HREF = "/workout-templates";

function WorkoutTemplateNotFound() {
  const { t } = useTranslation("workouts");

  return (
    <Box sx={{ py: 2 }}>
      <Typography sx={{ mb: 1 }}>{t("workoutTemplate.notFound")}</Typography>
      <Link component={NextLink} href={LIST_HREF}>
        {t("workoutTemplate.backToList")}
      </Link>
    </Box>
  );
}

/** The page heading, the way back to the list and the delete action. A template has no athlete, week or mesocycle. */
function WorkoutTemplateHeader({
  onDelete,
  deleting,
}: {
  onDelete: () => void;
  deleting: boolean;
}) {
  const { t } = useTranslation("workouts");

  return (
    <Box
      sx={{
        display: "flex",
        alignItems: "flex-start",
        justifyContent: "space-between",
        gap: 2,
        mb: 3,
      }}
    >
      <Box>
        <Typography variant="h4" component="h1">
          {t("workoutTemplate.title")}
        </Typography>
        <Link
          component={NextLink}
          href={LIST_HREF}
          underline="hover"
          sx={{ display: "inline-flex", alignItems: "center", gap: 0.5, mt: 0.5 }}
        >
          <ArrowBackOutlined fontSize="small" />
          {t("workoutTemplate.back")}
        </Link>
      </Box>
      <Button
        variant="outlined"
        color="error"
        startIcon={<DeleteOutlined />}
        onClick={onDelete}
        disabled={deleting}
      >
        {t("workoutTemplate.delete")}
      </Button>
    </Box>
  );
}

/** Builds the update from valid values: trimmed, and without a description when it is blank. */
function toUpdate(
  template: WorkoutTemplate,
  values: WorkoutDetailsFormValues,
): UpdateWorkoutTemplateInput {
  const input: UpdateWorkoutTemplateInput = { id: template.id, name: values.name.trim() };
  const description = values.description.trim();

  if (description !== "") {
    input.description = description;
  }
  return input;
}

/** The editor once the reads are in: it owns the template's form, its save, its deletion and the exercises. */
function LoadedWorkoutTemplate({
  template,
  exercises,
  favorites,
}: {
  template: WorkoutTemplate;
  exercises: Exercise[];
  favorites: ExerciseFavoritesState;
}) {
  const { t: tc } = useTranslation();
  const { t } = useTranslation("workouts");
  const router = useRouter();
  const { profile } = useCurrentUser();
  const propertiesTitleId = useId();
  const exercisesTitleId = useId();
  const { saving, errorCode, saved, save, clearSaved } = useUpdateWorkoutTemplate();
  const deletion = useDeleteWorkoutTemplate();
  const [confirmingDelete, setConfirmingDelete] = useState(false);
  // The card's values type carries a week; a template has none, so it stays empty and hidden.
  const [values, setValues] = useState<WorkoutDetailsFormValues>({
    name: template.name,
    description: template.description ?? "",
    workoutDate: "",
  });
  const [errors, setErrors] = useState<WorkoutTemplateDetailsFormViolations>({});
  const editor = useExercisesEditor("template", template.id, template.exercises, exercises);

  function handleChange(field: keyof WorkoutDetailsFormValues, value: string) {
    setValues((current) => ({ ...current, [field]: value }));
    clearSaved();
  }

  async function handleSave() {
    const violations = validateWorkoutTemplateDetailsForm(values);
    setErrors(violations);
    if (Object.keys(violations).length > 0) {
      return;
    }

    await save(toUpdate(template, values));
  }

  async function handleDelete() {
    const deleted = await deletion.remove(template.id);
    setConfirmingDelete(false);

    if (deleted) {
      router.push(LIST_HREF);
    }
  }

  const failure = errorCode ?? deletion.errorCode;

  return (
    <>
      <WorkoutTemplateHeader
        onDelete={() => setConfirmingDelete(true)}
        deleting={deletion.deleting}
      />
      {failure && (
        <Alert severity="error" sx={{ mb: 3 }}>
          {tc(`errors:${failure}`)}
        </Alert>
      )}
      {saved && (
        <Alert severity="success" sx={{ mb: 3 }}>
          {t("workoutTemplate.details.saved")}
        </Alert>
      )}
      <Box sx={{ display: "grid", gridTemplateColumns: "minmax(0, 1fr)", gap: 3 }}>
        <Box component="section" aria-labelledby={propertiesTitleId} sx={{ minWidth: 0 }}>
          <WorkoutDetailsCard
            title={t("workoutTemplate.details.title")}
            showWeek={false}
            values={values}
            errors={errors}
            saving={saving}
            onSave={() => void handleSave()}
            disabled={saving}
            onChange={handleChange}
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
      <DeleteWorkoutTemplateDialog
        open={confirmingDelete}
        pending={deletion.deleting}
        onCancel={() => setConfirmingDelete(false)}
        onConfirm={() => void handleDelete()}
      />
    </>
  );
}

function WorkoutTemplateEditor({ id }: { id: string | null }) {
  const template = useWorkoutTemplate(id);
  const catalog = useExercises();
  // Read beside the template and the catalog, but it never blocks the screen: a failure leaves the stars off.
  const favorites = useExerciseFavorites();

  if (template.status === "failed" && template.errorCode === "NOT_FOUND") {
    return <WorkoutTemplateNotFound />;
  }
  if (template.status === "failed" && template.errorCode) {
    return <LoadError code={template.errorCode} onRetry={template.reload} />;
  }
  if (catalog.status === "failed" && catalog.errorCode) {
    return <LoadError code={catalog.errorCode} onRetry={catalog.reload} />;
  }
  if (template.status !== "loaded" || catalog.status !== "loaded" || template.template === null) {
    return <HomeSkeleton />;
  }

  return (
    <LoadedWorkoutTemplate
      template={template.template}
      exercises={catalog.exercises}
      favorites={favorites}
    />
  );
}

/** Reads the id from the query string, which needs a `Suspense` boundary in a static export. */
function WorkoutTemplateScreen() {
  const id = useSearchParams().get("id");

  // Keyed by the id, so opening another template starts from a clean screen.
  return <WorkoutTemplateEditor key={id ?? "none"} id={id} />;
}

export default function WorkoutTemplateView() {
  return (
    <AppShell>
      <Container maxWidth={false} sx={{ py: 4 }}>
        <Suspense fallback={<HomeSkeleton />}>
          <WorkoutTemplateScreen />
        </Suspense>
      </Container>
    </AppShell>
  );
}
