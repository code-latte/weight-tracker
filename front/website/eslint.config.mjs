import { defineConfig, globalIgnores } from "eslint/config";
import nextVitals from "eslint-config-next/core-web-vitals";
import nextTs from "eslint-config-next/typescript";
import prettier from "eslint-config-prettier";

/** The website depends on no other workspace (STPR_INVARIANTS.md, section 4.4). */
const otherWorkspaces = {
  patterns: [
    {
      group: ["@weight-tracker/*"],
      message: "The website does not use core (STPR_INVARIANTS.md, section 4.4).",
    },
    {
      regex: "^(\\.\\./)+(core|backoffice|app)(/|$)",
      message: "The website imports no other workspace (STPR_INVARIANTS.md, section 4.4).",
    },
  ],
};

const eslintConfig = defineConfig([
  ...nextVitals,
  ...nextTs,
  {
    rules: {
      "no-restricted-imports": ["error", otherWorkspaces],
    },
  },
  prettier,
  // Override default ignores of eslint-config-next.
  globalIgnores([
    // Default ignores of eslint-config-next:
    ".next/**",
    "out/**",
    "build/**",
    "next-env.d.ts",
    // Test reports:
    "playwright-report/**",
    "test-results/**",
  ]),
]);

export default eslintConfig;
