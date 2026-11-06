"use client";

import { type ReactNode } from "react";
import List from "@/presentation/common/components/atoms/list";
import ListItem from "@/presentation/common/components/atoms/list-item";
import ListItemButton from "@/presentation/common/components/atoms/list-item-button";
import ListItemIcon from "@/presentation/common/components/atoms/list-item-icon";
import ListItemText from "@/presentation/common/components/atoms/list-item-text";

export interface SectionOption<T extends string> {
  value: T;
  label: string;
  icon: ReactNode;
  disabled?: boolean;
}

interface SectionListProps<T extends string> {
  sections: SectionOption<T>[];
  selected: T;
  onChange: (section: T) => void;
  "aria-label": string;
}

/** A vertical list of sections, each an icon and a name, with exactly one selected. */
export default function SectionList<T extends string>({
  sections,
  selected,
  onChange,
  "aria-label": ariaLabel,
}: SectionListProps<T>) {
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
