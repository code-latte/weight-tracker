import { useCallback } from "react";
import { useTranslation } from "react-i18next";

/** Turns a `YYYY-MM-DD` date into text in the active language, such as "3 nov 2026". */
export default function useMesocycleDate(): (date: string) => string {
  const { i18n } = useTranslation("mesocycles");
  const language = i18n.language;

  return useCallback(
    (date: string) =>
      new Intl.DateTimeFormat(language, {
        day: "numeric",
        month: "short",
        year: "numeric",
        // The day is a UTC midnight; formatting it in UTC keeps it on its calendar day.
        timeZone: "UTC",
      }).format(new Date(`${date}T00:00:00Z`)),
    [language],
  );
}
