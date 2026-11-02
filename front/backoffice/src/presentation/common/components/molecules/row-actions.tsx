"use client";

import { useState } from "react";
import MoreVertOutlined from "@mui/icons-material/MoreVertOutlined";
import IconButton from "@/presentation/common/components/atoms/icon-button";
import Menu from "@/presentation/common/components/atoms/menu";
import MenuItem from "@/presentation/common/components/atoms/menu-item";

interface RowActionsProps {
  /** Names the button, such as "Acciones de Ana García". */
  label: string;
  /** The menu's only item, such as "Desactivar". */
  actionLabel: string;
  onAction: () => void;
}

/** A ⋮ button at the end of a row that opens a menu with the row's action. */
export default function RowActions({ label, actionLabel, onAction }: RowActionsProps) {
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);

  function handleAction() {
    setAnchor(null);
    onAction();
  }

  return (
    <>
      <IconButton
        size="small"
        aria-label={label}
        aria-haspopup="true"
        aria-expanded={anchor !== null}
        onClick={(event) => setAnchor(event.currentTarget)}
      >
        <MoreVertOutlined fontSize="small" />
      </IconButton>
      <Menu anchorEl={anchor} open={anchor !== null} onClose={() => setAnchor(null)}>
        <MenuItem onClick={handleAction}>{actionLabel}</MenuItem>
      </Menu>
    </>
  );
}
