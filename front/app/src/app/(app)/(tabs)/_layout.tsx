import { Tabs } from "expo-router";
import { useTranslation } from "react-i18next";
import { Icon, useTheme } from "react-native-paper";

export default function TabsLayout() {
  const { t } = useTranslation("common");
  const theme = useTheme();

  // Both tint colours are Paper theme strings, so the ColorValue the tab bar passes back is always a string.
  return (
    <Tabs
      screenOptions={{
        headerShown: false,
        tabBarActiveTintColor: theme.colors.primary,
        tabBarInactiveTintColor: theme.colors.onSurfaceVariant,
        tabBarStyle: { backgroundColor: theme.colors.surface },
      }}
    >
      <Tabs.Screen
        name="index"
        options={{
          title: t("tabs.home"),
          tabBarIcon: ({ color, size }) => (
            <Icon source="home" color={color as string} size={size} />
          ),
        }}
      />
      <Tabs.Screen
        name="workouts"
        options={{
          title: t("tabs.workouts"),
          tabBarIcon: ({ color, size }) => (
            <Icon source="dumbbell" color={color as string} size={size} />
          ),
        }}
      />
      <Tabs.Screen
        name="progress"
        options={{
          title: t("tabs.progress"),
          tabBarIcon: ({ color, size }) => (
            <Icon source="chart-bar" color={color as string} size={size} />
          ),
        }}
      />
      <Tabs.Screen
        name="profile"
        options={{
          title: t("tabs.profile"),
          tabBarIcon: ({ color, size }) => (
            <Icon source="account" color={color as string} size={size} />
          ),
        }}
      />
    </Tabs>
  );
}
