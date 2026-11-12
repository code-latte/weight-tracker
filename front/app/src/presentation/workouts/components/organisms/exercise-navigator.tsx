import { useRef } from "react";
import { ScrollView, type ScrollViewInstance, StyleSheet, View } from "react-native";
import ExerciseNavigatorItem from "@/presentation/workouts/components/molecules/exercise-navigator-item";
import type { ExerciseProgress } from "@/presentation/workouts/components/molecules/exercise-progress-icon";

export interface ExerciseNavigatorOption {
  id: string;
  name: string;
  progress: ExerciseProgress;
  progressLabel: string;
}

interface ExerciseNavigatorProps {
  label: string;
  options: ExerciseNavigatorOption[];
  selectedId: string;
  onSelect: (id: string) => void;
}

/** The workout's exercises side by side, scrolling sideways, to jump to another one. The current one is kept in view. */
export default function ExerciseNavigator({
  label,
  options,
  selectedId,
  onSelect,
}: ExerciseNavigatorProps) {
  const scroll = useRef<ScrollViewInstance>(null);

  return (
    <ScrollView
      ref={scroll}
      horizontal
      accessibilityLabel={label}
      showsHorizontalScrollIndicator={false}
      style={styles.scroll}
      contentContainerStyle={styles.items}
    >
      {options.map((option) => {
        const selected = option.id === selectedId;

        return (
          <View
            key={option.id}
            onLayout={
              selected
                ? (event) =>
                    scroll.current?.scrollTo({
                      x: Math.max(0, event.nativeEvent.layout.x - 16),
                      animated: false,
                    })
                : undefined
            }
          >
            <ExerciseNavigatorItem
              name={option.name}
              progress={option.progress}
              progressLabel={option.progressLabel}
              selected={selected}
              onPress={() => onSelect(option.id)}
            />
          </View>
        );
      })}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  scroll: {
    flexGrow: 0,
  },
  items: {
    gap: 8,
    paddingHorizontal: 16,
    paddingVertical: 12,
  },
});
