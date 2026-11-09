import { StyleSheet, View } from "react-native";
import HelperText from "@/presentation/common/components/atoms/helper-text";
import TextInput from "@/presentation/common/components/atoms/text-input";

interface ProfileNameField {
  label: string;
  value: string;
  /** Translated error, or null when the field is valid. */
  error: string | null;
  onChange: (value: string) => void;
}

interface ProfileNameFieldsProps {
  name: ProfileNameField;
  surname: ProfileNameField;
  disabled: boolean;
}

/** Name and surname inputs, each with its error under it. */
export default function ProfileNameFields({ name, surname, disabled }: ProfileNameFieldsProps) {
  return (
    <View style={styles.fields}>
      {[name, surname].map((field, index) => (
        <View key={field.label}>
          <TextInput
            label={field.label}
            accessibilityLabel={field.label}
            value={field.value}
            onChangeText={field.onChange}
            error={field.error !== null}
            disabled={disabled}
            autoCapitalize="words"
            autoComplete={index === 0 ? "given-name" : "family-name"}
          />
          <HelperText type="error" visible={field.error !== null}>
            {field.error ?? ""}
          </HelperText>
        </View>
      ))}
    </View>
  );
}

const styles = StyleSheet.create({
  fields: {
    gap: 8,
  },
});
