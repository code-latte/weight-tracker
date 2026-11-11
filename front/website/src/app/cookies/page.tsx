import type { Metadata } from "next";
import LegalPage from "@/components/legal-page";
import { legalPageOf } from "@/content/legal";
import { site } from "@/content/site";

const page = legalPageOf("/cookies");

export const metadata: Metadata = {
  title: `${page.title} | ${site.name}`,
  alternates: { canonical: page.path },
};

export default function Page() {
  return <LegalPage page={page} />;
}
