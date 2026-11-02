"use client";

import { useState } from "react";
import PersonAddAltOutlined from "@mui/icons-material/PersonAddAltOutlined";
import Box from "@mui/material/Box";
import Container from "@mui/material/Container";
import type { AthleteInvitation } from "@weight-tracker/core/domain";
import { useTranslation } from "react-i18next";
import Alert from "@/presentation/common/components/atoms/alert";
import Button from "@/presentation/common/components/atoms/button";
import TextField from "@/presentation/common/components/atoms/text-field";
import Typography from "@/presentation/common/components/atoms/typography";
import HomeSkeleton from "@/presentation/common/components/molecules/home-skeleton";
import LoadError from "@/presentation/common/components/molecules/load-error";
import AppShell from "@/presentation/common/components/organisms/app-shell";
import useCurrentUser from "@/presentation/common/hooks/use-current-user";
import InvitationStatusFilter from "@/presentation/invitations/components/molecules/invitation-status-filter";
import InvitationsTable from "@/presentation/invitations/components/organisms/invitations-table";
import InviteAthleteDialog from "@/presentation/invitations/components/organisms/invite-athlete-dialog";
import RevokeInvitationDialog from "@/presentation/invitations/components/organisms/revoke-invitation-dialog";
import { filterInvitations } from "@/presentation/invitations/filter-invitations";
import useInvitations from "@/presentation/invitations/hooks/use-invitations";
import { invitationStatuses } from "@/presentation/invitations/statuses";

type Notice = "invited" | "revoked";

function InvitationsContent() {
  const { t } = useTranslation("invitations");
  const current = useCurrentUser();
  const { status, invitations, errorCode, reload } = useInvitations();
  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState<string | null>(null);
  const [inviting, setInviting] = useState(false);
  const [target, setTarget] = useState<AthleteInvitation | null>(null);
  const [notice, setNotice] = useState<Notice | null>(null);
  // Waits for the caller's profile too: what the screen shows depends on the role.
  const loaded = status === "loaded" && current.status === "loaded" && current.profile !== null;
  const role = current.status === "loaded" ? current.profile?.role : undefined;
  const isAdmin = role === "ADMIN";
  const isTrainer = role === "TRAINER";

  function handleInvited() {
    setNotice("invited");
    reload();
  }

  function handleRevoked() {
    setTarget(null);
    setNotice("revoked");
    reload();
  }

  function handleRevokeClosed(stale: boolean) {
    setTarget(null);
    if (stale) {
      reload();
    }
  }

  function openInvite() {
    setNotice(null);
    setInviting(true);
  }

  function openRevoke(invitation: AthleteInvitation) {
    setNotice(null);
    setTarget(invitation);
  }

  function list() {
    if (current.status === "failed" && current.errorCode) {
      return <LoadError code={current.errorCode} onRetry={current.reload} />;
    }
    if (status === "failed" && errorCode) {
      return <LoadError code={errorCode} onRetry={reload} />;
    }
    if (!loaded) {
      return <HomeSkeleton />;
    }
    return (
      <InvitationsTable
        invitations={filterInvitations(invitations, search, statusFilter, { byTrainer: isAdmin })}
        filtered={search.trim() !== "" || statusFilter !== null}
        showTrainer={isAdmin}
        onRevoke={isTrainer ? openRevoke : undefined}
      />
    );
  }

  return (
    <Container sx={{ py: 4 }}>
      <Box sx={{ display: "flex", alignItems: "center", justifyContent: "space-between", mb: 3 }}>
        <Typography variant="h4" component="h1">
          {t("list.title")}
        </Typography>
        {isTrainer && (
          <Button variant="contained" startIcon={<PersonAddAltOutlined />} onClick={openInvite}>
            {t("list.invite")}
          </Button>
        )}
      </Box>
      <Box sx={{ display: "flex", flexDirection: "column", gap: 2, mb: 3 }}>
        <TextField
          type="search"
          label={t(isAdmin ? "list.searchByTrainerOrEmail" : "list.searchByEmail")}
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          sx={{ maxWidth: 480 }}
        />
        <InvitationStatusFilter
          label={t("list.statusFilter")}
          options={invitationStatuses.map((code) => ({ code, label: t(`status.${code}`) }))}
          selected={statusFilter}
          onToggle={(code) => setStatusFilter((selected) => (selected === code ? null : code))}
        />
      </Box>
      {notice && (
        <Alert severity="success" onClose={() => setNotice(null)} sx={{ mb: 2 }}>
          {t(notice === "invited" ? "invite.success" : "revoke.success")}
        </Alert>
      )}
      {list()}
      {isTrainer && (
        <>
          <InviteAthleteDialog
            open={inviting}
            onClose={() => setInviting(false)}
            onInvited={handleInvited}
          />
          <RevokeInvitationDialog
            invitation={target}
            onClose={handleRevokeClosed}
            onRevoked={handleRevoked}
          />
        </>
      )}
    </Container>
  );
}

export default function InvitationsView() {
  return (
    <AppShell>
      <InvitationsContent />
    </AppShell>
  );
}
