import { Chip as PaperChip } from "react-native-paper";

interface ChipProps {
  label: string;
}

/** The project's Chip: a compact, non-pressable React Native Paper chip, themed by the Paper theme. */
export default function Chip({ label }: ChipProps) {
  return (
    <PaperChip compact mode="outlined">
      {label}
    </PaperChip>
  );
}
