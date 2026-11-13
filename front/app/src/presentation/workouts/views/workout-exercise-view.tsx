import { useEffect, useRef, useState } from "react";
import { FlatList, KeyboardAvoidingView, Linking, Platform, StyleSheet } from "react-native";
import { router, useLocalSearchParams } from "expo-router";
import { useTranslation } from "react-i18next";
import SafeArea from "@/presentation/common/components/atoms/safe-area";
import useKeyboardVisible from "@/presentation/common/hooks/use-keyboard-visible";
import ErrorState from "@/presentation/common/components/molecules/error-state";
import LoadingState from "@/presentation/common/components/molecules/loading-state";
import ExerciseHeroPanel from "@/presentation/workouts/components/molecules/exercise-hero-panel";
import RestCard from "@/presentation/workouts/components/molecules/rest-card";
import ExerciseNavigator from "@/presentation/workouts/components/organisms/exercise-navigator";
import SetRecordRow from "@/presentation/workouts/components/organisms/set-record-row";
import useSingleMuscleBodyParts from "@/presentation/workouts/hooks/use-single-muscle-body-parts";
import useRestCountdown from "@/presentation/workouts/hooks/use-rest-countdown";
import useSaveSetRecord from "@/presentation/workouts/hooks/use-save-set-record";
import useWorkoutExercise from "@/presentation/workouts/hooks/use-workout-exercise";
import {
  countdownTextOf,
  isRecordable,
  restSecondsOf,
  setTargetLabel,
} from "@/presentation/workouts/set-rows";

export default function WorkoutExerciseView() {
  const { t } = useTranslation("workouts");
  const { id, exerciseId } = useLocalSearchParams<{ id: string; exerciseId: string }>();
  const { status, errorCode, retry, reload, workoutName, workoutStatus, items, exercise } =
    useWorkoutExercise(id, exerciseId);
  const { save, savingNumber, errors } = useSaveSetRecord(id, exercise?.exerciseId ?? "");
  const readOnly = !isRecordable(workoutStatus);
  const restMinutes = exercise?.restMinutes ?? null;
  const countdown = useRestCountdown(
    exerciseId,
    restMinutes === null ? null : restSecondsOf(restMinutes),
  );
  const keyboardVisible = useKeyboardVisible();
  const singleMuscleBodyParts = useSingleMuscleBodyParts();
  const list = useRef<FlatList>(null);
  const [focusedIndex, setFocusedIndex] = useState<number | null>(null);

  // Once the keyboard is up (and the list has its extra room), the row being edited goes to the top of the list.
  useEffect(() => {
    if (!keyboardVisible || focusedIndex === null) {
      return;
    }
    const timer = setTimeout(
      () => list.current?.scrollToIndex({ index: focusedIndex, viewPosition: 0, animated: true }),
      100,
    );

    return () => clearTimeout(timer);
  }, [keyboardVisible, focusedIndex]);
  const video = exercise?.videoUrl;

  /** Swaps the exercise on this same screen: no transition, and back still returns to the workout. */
  function openExercise(next: string) {
    if (next !== exerciseId) {
      router.setParams({ exerciseId: next });
    }
  }

  function errorOf(number: number) {
    const code = errors[number];
    if (code === undefined) {
      return undefined;
    }

    return code === "CONFLICT" ? t("exercise.errors.CONFLICT") : t(`errors:${code}`);
  }

  return (
    <SafeArea edges={["left", "right"]}>
      <ExerciseHeroPanel
        backLabel={t("common:back")}
        onBack={() => router.back()}
        {...(exercise !== null && {
          workoutName: workoutName ?? undefined,
          name: exercise.name ?? t("workout.unknownExercise"),
          subtitle: exercise.subtitle ?? undefined,
          chips: {
            bodyPart: t(`bodyParts.${exercise.bodyPart}`, { defaultValue: exercise.bodyPart }),
            muscle: singleMuscleBodyParts.has(exercise.bodyPart)
              ? undefined
              : t(`muscles.${exercise.muscle}`, { defaultValue: exercise.muscle }),
          },
          video: video
            ? { label: t("workout.video"), onPress: () => void Linking.openURL(video) }
            : undefined,
        })}
      />

      {status === "ready" && exercise !== null && (
        <ExerciseNavigator
          label={t("exercise.navigator")}
          selectedId={exerciseId}
          onSelect={openExercise}
          options={items.map((item) => ({
            id: item.id,
            name: item.name ?? t("workout.unknownExercise"),
            progress: item.progress,
            progressLabel: t(`workout.exerciseStatus.${item.progress}`),
          }))}
        />
      )}

      <KeyboardAvoidingView
        style={styles.list}
        behavior={Platform.OS === "ios" ? "padding" : "height"}
      >
        {status === "loading" && <LoadingState />}

        {status === "error" && errorCode !== null && (
          <ErrorState message={t(`errors:${errorCode}`)} retryLabel={t("retry")} onRetry={retry} />
        )}

        {status === "ready" && exercise !== null && (
          <FlatList
            ref={list}
            data={exercise.rows}
            // The record is part of the key, so a row starts from what was saved once it has one.
            keyExtractor={(row) => `${row.number}-${row.record?.id ?? "new"}`}
            testID="exercise-sets"
            keyboardShouldPersistTaps="handled"
            keyboardDismissMode="on-drag"
            // With the keyboard up the list gets 50 px more at its end, so the last rows can scroll higher.
            contentContainerStyle={[styles.items, keyboardVisible && styles.itemsKeyboard]}
            onScrollToIndexFailed={() => {}}
            renderItem={({ item: row, index }) => {
              const target = setTargetLabel(row.target);

              return (
                <SetRecordRow
                  row={row}
                  targetLabel={t(target.key, target.values)}
                  labels={{
                    reps: t("exercise.reps"),
                    weight: t("exercise.weight"),
                    save: t("exercise.save"),
                    saved: t("exercise.saved"),
                    repsRequired: t("exercise.repsRequired"),
                    weightInvalid: t("exercise.weightInvalid"),
                  }}
                  readOnly={readOnly}
                  saving={savingNumber === row.number}
                  errorMessage={errorOf(row.number)}
                  onFocus={() => setFocusedIndex(index)}
                  onSave={(values) =>
                    void save(row, values).then((saved) => {
                      if (saved) {
                        reload();
                      }
                    })
                  }
                />
              );
            }}
          />
        )}

        {/* The rest band gives way to the keyboard, so the row being edited keeps the room. */}
        {status === "ready" && exercise !== null && !keyboardVisible && (
          <RestCard
            label={
              restMinutes === null
                ? t("exercise.noRest")
                : countdown.status === "finished"
                  ? t("exercise.restFinished")
                  : t("exercise.rest", { time: countdownTextOf(countdown.remainingSeconds) })
            }
            finished={countdown.status === "finished"}
            controls={
              restMinutes === null
                ? undefined
                : {
                    running: countdown.status === "running",
                    canPlay: countdown.status !== "finished",
                    canReset: countdown.status !== "idle",
                    onPlay: countdown.play,
                    onPause: countdown.pause,
                    onReset: countdown.reset,
                    playLabel: t("exercise.restPlay"),
                    pauseLabel: t("exercise.restPause"),
                    resetLabel: t("exercise.restReset"),
                  }
            }
          />
        )}
      </KeyboardAvoidingView>
    </SafeArea>
  );
}

const styles = StyleSheet.create({
  list: {
    flex: 1,
  },
  itemsKeyboard: {
    paddingBottom: 150,
  },
  items: {
    gap: 12,
    padding: 16,
    // Room to bring the last row above the keyboard.
    paddingBottom: 32,
  },
});
