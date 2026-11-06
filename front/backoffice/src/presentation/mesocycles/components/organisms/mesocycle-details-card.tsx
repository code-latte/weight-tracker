"use client";

import Box from "@mui/material/Box";
import {
  type CreateMesocycleFormValues,
  type CreateMesocycleFormViolations,
  MESOCYCLE_DURATION_MAX,
  MESOCYCLE_DURATION_MIN,
  MESOCYCLE_NAME_MAX_LENGTH,
  MESOCYCLE_OBJECTIVE_MAX_LENGTH,
} from "@weight-tracker/core/application";
import type { DomainErrorCode } from "@weight-tracker/core/domain";
import { useTranslation } from "react-i18next";
import Alert from "@/presentation/common/components/atoms/alert";
import Button from "@/presentation/common/components/atoms/button";
import Card from "@/presentation/common/components/atoms/card";
import CardContent from "@/presentation/common/components/atoms/card-content";
import TextField from "@/presentation/common/components/atoms/text-field";
import Typography from "@/presentation/common/components/atoms/typography";
import PersonPicker from "@/presentation/common/components/molecules/person-picker";
import type { Assignee } from "@/presentation/common/hooks/use-assignees";
import DurationField from "@/presentation/mesocycles/components/molecules/duration-field";

type Field = keyof CreateMesocycleFormValues;

/** Who the mesocycle is for, chosen while creating it. */
export interface MesocyclePeoplePickers {
  athletes: Assignee[];
  /** Only an admin chooses the trainer; `null` leaves the trainer picker out. */
  trainers: Assignee[] | null;
  /** The people are still loading or failed: the pickers and the save action wait. */
  ready: boolean;
  errorCode: DomainErrorCode | null;
  onRetry: () => void;
}

/** Who the mesocycle is for, shown once it exists. */
export interface MesocyclePeopleNames {
  athlete: string;
  /** `null` leaves the trainer out. */
  trainer: string | null;
}

interface MesocycleDetailsCardProps {
  values: CreateMesocycleFormValues;
  errors: CreateMesocycleFormViolations;
  onChange: (field: Field, value: string) => void;
  onSave: () => void;
  /** A save is in progress: the fields and the save action are disabled. */
  saving: boolean;
  /** The pickers while creating, or the names once the mesocycle exists. */
  people:
    | { kind: "pickers"; pickers: MesocyclePeoplePickers }
    | { kind: "names"; names: MesocyclePeopleNames };
  errorCode: DomainErrorCode | null;
  saved: boolean;
  titleId: string;
}

const maxLengthOf: Partial<Record<Field, number>> = {
  name: MESOCYCLE_NAME_MAX_LENGTH,
  objective: MESOCYCLE_OBJECTIVE_MAX_LENGTH,
};

/** One read-only line: a label above a value. */
function NameLine({ label, value }: { label: string; value: string }) {
  return (
    <Box>
      <Typography variant="caption" color="text.secondary" component="p">
        {label}
      </Typography>
      <Typography>{value}</Typography>
    </Box>
  );
}

/**
 * The mesocycle's general information: who it is for, its name, start, duration and objective, with the save action.
 * The screen owns and saves the values.
 */
