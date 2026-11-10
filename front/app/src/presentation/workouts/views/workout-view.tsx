import { FlatList, Linking, StyleSheet, View } from "react-native";
import { router, useLocalSearchParams } from "expo-router";
import { useTranslation } from "react-i18next";
import SafeArea from "@/presentation/common/components/atoms/safe-area";
import ErrorState from "@/presentation/common/components/molecules/error-state";
import LoadingState from "@/presentation/common/components/molecules/loading-state";
import WorkoutExerciseRow from "@/presentation/workouts/components/molecules/workout-exercise-row";
import WorkoutHeroPanel from "@/presentation/workouts/components/molecules/workout-hero-panel";
import useWorkout from "@/presentation/workouts/hooks/use-workout";

const TYPE_KEYS: Record<string, string> = {
  STRENGTH: "strength",
  CARDIO: "cardio",
  STRETCHING: "stretching",
};

export default function WorkoutView() {
  const { t } = useTranslation("workouts");
  const { id } = useLocalSearchParams<{ id: string }>();
  const { state, retry, rows, done, total, mesocycleName, mesocycleWeekNumber } = useWorkout(id);

  const subtitle =
    mesocycleName !== null && mesocycleWeekNumber !== null
      ? t("workout.subtitle", { mesocycle: mesocycleName, week: mesocycleWeekNumber })
      : undefined;

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
        })}
      />

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
              const videoUrl = item.videoUrl;

              return (
                <WorkoutExerciseRow
                  name={item.name ?? t("workout.unknownExercise")}
                  progress={item.progress}
                  progressLabel={t(`workout.exerciseStatus.${item.progress}`)}
                  type={item.type}
                  typeLabel={typeKey === undefined ? "" : t(`workout.exerciseType.${typeKey}`)}
                  detailsLabel={t("workout.details")}
                  video={
                    videoUrl
                      ? { label: t("workout.video"), onPress: () => void Linking.openURL(videoUrl) }
                      : undefined
                  }
                />
              );
            }}
          />
        )}
      </View>
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
