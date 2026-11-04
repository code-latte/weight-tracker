"use client";

import { type ReactNode } from "react";
import List from "@/presentation/common/components/atoms/list";
import ListItem from "@/presentation/common/components/atoms/list-item";
import ListItemButton from "@/presentation/common/components/atoms/list-item-button";
import ListItemIcon from "@/presentation/common/components/atoms/list-item-icon";
import ListItemText from "@/presentation/common/components/atoms/list-item-text";

/** The slices of the catalog the user can switch between. */
export type ExerciseSection = "ALL" | "MINE" | "FAVORITES";

export interface ExerciseSectionOption {
  value: ExerciseSection;
  label: string;
  icon: ReactNode;
  disabled?: boolean;
}

interface ExerciseSectionListProps {
  sections: ExerciseSectionOption[];
  selected: ExerciseSection;
  onChange: (section: ExerciseSection) => void;
  "aria-label": string;
}

/** A vertical list of sections, each an icon and a name, with exactly one selected. */
export default function ExerciseSectionList({
  sections,
  selected,
  onChange,
  "aria-label": ariaLabel,
}: ExerciseSectionListProps) {
  return (
    <List aria-label={ariaLabel} disablePadding>
      {sections.map((section) => (
        <ListItem key={section.value} disablePadding>
          <ListItemButton
            selected={section.value === selected}
            disabled={section.disabled}
            aria-current={section.value === selected ? "true" : undefined}
            onClick={() => onChange(section.value)}
          >
            <ListItemIcon>{section.icon}</ListItemIcon>
            <ListItemText primary={section.label} />
          </ListItemButton>
        </ListItem>
      ))}
    </List>
  );
}
