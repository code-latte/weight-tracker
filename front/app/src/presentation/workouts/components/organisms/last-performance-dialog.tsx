import { StyleSheet, View } from "react-native";
import { useTranslation } from "react-i18next";
import type { ExerciseLastPerformance, LastPerformanceSet } from "@weight-tracker/core/domain";
import Button from "@/presentation/common/components/atoms/button";
import Card from "@/presentation/common/components/atoms/card";
import Dialog, {
  DialogActions,
  DialogContent,
  DialogTitle,
} from "@/presentation/common/components/atoms/dialog";
import Divider from "@/presentation/common/components/atoms/divider";
import Icon from "@/presentation/common/components/atoms/icon";
import Text from "@/presentation/common/components/atoms/text";
import { formatDay } from "@/presentation/common/format/week";
import LastPerformanceSetLine from "@/presentation/workouts/components/molecules/last-performance-set-line";
import LastPerformanceStatCard from "@/presentation/workouts/components/molecules/last-performance-stat-card";
import { effortKeysOf } from "@/presentation/workouts/effort";
import { highestWeightIndexOf } from "@/presentation/workouts/last-performance";

interface LastPerformanceDialogProps {
  visible: boolean;
  performance: ExerciseLastPerformance;
  /** The exercise's type, which picks the scale the effort is shown in. */
  exerciseType?: string;
  onClose: () => void;
}

/** What the athlete did the last time they finished a workout with the exercise: its date, effort and sets. */
export default function LastPerformanceDialog({
  visible,
  performance,
  exerciseType,
  onClose,
}: LastPerformanceDialogProps) {
  const { t, i18n } = useTranslation("workouts");

  /** Reps or seconds as the set has them, left out when it has neither. */
  function amountOf({ reps, durationSeconds }: LastPerformanceSet): string | undefined {
    if (reps != null) {
      return t("exercise.lastPerformance.reps", { count: Number(reps) });
    }

    return durationSeconds != null
      ? t("exercise.lastPerformance.seconds", { count: Number(durationSeconds) })
      : undefined;
  }

  /** A weight that was not tracked is "no weight", which is not the same as 0 kg. */
  function weightOf({ weight }: LastPerformanceSet): string {
    return weight != null
      ? t("exercise.lastPerformance.weight", { weight: Number(weight) })
      : t("exercise.lastPerformance.noWeight");
  }

  const { workoutDate, effort, sets } = performance;
  const highest = highestWeightIndexOf(sets);

  return (
    <Dialog visible={visible} onDismiss={onClose}>
      <DialogTitle>{t("exercise.lastPerformance.title")}</DialogTitle>
      <DialogContent>
        <View style={styles.performance} testID="last-performance-content">
          <View style={styles.date}>
            <Icon source="calendar-outline" size={18} />
            <Text variant="bodyMedium">{formatDay(workoutDate, i18n.language)}</Text>
          </View>
          <Divider />
          <View style={styles.stats}>
            <LastPerformanceStatCard
              label={t("exercise.lastPerformance.setsDone")}
              value={String(sets.length)}
            />
            {effort !== "" && (
              <LastPerformanceStatCard
                label={t("exercise.lastPerformance.effort")}
                value={t(effortKeysOf(effort, exerciseType), { defaultValue: effort })}
              />
            )}
          </View>
          <Text variant="titleSmall">{t("exercise.lastPerformance.setsRecorded")}</Text>
          <Card mode="outlined" testID="last-performance-sets">
            {sets.map((set, index) => (
              <View key={index} testID={`last-performance-set-${index + 1}`}>
                {index > 0 && <Divider />}
                <LastPerformanceSetLine
                  number={index + 1}
                  amount={amountOf(set)}
                  weight={weightOf(set)}
                  highestLabel={
                    index === highest ? t("exercise.lastPerformance.highest") : undefined
                  }
                />
              </View>
            ))}
          </Card>
        </View>
      </DialogContent>
      <DialogActions>
        <Button onPress={onClose}>{t("exercise.lastPerformance.close")}</Button>
      </DialogActions>
    </Dialog>
  );
}

const styles = StyleSheet.create({
  performance: {
    gap: 12,
  },
  date: {
    flexDirection: "row",
    alignItems: "center",
    gap: 8,
  },
  stats: {
    flexDirection: "row",
    gap: 12,
  },
});
