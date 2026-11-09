import { Stack } from "expo-router";
import OnboardingProvider from "@/presentation/onboarding/context/onboarding-provider";

export default function OnboardingLayout() {
  return (
    <OnboardingProvider>
      <Stack screenOptions={{ headerShown: false }}>
        {/* The first step cannot be left by going back. */}
        <Stack.Screen name="name" options={{ gestureEnabled: false }} />
      </Stack>
    </OnboardingProvider>
  );
}
