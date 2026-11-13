"use client";

import { alpha, useTheme } from "@mui/material/styles";
import {
  AreaPlot,
  ChartsAxisHighlight,
  ChartsDataProvider,
  ChartsGrid,
  ChartsReferenceLine,
  ChartsSurface,
  ChartsTooltip,
  ChartsWrapper,
  ChartsXAxis,
  ChartsYAxis,
  LinePlot,
  MarkPlot,
} from "@mui/x-charts";

interface LineChartProps {
  /** One label per point, in order; shown on the x axis. */
  labels: readonly string[];
  /** One value per point, in order. */
  values: readonly number[];
  /** Formats a value for the y axis, the tooltip and the label of the last point. */
  valueFormatter: (value: number) => string;
  height?: number;
  ariaLabel: string;
}

/** The project's line chart: the points joined by a line with the area under it tinted, and the last value labelled. */
export default function LineChart({
  labels,
  values,
  valueFormatter,
  height = 280,
  ariaLabel,
}: LineChartProps) {
  const theme = useTheme();
  // The x axis works on positions, so two sessions with the same label stay apart. An empty slot at each end keeps
  // the first and last points off the borders of the plot.
  const positions = Array.from({ length: values.length + 2 }, (_, index) => index);
  const last = values.length;

  return (
    <ChartsDataProvider
      height={height}
      margin={{ right: 56 }}
      series={[
        {
          type: "line",
          data: [null, ...values, null],
          area: true,
          showMark: true,
          color: theme.palette.primary.main,
          valueFormatter: (value) => (value === null ? "" : valueFormatter(value)),
        },
      ]}
      xAxis={[
        {
          id: "sessions",
          scaleType: "point",
          data: positions,
          valueFormatter: (position: number) => labels[position - 1] ?? "",
          tickLabelInterval: () => true,
        },
      ]}
      yAxis={[
        {
          id: "values",
          width: 72,
          valueFormatter: (value: number) => valueFormatter(value),
        },
      ]}
    >
      <ChartsWrapper>
        <ChartsSurface
          role="img"
          aria-label={ariaLabel}
          sx={{ "& .MuiLineChart-area": { fillOpacity: 0.18 } }}
        >
          <ChartsGrid horizontal />
          <AreaPlot />
          <LinePlot />
          <MarkPlot />
          <ChartsReferenceLine
            x={last}
            label={valueFormatter(values[last - 1])}
            labelAlign="start"
            lineStyle={{ stroke: alpha(theme.palette.primary.main, 0.4), strokeDasharray: "4 4" }}
            labelStyle={{ fill: theme.palette.primary.main, fontWeight: 700 }}
          />
          <ChartsXAxis axisId="sessions" />
          <ChartsYAxis axisId="values" />
          <ChartsAxisHighlight x="line" />
        </ChartsSurface>
        <ChartsTooltip />
      </ChartsWrapper>
    </ChartsDataProvider>
  );
}
