import type { Metadata } from "next";
import type { ReactNode } from "react";
import { Inter, Plus_Jakarta_Sans } from "next/font/google";
import { config } from "@/config";
import { site } from "@/content/site";
import "@/styles/globals.css";

// Self-hosted at build time; the tokens read them through these CSS variables.
const inter = Inter({ subsets: ["latin"], display: "swap", variable: "--font-inter" });
const plusJakartaSans = Plus_Jakarta_Sans({
  subsets: ["latin"],
  display: "swap",
  variable: "--font-plus-jakarta-sans",
});

// The icons and the Open Graph image come from the file conventions in this folder.
export const metadata: Metadata = {
  metadataBase: new URL(config.siteUrl),
  title: site.title,
  description: site.description,
  alternates: { canonical: "/" },
  openGraph: {
    type: "website",
    locale: site.locale,
    siteName: site.name,
    title: site.title,
    description: site.description,
    url: "/",
  },
  twitter: {
    card: "summary_large_image",
    title: site.title,
    description: site.description,
  },
};

export default function RootLayout({ children }: { children: ReactNode }) {
  return (
    <html lang="es" className={`${inter.variable} ${plusJakartaSans.variable}`}>
      <head>
        {config.cookiebotId && (
          // Cookiebot asks to be the first script in <head>, loaded synchronously: its automatic blocking holds back
          // every later script that sets cookies until the visitor consents. next/script cannot guarantee that order.
          // eslint-disable-next-line @next/next/no-sync-scripts
          <script
            id="Cookiebot"
            src="https://consent.cookiebot.com/uc.js"
            data-cbid={config.cookiebotId}
            data-blockingmode="auto"
            type="text/javascript"
          />
        )}
      </head>
      <body>{children}</body>
    </html>
  );
}
