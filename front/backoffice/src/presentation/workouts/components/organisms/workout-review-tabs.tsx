"use client";

import BarChartOutlined from "@mui/icons-material/BarChartOutlined";
import ChatBubbleOutlineOutlined from "@mui/icons-material/ChatBubbleOutlineOutlined";
import FormatListBulletedOutlined from "@mui/icons-material/FormatListBulletedOutlined";
import Box from "@mui/material/Box";
import type { ReactElement, ReactNode } from "react";
import { useTranslation } from "react-i18next";
import Tab from "@/presentation/common/components/atoms/tab";
import Tabs from "@/presentation/common/components/atoms/tabs";

export type ReviewTab = "exercises" | "metrics" | "feedback";

interface WorkoutReviewTabsProps {
  value: ReviewTab;
  onChange: (value: ReviewTab) => void;
  /** The content of the "Ejercicios" tab; the other two are empty for now. */
  children: ReactNode;
}

const TABS: { value: ReviewTab; icon: ReactElement }[] = [
  { value: "exercises", icon: <FormatListBulletedOutlined /> },
  { value: "metrics", icon: <BarChartOutlined /> },
  { value: "feedback", icon: <ChatBubbleOutlineOutlined /> },
];

/** The review's tab row and the panel of the selected tab. */
export default function WorkoutReviewTabs({ value, onChange, children }: WorkoutReviewTabsProps) {
  const { t } = useTranslation("workouts");

  return (
    <Box>
      <Tabs
        value={value}
        onChange={(_event, next: ReviewTab) => onChange(next)}
        aria-label={t("review.tabs.label")}
        sx={{ borderBottom: 1, borderColor: "divider", mb: 2 }}
      >
        {TABS.map((tab) => (
          <Tab
            key={tab.value}
            value={tab.value}
            id={`review-tab-${tab.value}`}
            aria-controls={`review-panel-${tab.value}`}
            icon={tab.icon}
            iconPosition="start"
            label={t(`review.tabs.${tab.value}`)}
          />
        ))}
      </Tabs>
      <Box role="tabpanel" id={`review-panel-${value}`} aria-labelledby={`review-tab-${value}`}>
        {value === "exercises" ? children : null}
      </Box>
    </Box>
  );
}
