"use client";

import Autocomplete from "@/presentation/common/components/atoms/autocomplete";
import { fullNameOf } from "@/presentation/common/full-name";
import { normalized } from "@/presentation/common/search-text";
import type { Assignee } from "@/presentation/common/hooks/use-assignees";

interface PersonPickerProps {
  label: string;
  options: Assignee[];
  /** The id of the chosen person, or an empty string when none is chosen. */
  value: string;
  /** Called with the id of the newly chosen person, or an empty string when the choice is cleared. */
  onChange: (id: string) => void;
  noOptionsText: string;
  disabled?: boolean;
  /** A message under the field, such as a structural violation. */
  error?: string;
}

/** "Ana García · ana@example.com", or only the email when the person has no name. */
function labelOf(person: Assignee): string {
  const name = fullNameOf(person);

  return name ? `${name} · ${person.email}` : person.email;
}

/** Keeps the people whose full name or email contain the typed text, ignoring case and accents. */
function filterPeople(people: Assignee[], input: string): Assignee[] {
  const text = normalized(input.trim());

  return people.filter((person) =>
    [fullNameOf(person) ?? "", person.email].some((field) => normalized(field).includes(text)),
  );
}

/** Picks one person among the given options by typing part of their name or email. */
export default function PersonPicker({
  label,
  options,
  value,
  onChange,
  noOptionsText,
  disabled = false,
  error,
}: PersonPickerProps) {
  return (
    <Autocomplete
      label={label}
      options={options}
      value={options.find((option) => option.id === value) ?? null}
      onChange={(_event, person) => onChange(person?.id ?? "")}
      getOptionLabel={labelOf}
      getOptionKey={(option) => option.id}
      isOptionEqualToValue={(option, chosen) => option.id === chosen.id}
      filterOptions={(people, { inputValue }) => filterPeople(people, inputValue)}
      noOptionsText={noOptionsText}
      disabled={disabled}
      error={Boolean(error)}
      helperText={error}
    />
  );
}
