import { useEffect } from "react";
import { SplashScreen, Stack } from "expo-router";
import { StatusBar } from "expo-status-bar";
import { useFonts } from "expo-font";
import useSession from "@/presentation/auth/hooks/use-session";
import AppProviders from "@/presentation/common/components/app-providers";
import fonts from "@/presentation/common/fonts";

SplashScreen.preventAutoHideAsync();

function RootNavigator() {
  const { status } = useSession();
  const [fontsLoaded, fontsError] = useFonts(fonts);
  // A font failure only ends the wait: the app falls back to the system font.
  const ready = status !== "restoring" && (fontsLoaded || fontsError !== null);

  useEffect(() => {
    if (ready) {
      SplashScreen.hide();
    }
  }, [ready]);

  if (!ready) {
    return null;
  }

  const signedIn = status === "signedIn";

  return (
    <Stack screenOptions={{ headerShown: false }}>
      <Stack.Protected guard={signedIn} redirectTo="/login">
        <Stack.Screen name="(app)" />
      </Stack.Protected>
      <Stack.Protected guard={!signedIn}>
        <Stack.Screen name="(auth)" />
      </Stack.Protected>
    </Stack>
  );
}

export default function RootLayout() {
  return (
    <AppProviders>
      <RootNavigator />
      <StatusBar style="auto" />
    </AppProviders>
  );
}
