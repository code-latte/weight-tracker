import { useState } from "react";
import { StyleSheet, View } from "react-native";
import Card from "@/presentation/common/components/atoms/card";
import HelperText from "@/presentation/common/components/atoms/helper-text";
import Icon from "@/presentation/common/components/atoms/icon";
import IconButton from "@/presentation/common/components/atoms/icon-button";
import Text from "@/presentation/common/components/atoms/text";
import TextInput from "@/presentation/common/components/atoms/text-input";
import { useAppTheme } from "@/presentation/common/theme";
import type { SetRow, SetValues } from "@/presentation/workouts/set-rows";

interface SetRecordRowProps {
  row: SetRow;
  targetLabel: string;
  /** The row's texts, already translated. */
  labels: {
    reps: string;
    weight: string;
    save: string;
    saved: string;
    repsRequired: string;
    weightInvalid: string;
  };
  /** Inputs disabled and no way to save: the workout does not take set records now. */
  readOnly: boolean;
  saving: boolean;
  /** The last failed save of this row. */
  errorMessage?: string;
  onSave: (values: SetValues) => void;
  /** An input of the row got the focus: the screen can bring the row into view. */
  onFocus?: () => void;
}

const WHOLE_NUMBER = /^\d+$/;
const WEIGHT = /^\d+(\.\d{1,2})?$/;

/** A typed weight as the record would hold it, so "62,50" and 62.5 compare equal. */
function normalizeWeight(value: string): string {
  const typed = value.trim().replace(",", ".");

  return typed === "" || !WEIGHT.test(typed) ? typed : String(Number(typed));
}

/**
 * One prescribed set: its number beside a card with its target, what the athlete reports for it and a save action. The
 * action is a tick while the inputs match the saved record and a save button otherwise. The inputs start from the
 * saved record, so the parent gives the row a `key` that changes with its record.
 */
export default function SetRecordRow({
  row,
  targetLabel,
  labels,
  readOnly,
  saving,
  errorMessage,
  onSave,
  onFocus,
}: SetRecordRowProps) {
  const theme = useAppTheme();
  const savedReps = row.record?.reps == null ? "" : String(row.record.reps);
  const savedWeight = row.record?.weight == null ? "" : String(Number(row.record.weight));
  const [reps, setReps] = useState(savedReps);
  const [weight, setWeight] = useState(savedWeight);
  const [violation, setViolation] = useState<string | null>(null);

  const matchesRecord =
    row.record !== null &&
    (row.kind !== "range" || reps.trim() === savedReps) &&
    (row.kind === "timed" || normalizeWeight(weight) === savedWeight);

  function save() {
    const typedWeight = weight.trim().replace(",", ".");
    if (row.kind === "range" && (!WHOLE_NUMBER.test(reps.trim()) || Number(reps) < 1)) {
      setViolation(labels.repsRequired);
      return;
    }
    if (typedWeight !== "" && !WEIGHT.test(typedWeight)) {
      setViolation(labels.weightInvalid);
      return;
    }
    setViolation(null);
    onSave({
      reps: row.kind === "range" ? Number(reps) : null,
      weight: typedWeight === "" ? null : Number(typedWeight),
    });
  }

  const message = violation ?? errorMessage;

  return (
    <View style={styles.item}>
      <Text variant="headlineMedium" style={[styles.number, { color: theme.colors.secondary }]}>
        {row.number}
      </Text>
      <Card mode="outlined" style={styles.card}>
        <View style={styles.row}>
          <Text variant="bodyMedium" style={styles.target}>
            {targetLabel}
          </Text>

          {row.kind === "range" && (
            <TextInput
              dense
              mode="flat"
              label={labels.reps}
              accessibilityLabel={labels.reps}
              value={reps}
              onChangeText={setReps}
              onFocus={onFocus}
              keyboardType="number-pad"
              disabled={readOnly}
              style={styles.input}
            />
          )}
          {row.kind !== "timed" && (
            <TextInput
              dense
              mode="flat"
              label={labels.weight}
              accessibilityLabel={labels.weight}
              value={weight}
              onChangeText={setWeight}
              onFocus={onFocus}
              keyboardType="decimal-pad"
              disabled={readOnly}
              style={styles.input}
            />
          )}

          <View style={styles.action}>
            {matchesRecord ? (
              <Icon
                source="check-circle"
                tone="success"
                size={28}
                accessibilityLabel={labels.saved}
              />
            ) : (
              !readOnly && (
                <IconButton
                  icon="content-save"
                  accessibilityLabel={labels.save}
                  onPress={save}
                  loading={saving}
                  disabled={saving}
                />
              )
            )}
          </View>
        </View>
        {message !== undefined && (
          <HelperText type="error" visible style={styles.message}>
            {message}
          </HelperText>
        )}
      </Card>
    </View>
  );
}

const styles = StyleSheet.create({
  item: {
    flexDirection: "row",
    alignItems: "center",
    gap: 12,
  },
  number: {
    minWidth: 32,
    textAlign: "center",
  },
  card: {
    flex: 1,
  },
  row: {
    flexDirection: "row",
    alignItems: "center",
    gap: 8,
    paddingHorizontal: 12,
    paddingVertical: 8,
  },
  target: {
    minWidth: 64,
  },
  input: {
    flex: 1,
    minWidth: 64,
    backgroundColor: "transparent",
  },
  action: {
    width: 48,
    alignItems: "center",
    justifyContent: "center",
    // Pinned to the right edge even when the row has no inputs to push it there.
    marginLeft: "auto",
  },
  message: {
    paddingHorizontal: 12,
  },
});
