"use client";

import { useEffect, type ReactNode } from "react";
import Box from "@mui/material/Box";
import { usePathname, useRouter } from "next/navigation";
import RequireSession from "@/presentation/auth/components/require-session";
import useCurrentUser from "@/presentation/common/hooks/use-current-user";
import useExerciseCatalog from "@/presentation/common/hooks/use-exercise-catalog";
import AppHeader from "@/presentation/common/components/organisms/app-header";
import NavigationDrawer from "@/presentation/common/components/organisms/navigation-drawer";

interface AppShellProps {
  children: ReactNode;
}

/**
 * Reads the profile and, in the background, the exercise catalog once the session is confirmed, and keeps a user in
 * onboarding on the home screen.
 */
function SignedInContent({ children }: AppShellProps) {
  const router = useRouter();
  const pathname = usePathname();
  const { load, onboardingPending } = useCurrentUser();
  const { load: loadExercises } = useExerciseCatalog();
  const redirecting = onboardingPending && pathname !== "/";

  useEffect(() => {
    load();
    loadExercises();
  }, [load, loadExercises]);

  useEffect(() => {
    if (redirecting) {
      router.replace("/");
    }
  }, [redirecting, router]);

  return (
    <Box sx={{ display: "flex", minHeight: "100vh" }}>
      <NavigationDrawer />
      <Box sx={{ display: "flex", flexDirection: "column", flexGrow: 1, minWidth: 0 }}>
        <AppHeader />
        <Box component="main" sx={{ flexGrow: 1, bgcolor: "background.default" }}>
          {redirecting ? null : children}
        </Box>
      </Box>
    </Box>
  );
}

/** Layout of every signed-in view: the session guard, the full-height drawer, and the header above the content. */
export default function AppShell({ children }: AppShellProps) {
  return (
    <RequireSession>
      <SignedInContent>{children}</SignedInContent>
    </RequireSession>
  );
}
