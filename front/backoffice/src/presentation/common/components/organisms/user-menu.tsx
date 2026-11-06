"use client";

import { useState } from "react";
import ExpandMoreOutlined from "@mui/icons-material/ExpandMoreOutlined";
import LogoutOutlined from "@mui/icons-material/LogoutOutlined";
import PersonOutlined from "@mui/icons-material/PersonOutlined";
import SettingsOutlined from "@mui/icons-material/SettingsOutlined";
import Alert from "@/presentation/common/components/atoms/alert";
import Avatar from "@/presentation/common/components/atoms/avatar";
import Box from "@mui/material/Box";
import Divider from "@mui/material/Divider";
import IconButton from "@/presentation/common/components/atoms/icon-button";
import ListItemIcon from "@/presentation/common/components/atoms/list-item-icon";
import ListItemText from "@/presentation/common/components/atoms/list-item-text";
import Menu from "@/presentation/common/components/atoms/menu";
import MenuItem from "@/presentation/common/components/atoms/menu-item";
import Skeleton from "@/presentation/common/components/atoms/skeleton";
import Typography from "@/presentation/common/components/atoms/typography";
import { useRouter } from "next/navigation";
import { useTranslation } from "react-i18next";
import useCurrentUser from "@/presentation/common/hooks/use-current-user";
import useLogout from "@/presentation/common/hooks/use-logout";
import { initialsOf } from "@/presentation/common/initials";

export default function UserMenu() {
  const { t } = useTranslation("common");
  const router = useRouter();
  const { profile, status, onboardingPending, clear } = useCurrentUser();
  const { loggingOut, errorCode, logout } = useLogout();
  const showUser = status === "loaded" && profile !== null && !onboardingPending;
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);

  async function handleLogout() {
    if (await logout()) {
      clear();
      router.replace("/login");
    }
  }

  return (
    <Box sx={{ display: "flex", alignItems: "center", gap: 1.5 }}>
      {showUser ? (
        <>
          <Box sx={{ textAlign: "right" }}>
            <Typography variant="body2">{`${profile.name} ${profile.surname}`}</Typography>
            <Typography variant="caption" color="text.secondary">
              {t(`header.user.roles.${profile.role}`)}
            </Typography>
          </Box>
          <Avatar>{initialsOf(profile)}</Avatar>
        </>
      ) : (
        <>
          <Box sx={{ textAlign: "right" }} aria-label={t("loading")}>
            <Skeleton variant="text" width={120} />
            <Skeleton variant="text" width={72} sx={{ ml: "auto" }} />
          </Box>
          <Skeleton variant="circular" width={40} height={40} />
        </>
      )}
      <IconButton
        color="inherit"
        aria-label={t("header.user.menuLabel")}
        aria-haspopup="true"
        aria-expanded={anchor !== null}
        onClick={(event) => setAnchor(event.currentTarget)}
      >
        <ExpandMoreOutlined />
      </IconButton>
      <Menu anchorEl={anchor} open={anchor !== null} onClose={() => setAnchor(null)}>
        <MenuItem
          onClick={() => {
            setAnchor(null);
            router.push("/profile");
          }}
        >
          <ListItemIcon>
            <PersonOutlined />
          </ListItemIcon>
          <ListItemText>{t("header.user.profile")}</ListItemText>
        </MenuItem>
        <MenuItem onClick={() => setAnchor(null)}>
          <ListItemIcon>
            <SettingsOutlined />
          </ListItemIcon>
          <ListItemText>{t("header.user.preferences")}</ListItemText>
        </MenuItem>
        <Divider />
        <MenuItem disabled={loggingOut} onClick={() => void handleLogout()}>
          <ListItemIcon>
            <LogoutOutlined />
          </ListItemIcon>
          <ListItemText>{t("header.user.logout")}</ListItemText>
        </MenuItem>
        {errorCode && (
          <Alert severity="error" sx={{ mx: 2, mb: 1 }}>
            {t(`errors:${errorCode}`)}
          </Alert>
        )}
      </Menu>
    </Box>
  );
}
