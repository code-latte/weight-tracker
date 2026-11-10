import { type ComponentProps } from "react";
import { RefreshControl as NativeRefreshControl } from "react-native";
import { useTheme } from "react-native-paper";

type RefreshControlProps = Omit<
  ComponentProps<typeof NativeRefreshControl>,
  "colors" | "tintColor"
>;

/** The project's RefreshControl: the platform's pull-to-refresh spinner, in the Paper theme's primary colour. */
export default function RefreshControl(props: RefreshControlProps) {
  const theme = useTheme();

  return (
    <NativeRefreshControl
      colors={[theme.colors.primary]}
      tintColor={theme.colors.primary}
      progressBackgroundColor={theme.colors.surface}
      {...props}
    />
  );
}
