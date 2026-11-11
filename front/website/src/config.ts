/** The launch states the content knows; only the pre-launch one is built in this version. */
export const LAUNCH_STATES = ["prelaunch"] as const;
export type LaunchState = (typeof LAUNCH_STATES)[number];

const DEFAULT_API_URL = "http://localhost:5199";
const DEFAULT_SITE_URL = "http://localhost:3001";

/** The launch state a setting names, or `prelaunch` for none or an unknown one. */
export function launchStateOf(value: string | undefined): LaunchState {
  return LAUNCH_STATES.find((state) => state === value) ?? "prelaunch";
}

/** A base address without its trailing slash, or the fallback for none. */
export function baseUrlOf(value: string | undefined, fallback: string): string {
  const url = value?.trim() || fallback;
  return url.replace(/\/+$/, "");
}

/** Read once at build time: `NEXT_PUBLIC_*` values are inlined into the static export. */
export const config = {
  apiUrl: baseUrlOf(process.env.NEXT_PUBLIC_API_URL, DEFAULT_API_URL),
  siteUrl: baseUrlOf(process.env.NEXT_PUBLIC_SITE_URL, DEFAULT_SITE_URL),
  launchState: launchStateOf(process.env.NEXT_PUBLIC_LAUNCH_STATE),
};
