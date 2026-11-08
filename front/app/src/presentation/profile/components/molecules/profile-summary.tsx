import { StyleSheet, View } from "react-native";
import { useTheme } from "react-native-paper";
import Avatar from "@/presentation/common/components/atoms/avatar";
import Text from "@/presentation/common/components/atoms/text";
import initials from "@/presentation/common/format/initials";

interface ProfileSummaryProps {
  name: string;
  surname: string;
  email: string;
}

/** The signed-in athlete's identity: initials avatar, full name and email. */
export default function ProfileSummary({ name, surname, email }: ProfileSummaryProps) {
  const theme = useTheme();

  return (
    <View style={styles.container}>
      <Avatar label={initials(name, surname)} size={56} />
      <View style={styles.texts}>
        <Text variant="titleMedium">{`${name} ${surname}`}</Text>
        <Text variant="bodyMedium" style={{ color: theme.colors.onSurfaceVariant }}>
          {email}
        </Text>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flexDirection: "row",
    alignItems: "center",
    gap: 16,
  },
  texts: {
    flex: 1,
  },
});
