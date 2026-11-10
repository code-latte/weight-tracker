import { FlatList, StyleSheet, View } from "react-native";
import { router } from "expo-router";
import { useTranslation } from "react-i18next";
import type { WorkoutListItem } from "@weight-tracker/core/domain";
import RefreshControl from "@/presentation/common/components/atoms/refresh-control";
import SafeArea from "@/presentation/common/components/atoms/safe-area";
import Text from "@/presentation/common/components/atoms/text";
import ErrorState from "@/presentation/common/components/molecules/error-state";
import LoadingState from "@/presentation/common/components/molecules/loading-state";
import { formatShortWeekday, formatWeekRange, weekYear } from "@/presentation/common/format/week";
import WeekNavigator from "@/presentation/workouts/components/molecules/week-navigator";
import WeekProgressCard from "@/presentation/workouts/components/molecules/week-progress-card";
import WorkoutCard from "@/presentation/workouts/components/molecules/workout-card";
import type { WorkoutProgress } from "@/presentation/workouts/components/molecules/workout-status-icon";
import useWeekWorkouts, { isDone } from "@/presentation/workouts/hooks/use-week-workouts";

/** Drafts the athlete sees are their own, not started yet: they read as pending, like published ones. */
function progressOf(status: string): WorkoutProgress {
  switch (status) {
    case "COMPLETED":
      return "completed";
    case "REVIEWED":
      return "reviewed";
    case "STARTED":
      return "started";
    default:
      return "pending";
  }
}

export default function WorkoutsView() {
  const { t, i18n } = useTranslation("workouts");
  const {
    state,
    retry,
    refreshing,
    refresh,
    weekStart,
    workouts,
    completed,
    total,
    isCurrentWeek,
    isNextWeek,
    previousWeek,
    nextWeek,
  } = useWeekWorkouts();

  const range = formatWeekRange(weekStart, i18n.language);
  const named = isCurrentWeek || isNextWeek;
  const title = isCurrentWeek ? t("week.current") : isNextWeek ? t("week.next") : range;

  const authorOf = (workout: WorkoutListItem) =>
    workout.trainer ? `${workout.trainer.name} ${workout.trainer.surname}` : t("author.me");

  return (
    <SafeArea edges={["left", "right"]} style={styles.container}>
      <WeekProgressCard
        title={title}
        subtitle={named ? range : weekYear(weekStart)}
        progressLabel={t("progress", { completed, total })}
        completed={completed}
        total={total}
      />

      <View style={styles.body}>
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
              data={workouts}
              keyExtractor={(workout) => workout.id}
              testID="workouts-list"
              refreshControl={<RefreshControl refreshing={refreshing} onRefresh={refresh} />}
              contentContainerStyle={styles.items}
              ListEmptyComponent={<Text variant="bodyLarge">{t("empty")}</Text>}
              renderItem={({ item }) => (
                <WorkoutCard
                  name={item.name}
                  author={authorOf(item)}
                  // The day the athlete finished it, not the planned one; a done workout without it shows "--".
                  day={
                    isDone(item) && item.completedAt
                      ? formatShortWeekday(item.completedAt, i18n.language)
                      : t("day.pending")
                  }
                  progress={progressOf(item.status)}
                  progressLabel={t(`status.${progressOf(item.status)}`)}
                  onPress={() =>
                    router.push({ pathname: "/workouts/[id]", params: { id: item.id } })
                  }
                />
              )}
            />
          )}
        </View>

        <WeekNavigator
          range={range}
          previousLabel={t("week.previous")}
          nextLabel={t("week.following")}
          onPrevious={previousWeek}
          onNext={nextWeek}
        />
      </View>
    </SafeArea>
  );
}

const styles = StyleSheet.create({
  container: {
    gap: 16,
  },
  body: {
    flex: 1,
    gap: 16,
    paddingHorizontal: 24,
  },
  list: {
    flex: 1,
  },
  items: {
    gap: 12,
  },
});
