"use client";

import Box from "@mui/material/Box";
import type { WorkoutTemplate } from "@weight-tracker/core/domain";
import { useTranslation } from "react-i18next";
import List from "@/presentation/common/components/atoms/list";
import ListItem from "@/presentation/common/components/atoms/list-item";
import ListItemButton from "@/presentation/common/components/atoms/list-item-button";
import ListItemText from "@/presentation/common/components/atoms/list-item-text";
import Skeleton from "@/presentation/common/components/atoms/skeleton";
import Typography from "@/presentation/common/components/atoms/typography";
import LoadError from "@/presentation/common/components/molecules/load-error";
import type { WorkoutTemplateOptionsState } from "@/presentation/common/hooks/use-workout-template-options";

interface WorkoutTemplatePickerProps {
  options: WorkoutTemplateOptionsState;
  selectedId: string | null;
  onSelect: (template: WorkoutTemplate) => void;
  /** Shown instead of the list while nothing can be read yet, such as an admin without a trainer. */
  hint?: string;
  /** The message when no template is chosen on create. */
  error?: string;
  disabled?: boolean;
}

/** The list itself, or what stands in for it while it is not read, loading, failed or empty. */
function WorkoutTemplateOptions({
  options,
  selectedId,
  onSelect,
  hint,
  disabled = false,
}: Omit<WorkoutTemplatePickerProps, "error">) {
  const { t } = useTranslation();

  if (options.status === "idle") {
    return hint ? <Typography color="text.secondary">{hint}</Typography> : null;
  }
  if (options.status === "failed" && options.errorCode) {
    return <LoadError code={options.errorCode} onRetry={options.reload} />;
  }
  if (options.status === "loading") {
    return (
      <Box aria-busy="true">
        <Skeleton height={56} />
        <Skeleton height={56} />
      </Box>
    );
  }
  if (options.templates.length === 0) {
    return <Typography color="text.secondary">{t("workoutTemplatePicker.empty")}</Typography>;
  }

  return (
    <List aria-label={t("workoutTemplatePicker.listLabel")} disablePadding>
      {options.templates.map((template) => {
        const selected = template.id === selectedId;

        return (
          <ListItem key={template.id} disablePadding>
            <ListItemButton
              selected={selected}
              aria-pressed={selected}
              disabled={disabled}
              onClick={() => onSelect(template)}
            >
              <ListItemText
                primary={template.name}
                secondary={
                  <>
                    {template.description && (
                      <Box component="span" sx={{ display: "block" }}>
                        {template.description}
                      </Box>
                    )}
                    <Box component="span" sx={{ display: "block" }}>
                      {t("workoutTemplatePicker.exerciseCount", {
                        count: template.exercises.length,
                      })}
                    </Box>
                  </>
                }
              />
            </ListItemButton>
          </ListItem>
        );
      })}
    </List>
  );
}

/**
 * The templates a workout can be created from, one selectable row each with its name, description and number of
 * exercises, and the message when none is chosen. Presentational: the dialog reads them and holds the selection.
 */
export default function WorkoutTemplatePicker({ error, ...props }: WorkoutTemplatePickerProps) {
  return (
    <Box>
      <WorkoutTemplateOptions {...props} />
      {error && (
        <Typography color="error" variant="caption" role="alert">
          {error}
        </Typography>
      )}
    </Box>
  );
}
