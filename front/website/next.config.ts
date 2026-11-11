import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Static site: no server runtime (STPR_INVARIANTS.md, section 4.4).
  output: "export",
  // A static export has no image optimiser; images ship already optimised.
  images: { unoptimized: true },
  // Development only: lets a phone on the local network load `next dev` by the computer's address. Without it, Next
  // blocks its dev scripts for any origin but localhost and the page never becomes interactive.
  allowedDevOrigins: ["192.168.*.*", "10.*.*.*", "*.local"],
};

export default nextConfig;
