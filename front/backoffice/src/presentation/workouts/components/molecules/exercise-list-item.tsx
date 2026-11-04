"use client";

import type { CSSProperties, ReactNode, Ref } from "react";
import Box from "@mui/material/Box";
import Card from "@/presentation/common/components/atoms/card";
import Chip from "@/presentation/common/components/atoms/chip";
import ListItem from "@/presentation/common/components/atoms/list-item";
import Typography from "@/presentation/common/components/atoms/typography";
import type { LabelColor } from "@/presentation/workouts/hooks/use-exercise-labels";

/** Lists of exercise cards flow left to right in rows of at most three; fewer as the screen narrows. */
export const EXERCISE_GRID_COLUMNS = {
  xs: "minmax(0, 1fr)",
  sm: "repeat(2, minmax(0, 1fr))",
  md: "repeat(3, minmax(0, 1fr))",
};

export interface ExerciseLabel {
  text: string;
  color?: LabelColor;
  /** Filled with its colour, or only outlined in it. */
  variant?: "filled" | "outlined";
}

interface ExerciseListItemProps {
  name: string;
  /** A softer line under the name, such as the English name beside a translated one. */
  subtitle?: string;
  /** Shown as small labels under the name, in this order. */
  labels: ExerciseLabel[];
  /** A short status shown beside the name, such as "Pendiente". */
  note?: string;
  /** Extra facts shown under the labels, one per line. */
  lines?: string[];
  /** The exercise's place in its list, shown in a circle before the name. */
  position?: number;
  selected?: boolean;
  /** Shown on the left, before the position, such as a drag handle. */
  leading?: ReactNode;
  /** Icon buttons shown on the right. */
  actions: ReactNode;
  /** Shown at the bottom of the card, under the labels and the lines, such as a button. */
  footer?: ReactNode;
  /** The item's node and inline style, for a wrapper that moves it, such as a sortable list. */
  ref?: Ref<HTMLLIElement>;
  style?: CSSProperties;
  /**
   * Drawn behind the content in the card's bottom-right corner, at 75% of its height: an icon such as the exercise
   * type's. It takes no part in the layout, so the content does not move.
   */
  watermark?: ReactNode;
  /**
   * How far, in `sx` spacing, the watermark ends from the card's right edge: room for actions it must not sit under.
   */
  watermarkInsetRight?: number;
}

/**
 * One exercise as a bordered card, shared by the catalog and the workout: its first row holds the leading slot (such
 * as a drag handle), the position, the name and the actions; the labels and the lines follow below.
 */
export default function ExerciseListItem({
  name,
  subtitle,
  labels,
  note,
  lines = [],
  position,
  selected = false,
  leading,
  actions,
  footer,
  ref,
  style,
  watermark,
  watermarkInsetRight = 0,
}: ExerciseListItemProps) {
  const leadingSlot = leading && (
    <Box sx={{ display: "flex", flexShrink: 0, alignSelf: "center" }}>{leading}</Box>
  );
  const positionBadge = position !== undefined && (
    <Box
      sx={{
        flexShrink: 0,
        width: 24,
        height: 24,
        borderRadius: "50%",
        bgcolor: "primary.main",
        color: "primary.contrastText",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
      }}
    >
      <Typography variant="caption" component="span" sx={{ fontWeight: 600 }}>
        {position}
      </Typography>
    </Box>
  );
  const title = (
    <Typography sx={{ overflowWrap: "anywhere" }}>
      {name}
      {note && (
        <Typography
          component="span"
          variant="caption"
          color="text.secondary"
          sx={{ ml: 1, fontStyle: "italic" }}
        >
          {note}
        </Typography>
      )}
    </Typography>
  );
  const subtitleText = subtitle && (
    <Typography
      variant="body2"
      color="text.secondary"
      sx={{ overflowWrap: "anywhere", opacity: 0.75 }}
    >
      {subtitle}
    </Typography>
  );
  const labelChips = labels.length > 0 && (
    <Box sx={{ display: "flex", flexWrap: "wrap", gap: 0.5, mt: 0.5 }}>
      {labels.map(({ text, color = "default", variant = "outlined" }, index) => (
        // Body part and muscle can share a text (Pecho, Pecho), so the position is the key.
        <Chip key={index} label={text} size="small" variant={variant} color={color} />
      ))}
    </Box>
  );
  const lineTexts = lines.map((line) => (
    <Typography key={line} variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
      {line}
    </Typography>
  ));
  // Inline rather than MUI's absolutely positioned secondary action, so names get all the width left.
  const actionButtons = (
    <Box sx={{ display: "flex", flexShrink: 0, alignSelf: "center" }}>{actions}</Box>
  );
  const cardBackground = selected ? "action.selected" : undefined;

  return (
    <ListItem
      ref={ref}
      style={style}
      disablePadding
      aria-current={selected ? "true" : undefined}
      sx={{ display: "block", height: "100%" }}
    >
      <Card
        variant="outlined"
        sx={{
          position: "relative",
          overflow: "hidden",
          height: "100%",
          p: 1.5,
          bgcolor: cardBackground,
        }}
      >
        {watermark && (
          <Box
            aria-hidden
            data-watermark
            sx={{
              position: "absolute",
              right: (theme) => theme.spacing(watermarkInsetRight),
              bottom: 0,
              height: "75%",
              aspectRatio: "1",
              pointerEvents: "none",
              zIndex: 0,
              display: "flex",
              "& svg": { width: "100%", height: "100%" },
            }}
          >
            {watermark}
          </Box>
        )}
        <Box sx={{ position: "relative", zIndex: 1 }}>
          <Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
            {leadingSlot}
            {positionBadge}
            <Box sx={{ minWidth: 0, flex: 1 }}>
              {title}
              {subtitleText}
            </Box>
            {actionButtons}
          </Box>
          {labelChips}
          {lineTexts}
          {footer && <Box sx={{ mt: 0.5 }}>{footer}</Box>}
        </Box>
      </Card>
    </ListItem>
  );
}
