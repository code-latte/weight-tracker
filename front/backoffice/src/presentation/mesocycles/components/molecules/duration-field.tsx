"use client";

import AddOutlined from "@mui/icons-material/AddOutlined";
import RemoveOutlined from "@mui/icons-material/RemoveOutlined";
import Box from "@mui/material/Box";
import IconButton from "@/presentation/common/components/atoms/icon-button";
import TextField from "@/presentation/common/components/atoms/text-field";

interface DurationFieldProps {
  label: string;
  /** The typed number of weeks, as text. */
  value: string;
  onChange: (value: string) => void;
  min: number;
  max: number;
  decreaseLabel: string;
  increaseLabel: string;
  disabled?: boolean;
  /** A message under the field, such as a structural violation. */
  error?: string;
}

/** A whole number of weeks typed or stepped with − and +, which stop at the given bounds. */
export default function DurationField({
  label,
  value,
  onChange,
  min,
  max,
  decreaseLabel,
  increaseLabel,
  disabled = false,
  error,
}: DurationFieldProps) {
  const current = Number(value);
  const isNumber = value.trim() !== "" && Number.isInteger(current);

  function step(by: number) {
    const from = isNumber ? current : min;
    onChange(String(Math.min(max, Math.max(min, from + by))));
  }

  return (
    <Box sx={{ display: "flex", alignItems: "flex-start", gap: 1 }}>
      <IconButton
        aria-label={decreaseLabel}
        onClick={() => step(-1)}
        disabled={disabled || (isNumber && current <= min)}
        sx={{ mt: 1 }}
      >
        <RemoveOutlined />
      </IconButton>
      <TextField
        label={label}
        value={value}
        onChange={(event) => onChange(event.target.value)}
        disabled={disabled}
        error={Boolean(error)}
        helperText={error}
        slotProps={{ htmlInput: { inputMode: "numeric", style: { textAlign: "center" } } }}
        sx={{ width: 160 }}
      />
      <IconButton
        aria-label={increaseLabel}
        onClick={() => step(1)}
        disabled={disabled || (isNumber && current >= max)}
        sx={{ mt: 1 }}
      >
        <AddOutlined />
      </IconButton>
    </Box>
  );
}
