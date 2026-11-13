import { Image, StyleSheet, View } from "react-native";
import SafeArea from "@/presentation/common/components/atoms/safe-area";
import { useAppTheme } from "@/presentation/common/theme";

// The logo with the name in it, one image per theme (both 1200 × 265).
const logos = {
  light: require("../../../../../assets/brand/trainera-logo-light-1200.png"),
  dark: require("../../../../../assets/brand/trainera-logo-dark-1200.png"),
};

// The bar stays 48 px high: the logo's height plus the padding above and below it.
const LOGO_HEIGHT = 36;
const BAR_PADDING = 6;
const LOGO_RATIO = 1200 / 265;

interface BrandBarProps {
  /** The product's name, read out for the logo. */
  name: string;
}

/** A row in the primary colour with the logo centred, above the screens' own heads. It takes the top inset. */
export default function BrandBar({ name }: BrandBarProps) {
  const theme = useAppTheme();

  return (
    <SafeArea
      edges={["top", "left", "right"]}
      style={[styles.bar, { backgroundColor: theme.colors.primary }]}
    >
      <View style={styles.row}>
        <Image
          source={theme.dark ? logos.dark : logos.light}
          accessible
          accessibilityRole="image"
          accessibilityLabel={name}
          resizeMode="contain"
          style={styles.logo}
        />
      </View>
    </SafeArea>
  );
}

const styles = StyleSheet.create({
  bar: {
    flex: 0,
  },
  row: {
    alignItems: "center",
    justifyContent: "center",
    paddingVertical: BAR_PADDING,
  },
  logo: {
    height: LOGO_HEIGHT,
    width: LOGO_HEIGHT * LOGO_RATIO,
  },
});
