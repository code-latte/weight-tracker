"use client";

import type { ComponentType } from "react";
import AssignmentIndOutlined from "@mui/icons-material/AssignmentIndOutlined";
import AssignmentOutlined from "@mui/icons-material/AssignmentOutlined";
import CalendarMonthOutlined from "@mui/icons-material/CalendarMonthOutlined";
import DescriptionOutlined from "@mui/icons-material/DescriptionOutlined";
import EventNoteOutlined from "@mui/icons-material/EventNoteOutlined";
import FitnessCenterOutlined from "@mui/icons-material/FitnessCenterOutlined";
import GroupOutlined from "@mui/icons-material/GroupOutlined";
import HomeOutlined from "@mui/icons-material/HomeOutlined";
import ListAltOutlined from "@mui/icons-material/ListAltOutlined";
import MenuOutlined from "@mui/icons-material/MenuOutlined";
import PendingActionsOutlined from "@mui/icons-material/PendingActionsOutlined";
import PersonAddAltOutlined from "@mui/icons-material/PersonAddAltOutlined";
import RateReviewOutlined from "@mui/icons-material/RateReviewOutlined";
import Box from "@mui/material/Box";
import Drawer from "@mui/material/Drawer";
import List from "@mui/material/List";
import ListItemButton from "@mui/material/ListItemButton";
import type { Theme } from "@mui/material/styles";
import IconButton from "@/presentation/common/components/atoms/icon-button";
import ListItemIcon from "@/presentation/common/components/atoms/list-item-icon";
import ListItemText from "@/presentation/common/components/atoms/list-item-text";
import ListSubheader from "@/presentation/common/components/atoms/list-subheader";
import Toolbar from "@/presentation/common/components/atoms/toolbar";
import Tooltip from "@/presentation/common/components/atoms/tooltip";
import useCurrentUser from "@/presentation/common/hooks/use-current-user";
import useNavigationDrawer from "@/presentation/common/hooks/use-navigation-drawer";
import NextLink from "next/link";
import { usePathname } from "next/navigation";
import { useTranslation } from "react-i18next";

const DRAWER_WIDTH = 260;
const COLLAPSED_WIDTH = 72;

/** A collapsed entry centres its icon in the rail. */
const collapsedItemSx = {
  justifyContent: "center",
  px: 0,
  "& .MuiListItemIcon-root": { minWidth: 0 },
};

interface MenuItemDefinition {
  label: string;
  icon: ComponentType;
  /** The item's screen. Items without one have no screen yet and render as buttons that do nothing. */
  href?: string;
}

interface DrawerItemProps {
  label: string;
  icon: ComponentType;
  href?: string;
  selected: boolean;
  expanded: boolean;
}

/**
 * The people section: an admin manages every user and their trainer assignment from it; anyone else, or a visitor whose profile has not loaded yet,
 * sees the athletes copy. Both reach the invitations screen, which shows what each role may see.
 */
function peopleSection(isAdmin: boolean): { title: string; items: MenuItemDefinition[] } {
  return {
    title: isAdmin ? "menu.users.title" : "menu.athletes.title",
    items: [
      ...(isAdmin
        ? [
            { label: "menu.users.all", icon: GroupOutlined, href: "/users" },
            {
              label: "menu.users.trainerAssignments",
              icon: AssignmentIndOutlined,
              href: "/trainer-assignments",
            },
          ]
        : [{ label: "menu.athletes.all", icon: GroupOutlined, href: "/athletes" }]),
      { label: "menu.athletes.invitations", icon: PersonAddAltOutlined, href: "/invitations" },
    ],
  };
}

/** Sections after Inicio and the people section. */
const sections: { title: string; items: MenuItemDefinition[] }[] = [
  {
    title: "menu.programming.title",
    items: [
      {
        label: "menu.programming.assignedPlans",
        icon: AssignmentOutlined,
        href: "/assigned-plans",
      },
      {
        label: "menu.programming.assignedWorkouts",
        icon: ListAltOutlined,
        href: "/assigned-workouts",
      },
      { label: "menu.programming.calendar", icon: CalendarMonthOutlined },
    ],
  },
  {
    title: "menu.library.title",
    items: [
      { label: "menu.library.workoutTemplates", icon: DescriptionOutlined },
      {
        label: "menu.library.exerciseCatalog",
        icon: FitnessCenterOutlined,
        href: "/exercises",
      },
    ],
  },
  {
    title: "menu.tracking.title",
    items: [
      { label: "menu.tracking.workoutFeedback", icon: RateReviewOutlined },
      { label: "menu.tracking.weeklyReviews", icon: EventNoteOutlined },
      { label: "menu.tracking.pendingReview", icon: PendingActionsOutlined },
    ],
  },
];

/** One entry: an icon and its label, or the icon alone with the label as a tooltip when the drawer is collapsed. */
function DrawerItem({ label, icon: Icon, href, selected, expanded }: DrawerItemProps) {
  const linkProps = href ? { component: NextLink, href, selected } : {};
  const button = (
    <ListItemButton
      {...linkProps}
      aria-label={expanded ? undefined : label}
      sx={expanded ? undefined : collapsedItemSx}
    >
      <ListItemIcon>
        <Icon />
      </ListItemIcon>
      {expanded && <ListItemText primary={label} />}
    </ListItemButton>
  );

  return expanded ? (
    button
  ) : (
    <Tooltip title={label} placement="right">
      {button}
    </Tooltip>
  );
}

export default function NavigationDrawer() {
  const { t } = useTranslation("common");
  const pathname = usePathname();
  const { expanded, toggleExpanded } = useNavigationDrawer();
  const { profile } = useCurrentUser();
  const width = expanded ? DRAWER_WIDTH : COLLAPSED_WIDTH;
  const widthTransition = (theme: Theme) =>
    theme.transitions.create("width", { duration: theme.transitions.duration.shorter });

  return (
    <Drawer
      variant="permanent"
      sx={{ width, flexShrink: 0, transition: widthTransition }}
      slotProps={{
        paper: {
          component: "nav",
          "aria-label": t("menu.label"),
          sx: { width, boxSizing: "border-box", overflowX: "hidden", transition: widthTransition },
        },
      }}
    >
      {/* Same height as the header's toolbar, so both rows line up. */}
      <Toolbar
        disableGutters
        sx={{ px: expanded ? 2 : 0, justifyContent: expanded ? "space-between" : "center" }}
      >
        {/* The wordmark for dark backgrounds: the sidebar is dark in both theme modes. */}
        {expanded && (
          <Box
            component="img"
            src="/trainera-logo-dark.svg"
            alt={t("menu.appName")}
            sx={{ height: 32 }}
          />
        )}
        <IconButton
          color="inherit"
          aria-label={t(expanded ? "menu.collapse" : "menu.expand")}
          aria-expanded={expanded}
          onClick={toggleExpanded}
        >
          <MenuOutlined />
        </IconButton>
      </Toolbar>
      <List>
        <DrawerItem
          label={t("menu.home")}
          icon={HomeOutlined}
          href="/"
          selected={pathname === "/"}
          expanded={expanded}
        />
      </List>
      {[peopleSection(profile?.role === "ADMIN"), ...sections].map((section) => (
        <List
          key={section.title}
          subheader={expanded ? <ListSubheader>{t(section.title)}</ListSubheader> : undefined}
        >
          {section.items.map(({ label, icon, href }) => (
            <DrawerItem
              key={label}
              label={t(label)}
              icon={icon}
              href={href}
              selected={href !== undefined && pathname === href}
              expanded={expanded}
            />
          ))}
        </List>
      ))}
    </Drawer>
  );
}
