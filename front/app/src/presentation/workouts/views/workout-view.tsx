import { useCallback, useRef, useState } from "react";
import { FlatList, StyleSheet, View } from "react-native";
import { router, useFocusEffect, useLocalSearchParams } from "expo-router";
import { useTranslation } from "react-i18next";
import HelperText from "@/presentation/common/components/atoms/helper-text";
import SafeArea from "@/presentation/common/components/atoms/safe-area";
import ConfirmDialog from "@/presentation/common/components/molecules/confirm-dialog";
import ErrorState from "@/presentation/common/components/molecules/error-state";
import LoadingState from "@/presentation/common/components/molecules/loading-state";
import WorkoutExerciseRow from "@/presentation/workouts/components/molecules/workout-exercise-row";
import WorkoutHeroPanel from "@/presentation/workouts/components/molecules/workout-hero-panel";
import useChangeWorkoutStatus from "@/presentation/workouts/hooks/use-change-workout-status";
import useSingleMuscleBodyParts from "@/presentation/workouts/hooks/use-single-muscle-body-parts";
import useWorkout from "@/presentation/workouts/hooks/use-workout";

const TYPE_KEYS: Record<string, string> = {
  STRENGTH: "strength",
  CARDIO: "cardio",
  STRETCHING: "stretching",
};

export default function WorkoutView() {
  const { t } = useTranslation("workouts");
  const { id } = useLocalSearchParams<{ id: string }>();
  const { state, retry, reload, rows, done, total, mesocycleName, mesocycleWeekNumber } =
    useWorkout(id);
  const { change, changing, errorCode: statusErrorCode } = useChangeWorkoutStatus(id);
  const [confirmingFinish, setConfirmingFinish] = useState(false);
  const singleMuscleBodyParts = useSingleMuscleBodyParts();

  // The stack keeps this screen mounted under an exercise's, so what was recorded there is read again on coming back.
  // The first focus is the mount, which already reads.
  const mounted = useRef(false);
  useFocusEffect(
    useCallback(() => {
      if (!mounted.current) {
        mounted.current = true;
        return;
      }
      reload();
    }, [reload]),
  );

  const subtitle =
    mesocycleName !== null && mesocycleWeekNumber !== null
      ? t("workout.subtitle", { mesocycle: mesocycleName, week: mesocycleWeekNumber })
      : undefined;

  const workoutStatus = state.status === "ready" ? state.workout.status : null;
  // Published: start it. Started: finish it, after confirming. Completed or reviewed: it says so. A draft has none.
  const startButton =
    workoutStatus === "PUBLISHED"
      ? {
          label: t("workout.start"),
          icon: "play",
          onPress: () =>
            void change("STARTED").then((changed) => {
              if (changed) {
                reload();
              }
            }),
          disabled: changing,
          loading: changing,
        }
      : workoutStatus === "STARTED"
        ? {
            label: t("workout.finish"),
            icon: "stop",
            onPress: () => setConfirmingFinish(true),
            disabled: changing,
            loading: false,
          }
        : workoutStatus === "COMPLETED" || workoutStatus === "REVIEWED"
          ? {
              label: t("workout.finished"),
              icon: "check",
              onPress: () => {},
              disabled: true,
              loading: false,
            }
          : undefined;

  function finish() {
    void change("COMPLETED").then((changed) => {
      setConfirmingFinish(false);
      if (changed) {
        reload();
      }
    });
  }

  return (
    <SafeArea edges={["left", "right"]}>
      <WorkoutHeroPanel
        backLabel={t("common:back")}
        onBack={() => router.back()}
        {...(state.status === "ready" && {
          name: state.workout.name,
          subtitle,
          progressLabel: t("workout.progress", { done, total }),
          progress: total === 0 ? 0 : done / total,
          start: startButton,
        })}
      />
      {statusErrorCode !== null && (
        <HelperText type="error" visible>
          {t(`errors:${statusErrorCode}`)}
        </HelperText>
      )}

      <View style={styles.list}>
        {state.status === "loading" && <LoadingState />}

        {state.status === "error" && (
          <ErrorState
            message={t(`errors:${state.errorCode}`)}
            retryLabel={t("retry")}
            onRetry={retry}
          />
        )}

        {state.status === "ready" && (
          <FlatList
            data={rows}
            keyExtractor={(row) => row.id}
            testID="workout-exercises"
            contentContainerStyle={styles.items}
            renderItem={({ item }) => {
              const typeKey = item.type === null ? undefined : TYPE_KEYS[item.type];

              return (
                <WorkoutExerciseRow
                  name={item.name ?? t("workout.unknownExercise")}
                  progress={item.progress}
                  progressLabel={t(`workout.exerciseStatus.${item.progress}`)}
                  type={item.type}
                  typeLabel={typeKey === undefined ? "" : t(`workout.exerciseType.${typeKey}`)}
                  detailsLabel={t("workout.details")}
                  chips={{
                    bodyPart: t(`bodyParts.${item.bodyPart}`, { defaultValue: item.bodyPart }),
                    muscle: singleMuscleBodyParts.has(item.bodyPart)
                      ? undefined
                      : t(`muscles.${item.muscle}`, { defaultValue: item.muscle }),
                  }}
                  onPress={() =>
                    router.push({
                      pathname: "/workouts/[id]/exercises/[exerciseId]",
                      params: { id, exerciseId: item.id },
                    })
                  }
                />
              );
            }}
          />
        )}
      </View>
      <ConfirmDialog
        visible={confirmingFinish}
        title={t("workout.finishDialog.title")}
        message={
          state.status === "ready" && state.workout.trainerId === null
            ? t("workout.finishDialog.messageNoTrainer")
            : t("workout.finishDialog.message")
        }
        confirmLabel={t("workout.finishDialog.confirm")}
        cancelLabel={t("workout.finishDialog.cancel")}
        loading={changing}
        onConfirm={finish}
        onCancel={() => setConfirmingFinish(false)}
      />
    </SafeArea>
  );
}

const styles = StyleSheet.create({
  list: {
    flex: 1,
  },
  items: {
    gap: 12,
    padding: 16,
  },
});