export default function MesocycleDetailsCard({
  values,
  errors,
  onChange,
  onSave,
  saving,
  people,
  errorCode,
  saved,
  titleId,
}: MesocycleDetailsCardProps) {
  const { t } = useTranslation("mesocycles");
  const { t: tc } = useTranslation();
  const creating = people.kind === "pickers";

  function messageOf(name: Field): string | undefined {
    const violation = errors[name];

    return violation
      ? t(`mesocycle.validation.${violation}`, {
          max: name === "durationInWeeks" ? MESOCYCLE_DURATION_MAX : maxLengthOf[name],
          min: MESOCYCLE_DURATION_MIN,
        })
      : undefined;
  }

  function field(name: Field) {
    const message = messageOf(name);
    const max = maxLengthOf[name];

    return {
      label: t(`mesocycle.fields.${name === "durationInWeeks" ? "duration" : name}`),
      value: values[name],
      disabled: saving,
      required: true,
      onChange: (event: { target: { value: string } }) => onChange(name, event.target.value),
      error: Boolean(message),
      // The message replaces the counter while there is one.
      helperText: message ?? (max ? `${values[name].length} / ${max}` : undefined),
    };
  }

  function picker(name: "athleteId" | "trainerId", requiredMessage: string, ready: boolean) {
    const violation = errors[name];

    return {
      label: t(`mesocycle.fields.${name === "athleteId" ? "athlete" : "trainer"}`),
      value: values[name],
      onChange: (id: string) => onChange(name, id),
      disabled: saving || !ready,
      error: violation
        ? violation === "required"
          ? requiredMessage
          : t(`mesocycle.validation.${violation}`)
        : undefined,
    };
  }

  return (
    <Card variant="outlined">
      <CardContent>
        <Typography id={titleId} variant="h6" component="h2" sx={{ mb: 2 }}>
          {t("mesocycle.details.title")}
        </Typography>
        <Box sx={{ display: "flex", flexDirection: "column", gap: 2 }}>
          {people.kind === "pickers" && people.pickers.errorCode && (
            <Alert
              severity="error"
              action={
                <Button color="inherit" size="small" onClick={people.pickers.onRetry}>
                  {t("mesocycle.retry")}
                </Button>
              }
            >
              {tc(`errors:${people.pickers.errorCode}`)}
            </Alert>
          )}
          {errorCode && <Alert severity="error">{tc(`errors:${errorCode}`)}</Alert>}
          {saved && <Alert severity="success">{t("mesocycle.saved")}</Alert>}
          {people.kind === "pickers" ? (
            <Box
              sx={{
                display: "grid",
                gridTemplateColumns: { xs: "1fr", md: "repeat(2, minmax(0, 1fr))" },
                gap: 2,
              }}
            >
              <PersonPicker
                {...picker(
                  "athleteId",
                  t("mesocycle.validation.athleteRequired"),
                  people.pickers.ready,
                )}
                options={people.pickers.athletes}
                noOptionsText={t("mesocycle.noAthletes")}
              />
              {people.pickers.trainers && (
                <PersonPicker
                  {...picker(
                    "trainerId",
                    t("mesocycle.validation.trainerRequired"),
                    people.pickers.ready,
                  )}
                  options={people.pickers.trainers}
                  noOptionsText={t("mesocycle.noTrainers")}
                />
              )}
            </Box>
          ) : (
            <Box sx={{ display: "flex", flexWrap: "wrap", gap: 4 }}>
              <NameLine label={t("mesocycle.fields.athlete")} value={people.names.athlete} />
              {people.names.trainer !== null && (
                <NameLine label={t("mesocycle.fields.trainer")} value={people.names.trainer} />
              )}
            </Box>
          )}
          <Box
            sx={{
              display: "grid",
              gridTemplateColumns: { xs: "1fr", md: "repeat(2, minmax(0, 1fr))" },
              gap: 2,
            }}
          >
            <Box sx={{ display: "flex", flexDirection: "column", gap: 2, minWidth: 0 }}>
              <TextField {...field("name")} />
              <Box sx={{ display: "flex", flexWrap: "wrap", gap: 2 }}>
                <TextField
                  {...field("startDate")}
                  type="date"
                  slotProps={{ inputLabel: { shrink: true } }}
                />
                <DurationField
                  label={field("durationInWeeks").label}
                  value={values.durationInWeeks}
                  onChange={(value) => onChange("durationInWeeks", value)}
                  min={MESOCYCLE_DURATION_MIN}
                  max={MESOCYCLE_DURATION_MAX}
                  decreaseLabel={t("mesocycle.duration.decrease")}
                  increaseLabel={t("mesocycle.duration.increase")}
                  disabled={saving}
                  error={messageOf("durationInWeeks")}
                />
              </Box>
            </Box>
            <TextField {...field("objective")} multiline minRows={4} />
          </Box>
          <Box sx={{ display: "flex", justifyContent: "flex-end" }}>
            <Button
              variant="contained"
              disabled={saving || (people.kind === "pickers" && !people.pickers.ready)}
              onClick={onSave}
            >
              {t(creating ? "mesocycle.details.create" : "mesocycle.details.save")}
            </Button>
          </Box>
        </Box>
      </CardContent>
    </Card>
  );
}
