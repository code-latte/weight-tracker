import { useEffect, useState } from "react";
import { SplashScreen, Stack } from "expo-router";
import { useFonts } from "expo-font";
import useSession from "@/presentation/auth/hooks/use-session";
import AppProviders from "@/presentation/common/components/app-providers";
import fonts from "@/presentation/common/fonts";
import useCurrentUser from "@/presentation/common/hooks/use-current-user";
import usePreferences from "@/presentation/common/hooks/use-preferences";
import onboardingSteps from "@/presentation/onboarding/steps";

SplashScreen.preventAutoHideAsync();

function RootNavigator() {
  const { status } = useSession();
  const [fontsLoaded, fontsError] = useFonts(fonts);
  const { ready: preferencesReady } = usePreferences();
  const { settled: profileSettled, onboardingPending } = useCurrentUser();
  const signedIn = status === "signedIn";
  // A font, preferences or profile failure only ends the wait: the app falls back to the system font, the device
  // settings and the onboarding.
  const ready =
    status !== "restoring" &&
    (fontsLoaded || fontsError !== null) &&
    preferencesReady &&
    (!signedIn || profileSettled);

  // The wait holds only the launch: a later profile read (after login or signup) keeps the signed-out screens up instead.
  const [launched, setLaunched] = useState(false);
  if (ready && !launched) {
    setLaunched(true);
  }

  useEffect(() => {
    if (launched) {
      SplashScreen.hide();
    }
  }, [launched]);

  if (!launched) {
    return null;
  }

  // Signed out, or signed in before the first profile read ends: the signed-out screens stay up.
  const showAuth = !signedIn || !profileSettled;
  // Where a closed group sends its URL, so it never falls through to the not-found screen: the open group's entry.
  const openEntry = showAuth ? "/login" : onboardingPending ? onboardingSteps[0].href : "/";

  return (
    <Stack screenOptions={{ headerShown: false }}>
      <Stack.Protected guard={!showAuth && !onboardingPending} redirectTo={openEntry}>
        <Stack.Screen name="(app)" />
      </Stack.Protected>
      <Stack.Protected guard={!showAuth && onboardingPending} redirectTo={openEntry}>
        <Stack.Screen name="(onboarding)" />
      </Stack.Protected>
      <Stack.Protected guard={showAuth} redirectTo={openEntry}>
        <Stack.Screen name="(auth)" />
      </Stack.Protected>
    </Stack>
  );
}

export default function RootLayout() {
  return (
    <AppProviders>
      <RootNavigator />
    </AppProviders>
  );
}
