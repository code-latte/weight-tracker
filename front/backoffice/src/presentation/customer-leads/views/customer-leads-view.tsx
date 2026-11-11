"use client";

import Container from "@mui/material/Container";
import { useTranslation } from "react-i18next";
import Typography from "@/presentation/common/components/atoms/typography";
import HomeSkeleton from "@/presentation/common/components/molecules/home-skeleton";
import LoadError from "@/presentation/common/components/molecules/load-error";
import AppShell from "@/presentation/common/components/organisms/app-shell";
import CustomerLeadsTable from "@/presentation/customer-leads/components/organisms/customer-leads-table";
import useCustomerLeads from "@/presentation/customer-leads/hooks/use-customer-leads";

function CustomerLeadsContent() {
  const { t } = useTranslation("customerLeads");
  const { status, leads, errorCode, reload } = useCustomerLeads();

  function list() {
    if (status === "failed" && errorCode) {
      return <LoadError code={errorCode} onRetry={reload} />;
    }
    if (status !== "loaded") {
      return <HomeSkeleton />;
    }
    return <CustomerLeadsTable leads={leads} />;
  }

  return (
    <Container sx={{ py: 4 }}>
      <Typography variant="h4" component="h1" sx={{ mb: 3 }}>
        {t("list.title")}
      </Typography>
      {list()}
    </Container>
  );
}

/** Every lead the public website has collected. Only an admin gets them; anyone else sees the API's refusal. */
export default function CustomerLeadsView() {
  return (
    <AppShell>
      <CustomerLeadsContent />
    </AppShell>
  );
}
