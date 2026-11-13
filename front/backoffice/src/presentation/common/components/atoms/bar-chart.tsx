"use client";

import { alpha, useTheme } from "@mui/material/styles";
import { BarChart as MuiBarChart } from "@mui/x-charts";

interface BarChartProps {
  /** One label per bar, in order; shown on the x axis. */
  labels: readonly string[];
  /** One value per bar, in order. */
  values: readonly number[];
  /** Formats a value for the y axis, the tooltip and the label of the last bar. */
  valueFormatter: (value: number) => string;
  height?: number;
  ariaLabel: string;
}

/** The project's bar chart: one bar per value, the last one in full colour and labelled with its value. */
export default function BarChart({
  labels,
  values,
  valueFormatter,
  height = 280,
  ariaLabel,
}: BarChartProps) {
  const theme = useTheme();
  const last = values.length - 1;
  const positions = values.map((_, index) => index);

  return (
    <MuiBarChart
      height={height}
      hideLegend
      aria-label={ariaLabel}
      series={[
        {
          data: [...values],
          valueFormatter: (value) => (value === null ? "" : valueFormatter(value)),
          barLabel: (item) => (item.dataIndex === last ? valueFormatter(values[last]) : null),
          barLabelPlacement: "outside",
        },
      ]}
      xAxis={[
        {
          scaleType: "band",
          data: positions,
          valueFormatter: (position: number) => labels[position] ?? "",
          tickLabelInterval: () => true,
          colorMap: {
            type: "ordinal",
            values: positions,
            colors: positions.map((position) =>
              position === last
                ? theme.palette.primary.main
                : alpha(theme.palette.primary.main, 0.55),
            ),
          },
        },
      ]}
      yAxis={[{ width: 72, valueFormatter: (value: number) => valueFormatter(value) }]}
    />
  );
}
