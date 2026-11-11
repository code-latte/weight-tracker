import type { MetadataRoute } from "next";
import { config } from "@/config";
import { legalPages } from "@/content/legal";

// Prerendered into the static export.
export const dynamic = "force-static";

export default function sitemap(): MetadataRoute.Sitemap {
  return [
    { url: `${config.siteUrl}/`, changeFrequency: "weekly", priority: 1 },
    ...legalPages.map((page) => ({
      url: `${config.siteUrl}${page.path}`,
      changeFrequency: "yearly" as const,
      priority: 0.3,
    })),
  ];
}
