"use client";

import { type ReactNode } from "react";
import MuiAutocomplete, {
  type AutocompleteProps as MuiAutocompleteProps,
} from "@mui/material/Autocomplete";
import TextField from "@/presentation/common/components/atoms/text-field";

interface AutocompleteProps<T> extends Omit<
  MuiAutocompleteProps<T, false, false, false>,
  "renderInput"
> {
  label: string;
  error?: boolean;
  helperText?: ReactNode;
}

/** The project's single-choice Autocomplete: the MUI widget, its input drawn with the project's TextField. */
export default function Autocomplete<T>({
  label,
  error,
  helperText,
  ...props
}: AutocompleteProps<T>) {
  return (
    <MuiAutocomplete
      {...props}
      renderInput={(params) => (
        <TextField {...params} label={label} error={error} helperText={helperText} />
      )}
    />
  );
}
